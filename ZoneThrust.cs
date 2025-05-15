using ESThrustKiller.Configuration;
using ESThrustKiller.ZoneBeacon;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace ESThrustKiller.ZoneThrust
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false)]
    public class ZoneThrust : MyGameLogicComponent
    {
        private IMyThrust myThrust;
        private static ZoneConfig config;
        private bool enabledState;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            Log.DebugLog = true;

            Log.Msg("Init.");
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            myThrust = Entity as IMyThrust;

            myThrust.EnabledChanged += MyThrust_EnabledChanged;
        }

        private void MyThrust_EnabledChanged(IMyTerminalBlock obj)
        {
            if (enabledState == false)
            {
                myThrust.Enabled = false;
                Log.Msg("Turned off");
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            config = new ZoneConfig();
            config.LoadSettings();

            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;
            Log.Msg("Update100.");
            var myPosition = myThrust.CubeGrid.GetPosition();

            foreach (var position in ZoneBeaconGameLogic.Positions)
            {
                Log.Msg($"Position = {position.Position.ToString()} Radius = {position.Radius} Distance = {VRageMath.Vector3D.Distance(myPosition, position.Position)} ");
            }


            string planet = MyVisualScriptLogicProvider.GetNearestPlanet(myPosition);


        }
    }
}
