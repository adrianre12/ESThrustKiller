using ESThrustKiller.Configuration;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using static ESThrustKiller.Configuration.ZoneConfig;

namespace ESThrustKiller.ZoneThrust
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false)]
    public class ZoneThrust : MyGameLogicComponent
    {
        const int PollPeriodSpace = 111;//3mins //375;//12;//375; //10 mins
        const int PollPeriodPlanet = 37;//3; //37; //1 min
        const int MaxAltitude = 65000;

        private IMyThrust myThrust;
        private double altiude;
        private CacheItem currentState = new CacheItem();
        private int pollCounter = 0;
        private Vector3D myPosition;
        private MyPlanet closestPlanet;
        private ZoneConfig.PlanetInfo planetInfo;
        private List<MySafeZone> tmpBuffer = new List<MySafeZone>();//this is to stop GetSafeZonesInAABB() creating one each call.
        private long currentFrame;
        private bool cacheHit;
        private float damageMultiplyer = 0.005f;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            myThrust = Entity as IMyThrust;
        }


        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (!MyAPIGateway.Session.IsServer || myThrust?.CubeGrid?.Physics == null) // ignore projected and other non-physical grids
                return;

            if (Config != null && !Config.ConfigLoaded)
            {
                Config = Config.LoadSettings();
            }
            Log.DebugLog = Config.DebugLog;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            if (!MyAPIGateway.Session.IsServer)
                return;

            if (currentState.TurnOff)
            {
                DoDamage();
            }

            if (pollCounter > 0) //using a poll counter to avoid having to get the frame counter each time.
            {
                pollCounter--;
                //Log.Debug($"Update100 Grid={myThrust.CubeGrid.DisplayName} pollCounter. {pollCounter}");
                return;
            }
            currentFrame = MyAPIGateway.Session.GameplayFrameCounter;
            cacheHit = GetCachedState();

            if (currentFrame > currentState.NextFrame)
            {
                //Log.Debug($"Cache Expired Grid={myThrust.CubeGrid.DisplayName} {currentFrame} > {currentState.NextFrame}");
                CalculateState();
                cacheHit = false;
            }

            if (currentState.NearPlanet)
            {
                pollCounter = PollPeriodPlanet;
            }
            else
            {
                pollCounter = PollPeriodSpace;
            }

            if (!cacheHit)
            {
                currentState.NextFrame = currentFrame + pollCounter * 100;
                GridStateCache[myThrust.CubeGrid.EntityId] = currentState;
            }

            if (Config.DebugLog) // more efficent
                Log.Debug($"CurrentState Grid={myThrust.CubeGrid.DisplayName} PollCounter={pollCounter} TurnOff={currentState.TurnOff} NearPlanet={currentState.NearPlanet} cacheHit={cacheHit} NextFrame={currentState.NextFrame}");
        }

        private void DoDamage()
        {
            if (!myThrust.Enabled || !myThrust.IsFunctional)
                return;

            var slim = myThrust.SlimBlock;
            slim.DoDamage(slim.MaxIntegrity * damageMultiplyer, MyDamageType.Bolt, true);
            damageMultiplyer *= 2;
            slim.FatBlock.SetDamageEffect(true);

            if (Config.DebugLog)
                Log.Debug($"DoDamage {myThrust.CubeGrid.DisplayName} MaxIntegrity={slim.MaxIntegrity} Integrity={slim.Integrity} IsFunctional={myThrust.IsFunctional} ");
        }

        private bool GetCachedState()
        {
            try
            {
                if (GridStateCache.TryGetValue(myThrust.CubeGrid.EntityId, out currentState))
                {

                    //Log.Debug($"Cache hit Grid={myThrust.CubeGrid.DisplayName} ");
                    return true;
                }
            }
            catch (NullReferenceException ex)
            {
                Log.Msg($"NullReference in GetCachedState(): {ex}");
                return false;
            }

            //Log.Debug($"Cache miss Grid={myThrust.CubeGrid.DisplayName} ");
            return false;
        }

        private void CalculateState()
        {
            currentState = new CacheItem();
            myPosition = myThrust.CubeGrid.GetPosition();

            try
            {
                closestPlanet = MyGamePruningStructure.GetClosestPlanet(myPosition);
                if (closestPlanet == null)
                {
                    //Log.Debug($"No Planet Grid={myThrust.CubeGrid.DisplayName} thruster={myThrust.DisplayNameText} turnOff={currentState.TurnOff}");
                    return;
                }
                if (closestPlanet.Generator != null)
                {
                    if (!Config.TryGetPlanet(closestPlanet.StorageName, out planetInfo)) //planet not found
                    {
                        //Log.Debug($"Grid={myThrust.CubeGrid.DisplayName} No Planet match");
                        return;
                    }
                    currentState.NearPlanet = true;
                }
            }
            catch (NullReferenceException ex)
            {
                Log.Msg($"NullReference in CalculateState() part 1: {ex}");
                return;
            }

            try
            {
                altiude = closestPlanet.GetHeightFromSurface(myPosition);
                if (altiude > MaxAltitude)
                {
                    currentState.NearPlanet = false;
                    //Log.Debug($"Too High Grid={myThrust.CubeGrid.DisplayName}");
                    return;
                }
                //turnOff, bellow altitude and not in zone;
                currentState.TurnOff = NotInSafeZone() && altiude < (double)planetInfo.Altitude;

            }
            catch (NullReferenceException ex)
            {
                Log.Msg($"NullReference in CalculateState() part 2: {ex}");
                return;
            }
        }

        public override void MarkForClose()
        {
            base.MarkForClose();

            //Log.Msg($"MarkForClose Grid={myThrust.CubeGrid.DisplayName} closing={closing}");
            if (!MyAPIGateway.Session.IsServer)
                return;

            GridStateCache.Remove(myThrust.CubeGrid.EntityId);
        }

        private bool NotInSafeZone()
        {
            tmpBuffer.Clear();
            return !MySessionComponentSafeZones.IsInSafezone(myThrust.CubeGrid.EntityId, MySessionComponentSafeZones.GetSafeZonesInAABB(myThrust.CubeGrid.WorldAABB, tmpBuffer));
        }

    }
}
