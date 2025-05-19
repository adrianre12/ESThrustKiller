using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace ESThrustKiller.ZoneBeacon
{

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "LargeBlockZoneBeacon")]
    public class ZoneBeaconGameLogic : MyGameLogicComponent
    {
        const string VisualTexture = "SafeZone_Texture_Disco";

        public static Guid ZoneIdsKey = new Guid("0a1db65e-a169-4cf2-9a83-8903add9ca26");

        private IMyBeacon myBeacon;

        private MySafeZone zone1;
        private MySafeZone zone2;

        private float width = 10f;
        private float height = 10f;
        private float vertOffset = 2.5f;
        private int numZones = 2;
        private List<long> zoneIds = new List<long>();
        private bool zonesCreated;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            Log.Msg("Init.");
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            myBeacon = Entity as IMyBeacon;

            if (myBeacon.Storage == null)
                myBeacon.Storage = new MyModStorageComponent();

            myBeacon.EnabledChanged += MyBeacon_EnabledChanged;
        }

        private void MyBeacon_EnabledChanged(IMyTerminalBlock obj)
        {
            if (!zonesCreated)
            {
                return;
            }

            if (myBeacon.Enabled)
            {
                Log.Msg("Enabled");
                CreateZones();
            }
            else
            {
                Log.Msg("Disabled");
                RemoveZones();
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (myBeacon?.CubeGrid?.Physics == null) // ignore projected and other non-physical grids
                return;
            string tmpIdsStr;
            if (myBeacon.Storage.TryGetValue(ZoneIdsKey, out tmpIdsStr))
            {
                zoneIds.Clear();
                var tmp = tmpIdsStr.Split(',');
                foreach (var str in tmpIdsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    Int64 val = 0;
                    if (!Int64.TryParse(str, out val))
                    {
                        Log.Msg($"Failed to Parse Int64: {str}");
                        continue;
                    }
                    zoneIds.Add(val);
                }
                Log.Msg($"ZoneIDs loaded: {zoneIds.Count}");
            }
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;
            //Log.Msg("Update100.");

            if (myBeacon.Enabled)
            {
                CreateZones();
            }

        }

        public void CreateZones()
        {
            Log.Msg("CreateZones");
            if (zoneIds.Count > 0)
            {
                return;
            }
            zoneIds.Clear();

            var colour = new Vector3D(0, 255, 0);

            //var position1 = myBeacon.GetPosition() + myBeacon.WorldMatrix.Up * vertOffset;
            //zone1 = (MySafeZone)MySessionComponentSafeZones.CrateSafeZone(MatrixD.CreateWorld(position1, myBeacon.WorldMatrix.Forward, myBeacon.WorldMatrix.Up), MySafeZoneShape.Box, MySafeZoneAccess.Blacklist, null, null, 10f, true, true, colour, "SafeZone_Texture_Disco", 0L, "FlyZone1");
            //zone1.Size = new Vector3D(width, height, width);
            //var position2 = position1 + myBeacon.WorldMatrix.Up * (height + 0.01);
            //zoneId2 = MySessionComponentSafeZones.CrateSafeZone(MatrixD.CreateWorld(position2, myBeacon.WorldMatrix.Forward, myBeacon.WorldMatrix.Up), MySafeZoneShape.Box, MySafeZoneAccess.Blacklist, null, null, 10f, true, true, colour, "SafeZone_Texture_Disco", 0L, "FlyZone2");
            // ((MySafeZone)zoneId2).Size = new Vector3D(width, height, width);

            //ob.AllowedActions = CastHax(MySessionComponentSafeZones.AllowedActions, 0x3FF);

            //zone1 = (MySafeZone)CrateSafeZone(MatrixD.CreateWorld(position1, myBeacon.WorldMatrix.Forward, myBeacon.WorldMatrix.Up), MySafeZoneShape.Box, colour, "FlyZone1");

            var zoneIdsStr = new StringBuilder();
            var position = myBeacon.GetPosition() + myBeacon.WorldMatrix.Up * vertOffset;
            for (int i = 0; i < numZones; i++)
            {
                position += i * myBeacon.WorldMatrix.Up * (height + 0.01);
                var zone = (MySafeZone)CrateSafeZone(MatrixD.CreateWorld(position, myBeacon.WorldMatrix.Forward, myBeacon.WorldMatrix.Up), MySafeZoneShape.Box, colour, $"FlyZone{i + 1}");
                zoneIds.Add(zone.EntityId);
                zoneIdsStr.Append($"{zone.EntityId},");
            }
            ;
            Log.Msg($"zoneIdsStr={zoneIdsStr.ToString()}");
            myBeacon.Storage[ZoneIdsKey] = zoneIdsStr.ToString();
            //Log.Msg($"Entity.Storage[ZoneIdsKey] ={myBeacon.Storage[ZoneIdsKey]}");

            zonesCreated = true;
        }

        //public static T CastHax<T>(T typeRef, object castObj) => (T)castObj;

        public MyEntity CrateSafeZone(MatrixD transform, MySafeZoneShape safeZoneShape, Vector3 colour = default(Vector3), string name = null)
        {
            MyObjectBuilder_SafeZone myObjectBuilder_SafeZone = new MyObjectBuilder_SafeZone();
            myObjectBuilder_SafeZone.Size = new Vector3D(width, height, width);
            myObjectBuilder_SafeZone.PositionAndOrientation = new MyPositionAndOrientation(transform);
            myObjectBuilder_SafeZone.Radius = 10f;
            myObjectBuilder_SafeZone.PersistentFlags = MyPersistentEntityFlags2.InScene;
            myObjectBuilder_SafeZone.Shape = safeZoneShape;
            myObjectBuilder_SafeZone.AccessTypePlayers = MySafeZoneAccess.Blacklist;
            myObjectBuilder_SafeZone.AccessTypeFactions = MySafeZoneAccess.Blacklist; ;
            myObjectBuilder_SafeZone.AccessTypeGrids = MySafeZoneAccess.Blacklist; ;
            myObjectBuilder_SafeZone.AccessTypeFloatingObjects = MySafeZoneAccess.Blacklist;
            myObjectBuilder_SafeZone.IsVisible = true;
            myObjectBuilder_SafeZone.ModelColor = colour;
            myObjectBuilder_SafeZone.Texture = VisualTexture;
            myObjectBuilder_SafeZone.Enabled = true;
            myObjectBuilder_SafeZone.SafeZoneBlockId = 0l;
            myObjectBuilder_SafeZone.Name = (myObjectBuilder_SafeZone.DisplayName = name);
            //myObjectBuilder_SafeZone.AllowedActions = CastHax(MySessionComponentSafeZones.AllowedActions, 0x3FF);
            return MyEntities.CreateFromObjectBuilderAndAdd(myObjectBuilder_SafeZone, fadeIn: false);
        }

        private void RemoveZones()
        {
            if (zoneIds.Count == 0)
            {
                return;
            }
            foreach (var zoneId in zoneIds)
            {
                MySessionComponentSafeZones.RequestDeleteSafeZone(zoneId);
            }
            zoneIds.Clear();
        }

        public override void OnRemovedFromScene()
        {
            RemoveZones();
        }

    }
}