using ESThrustKiller.Configuration;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace ESThrustKiller.ZoneThrust
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false)]
    public class ZoneThrust : MyGameLogicComponent
    {
        const int SlowPollPeriod = 60;
        private static ZoneConfig config = new ZoneConfig();
        private static long currentFrame;

        private static Dictionary<long, bool> turnOffCache = new Dictionary<long, bool>();
        private IMyThrust myThrust;
        private double altiude;
        private bool turnOff = false;
        private int slowPollCounter = 0;
        private Vector3D myPosition;
        private MyPlanet closestPlanet;
        private ZoneConfig.PlanetInfo planetInfo;
        private List<MySafeZone> tmpBuffer = new List<MySafeZone>();//move

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            myThrust = Entity as IMyThrust;

            myThrust.EnabledChanged += MyThrust_EnabledChanged;
        }

        private void MyThrust_EnabledChanged(IMyTerminalBlock obj)
        {
            if (turnOff)
            {
                myThrust.Enabled = false;
                Log.Debug("Turned off");
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            base.UpdateOnceBeforeFrame();
            if (config != null && !config.ConfigLoaded)
            {
                config = config.LoadSettings();
            }
            Log.DebugLog = config.DebugLog;

            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            if (slowPollCounter > 0)
            {
                slowPollCounter--;
                //Log.Debug($"Update100 slowpoll. {slowPollCounter}");

                return;
            }

            if (MyAPIGateway.Session.GameplayFrameCounter > currentFrame + 200)
            {
                currentFrame = MyAPIGateway.Session.GameplayFrameCounter;
                turnOffCache.Clear();
                Log.Debug($"Cache Flushed, Grid={myThrust.CubeGrid.DisplayName} Frame={currentFrame}");
            }

            //get turnOff from cache
            FetchCachedTurnOff();

            if (turnOff)
            {
                myThrust.Enabled = false;
            }
        }

        private void FetchCachedTurnOff()
        {
            try
            {
                if (turnOffCache.TryGetValue(myThrust.CubeGrid.EntityId, out turnOff))
                {
                    Log.Debug($"Cache hit Grid={myThrust.CubeGrid.DisplayName} ");
                    return;
                }
                Log.Debug($"Cache miss Grid={myThrust.CubeGrid.DisplayName} ");
            }
            catch (NullReferenceException ex)
            {
                Log.Msg($"NullReference in FetchCachedTurnOff() part 1: {ex}");
            }

            //not found calculate value
            try
            {
                myPosition = myThrust.CubeGrid.GetPosition();
                if (MyAPIGateway.GravityProviderSystem.CalculateNaturalGravityInPoint(myPosition).LengthSquared() == 0f)
                {
                    SlowMode();
                    Log.Debug($"No Gravity, Grid={myThrust.CubeGrid.DisplayName} turnOff={turnOff} ");
                    return;
                }
            }
            catch (NullReferenceException ex)
            {
                Log.Msg($"NullReference in FetchCachedTurnOff() part 2: {ex}");
            }

            try
            {
                closestPlanet = MyGamePruningStructure.GetClosestPlanet(myPosition); //in gravity find planet
                if (closestPlanet != null && closestPlanet.Generator != null)
                {
                    if (!config.TryGetPlanet(closestPlanet.Generator.FolderName, out planetInfo)) //planet not found go back to slowPoll
                    {
                        SlowMode();
                        Log.Debug($"Grid={myThrust.CubeGrid.DisplayName} No Planet match");
                        return;
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                Log.Msg($"NullReference in FetchCachedTurnOff() part 3: {ex}");
            }

            try
            {
                altiude = closestPlanet.GetHeightFromSurface(myThrust.CubeGrid.GetPosition());

                //turnOff, bellow altitude and not in zone;
                turnOff = NotInSafeZone() && altiude < (double)planetInfo.Altitude;
                turnOffCache[myThrust.CubeGrid.EntityId] = turnOff;

                if (turnOff)
                    SlowMode();

                Log.Debug($"Grid={myThrust.CubeGrid.DisplayName} thruster={myThrust.DisplayNameText} turnOff={turnOff} altitude={altiude} AltidudeLimit={planetInfo.Altitude}");
            }
            catch (NullReferenceException ex)
            {
                Log.Msg($"NullReference in FetchCachedTurnOff() part 4: {ex}");
            }
        }

        public override void OnRemovedFromScene()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            NeedsUpdate = MyEntityUpdateEnum.NONE;
            myThrust.EnabledChanged -= MyThrust_EnabledChanged;
        }

        private bool NotInSafeZone()
        {
            tmpBuffer.Clear();
            return !MySessionComponentSafeZones.IsInSafezone(myThrust.CubeGrid.EntityId, MySessionComponentSafeZones.GetSafeZonesInAABB(myThrust.CubeGrid.WorldAABB, tmpBuffer));
        }

        private void SlowMode()
        {
            slowPollCounter = SlowPollPeriod;
        }
    }
}
