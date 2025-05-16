using ESThrustKiller.Configuration;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace ESThrustKiller.ZoneThrust
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false)]
    public class ZoneThrust : MyGameLogicComponent
    {
        const int SlowPollPeriod = 10;
        private static ZoneConfig config = new ZoneConfig();
        private static long currentFrame;
        private IMyThrust myThrust;
        private bool turnOff = false;
        private int slowPollCounter = 0;
        private Vector3D myPosition;
        private MyPlanet closestPlanet;
        private ZoneConfig.PlanetInfo planetInfo;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            Log.Msg("Init.");
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
            base.UpdateOnceBeforeFrame();
            Log.Msg("once");
            if (config != null && !config.ConfigLoaded)
            {
                config = config.LoadSettings();

                /*                foreach (var planet in Config.PlanetNames)
                                {
                                    Log.Msg($">>>>{planet}<<<<");
                                }*/
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
                Log.Msg($"Update100 slowpoll. {slowPollCounter}");

                return;
            }

            //string planet = MyVisualScriptLogicProvider.GetNearestPlanet(myThrust.CubeGrid.GetPosition());

            myPosition = myThrust.CubeGrid.GetPosition();

            if (MyAPIGateway.GravityProviderSystem.CalculateNaturalGravityInPoint(myPosition).LengthSquared() == 0f)
            {
                Log.Msg($"No Gravity Grid={myThrust.CubeGrid.DisplayName} turnOff={turnOff} ");
                return;
            }

            string planetName = "";
            closestPlanet = MyGamePruningStructure.GetClosestPlanet(myPosition);
            if (closestPlanet != null && closestPlanet.Generator != null)
            {
                planetName = closestPlanet.Generator.FolderName;
            }

            if (!config.TryGetPlanet(planetName, out planetInfo))
            {
                slowPollCounter = SlowPollPeriod;
                return;
            }

            int Frame = MyAPIGateway.Session.GameplayFrameCounter;
            if (Frame > currentFrame)
            {
                currentFrame = Frame;
                //clear cache
            }

            // get cached altitude
            var altiude = GetAltitude();

            //turnOff = bellow altitude;

            turnOff = altiude < (double)planetInfo.Altitude;

            Log.Msg($"Grid={myThrust.CubeGrid.DisplayName} Planet={planetName} turnOff={turnOff} altitude={altiude} AltidudeLimit={planetInfo.Altitude}");

            //var inGrav = MyAPIGateway.GravityProviderSystem.IsPositionInNaturalGravity(myThrust.CubeGrid.GetPosition());
            //MyPlanet tmp = MyGamePruningStructure.GetClosestPlanet(myThrust.CubeGrid.GetPosition());
            //var height = tmp.GetHeightFromSurface(myThrust.CubeGrid.GetPosition());

        }


        private double GetAltitude()
        {
            return closestPlanet.GetHeightFromSurface(myThrust.CubeGrid.GetPosition());
        }
    }
}
