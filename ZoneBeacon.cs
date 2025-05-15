using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace ESThrustKiller.ZoneBeacon
{

    public struct PositionInfo
    {
        public Vector3D Position;
        public float Radius;

        public PositionInfo(Vector3D position, float radius)
        {
            this.Position = position;
            this.Radius = radius;
        }
    }


    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "LargeBlockZoneBeacon")]
    public class ZoneBeaconGameLogic : MyGameLogicComponent
    {
        private IMyBeacon myBeacon;
        private bool debugLog = true;

        public static List<PositionInfo> Positions = new List<PositionInfo>();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            Log.DebugLog = true;

            Log.Msg("Init.");
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            myBeacon = Entity as IMyBeacon;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (myBeacon?.CubeGrid?.Physics == null) // ignore projected and other non-physical grids
                return;
            Log.Msg("Store position.");
            Positions.Add(new PositionInfo(myBeacon.CubeGrid.GetPosition(), myBeacon.Radius));
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;
            Log.Msg("Update100.");

        }
    }
}
