using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.Entity;
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
        private bool zoneCreated;
        private MyEntity zoneId;
        private MyEntity zoneId2;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

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

            CreateZone();

            //var tmp = zoneId as MySafeZone;
            //Log.Msg(tmp.CurrentTexture.String);
        }

        public void CreateZone()
        {
            if (zoneCreated)
            {
                return;
            }
            zoneCreated = true;

            var colour = new Vector3D(255, 0, 0);

            var position = myBeacon.GetPosition();
            zoneId = MySessionComponentSafeZones.CrateSafeZone(MatrixD.CreateWorld(position), MySafeZoneShape.Sphere, MySafeZoneAccess.Blacklist, null, null, 10f, true, true, colour, "SafeZone_Texture_Disco", 0L, "Test1");

            var position2 = position;
            position2 = position + myBeacon.WorldMatrix.Up * 20.01;
            zoneId2 = MySessionComponentSafeZones.CrateSafeZone(MatrixD.CreateWorld(position2), MySafeZoneShape.Sphere, MySafeZoneAccess.Blacklist, null, null, 10f, true, true, colour, "SafeZone_Texture_Disco", 0L, "Test2");


            Log.Msg($"Zone created: {zoneCreated}");

        }

        public override void OnRemovedFromScene()
        {
            if (zoneId != null)
            {

                MySessionComponentSafeZones.RequestDeleteSafeZone(zoneId.EntityId);

            }
            if (zoneId2 != null)
            {
                MySessionComponentSafeZones.RequestDeleteSafeZone(zoneId2.EntityId);
            }
        }
    }
}
