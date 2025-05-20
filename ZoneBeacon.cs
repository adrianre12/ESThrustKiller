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
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace ESThrustKiller.ZoneBeacon
{

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "LargeBlockZoneBeacon")]
    public class ZoneBeaconGameLogic : MyGameLogicComponent
    {
        const string VisualTexture = "SafeZone_Texture_Disco";
        const float DefaultWidth = 10f;
        const float DefaultHeight = 10f;
        const float DefaultVertOffset = 0f;
        const int DefultNumZones = 2;

        public static Guid ZoneIdsKey = new Guid("0a1db65e-a169-4cf2-9a83-8903add9ca26");

        private IMyBeacon myBeacon;
        private MyIni config = new MyIni();
        private double width;
        private double height;
        private double vertOffset;
        private int numZones;
        private List<long> zoneIds = new List<long>();

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
            if (zoneIds.Count == 0)
                return;

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
                //Log.Msg($"ZoneIDs loaded: {zoneIds.Count}");
            }
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            if (myBeacon.Enabled)
                CreateZones();
        }

        public void CreateZones()
        {
            if (zoneIds.Count > 0)
                return;
            //Log.Msg($"CreateZones ZoneIds.Count={zoneIds.Count}");

            zoneIds.Clear();

            LoadConfig();

            var colour = new Vector3D(0, 255, 0);
            var zoneIdsStr = new StringBuilder();
            var beaconPosition = myBeacon.GetPosition() + myBeacon.WorldMatrix.Up * (vertOffset - 2.5);
            for (int i = 0; i < numZones; i++)
            {
                var position = beaconPosition + i * myBeacon.WorldMatrix.Up * (height + 0.01);
                var zone = (MySafeZone)CrateSafeZone(MatrixD.CreateWorld(position, myBeacon.WorldMatrix.Forward, myBeacon.WorldMatrix.Up), MySafeZoneShape.Box, colour, $"FlyZone{i + 1}");
                zoneIds.Add(zone.EntityId);
                zoneIdsStr.Append($"{zone.EntityId},");
            }

            //Log.Msg($"ZoneIds.Count={zoneIds.Count} zoneIdsStr={zoneIdsStr.ToString()}");
            myBeacon.Storage[ZoneIdsKey] = zoneIdsStr.ToString();
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
                return;

            foreach (var zoneId in zoneIds)
            {
                MySessionComponentSafeZones.RequestDeleteSafeZone(zoneId);
            }
            zoneIds.Clear();
        }

        public override void OnRemovedFromScene()
        {
            myBeacon.EnabledChanged -= MyBeacon_EnabledChanged;
            RemoveZones();
        }

        private void LoadConfig()
        {
            if (!LoadConfigFromCD())
                CreateCDConfig();
        }

        private void CreateCDConfig()
        {
            //Log.Msg("Creating CD config");
            config.Clear();
            var sb = new StringBuilder();
            sb.AppendLine("Width: 10<value<1000 Horizontal size of each zone");
            sb.AppendLine("Height: 10<value<1000 Verical size of each zone");
            sb.AppendLine("VerticalOffset: -500<value<500 Verical displacement of each zone relative to the beacon");
            sb.AppendLine("NumberOfZones: 1<value<10");
            sb.AppendLine("Toggle enabled off/on to recreate zones.");

            config.AddSection("Settings");
            config.SetSectionComment("Settings", sb.ToString());

            width = DefaultWidth;
            config.Set("Settings", "Width", width);
            height = DefaultHeight;
            config.Set("Settings", "Height", height);
            vertOffset = DefaultVertOffset;
            config.Set("Settings", "VericalOffset", vertOffset);
            numZones = DefultNumZones;
            config.Set("Settings", "NumberOfZones", numZones);

            config.Invalidate();
            myBeacon.CustomData = config.ToString();
        }

        private double ClampDouble(double value, double min, double max)
        {
            if (value < min)
                return min;

            else if (value > max)
                return max;

            return value;
        }
        private bool LoadConfigFromCD()
        {
            //Log.Msg("LoadConfigFromCD");
            if (config.TryParse(myBeacon.CustomData))
            {
                if (!config.ContainsSection("Settings"))
                    return false;

                if (!config.Get("Settings", "Width").TryGetDouble(out width))
                    return false;
                width = ClampDouble(width, 10, 1000);

                if (!config.Get("Settings", "Height").TryGetDouble(out height))
                    return false;
                height = ClampDouble(height, 10, 1000);

                if (!config.Get("Settings", "VericalOffset").TryGetDouble(out vertOffset))
                    return false;
                vertOffset = ClampDouble(vertOffset, -500, 500);

                if (!config.Get("Settings", "NumberOfZones").TryGetInt32(out numZones))
                    return false;
                if (numZones < 1)
                    numZones = 1;
                if (numZones > 10)
                    numZones = 10;
                return true;
            }
            Log.Msg("Failed to load config");
            return false;
        }
    }
}