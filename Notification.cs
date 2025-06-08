using Sandbox.Game;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;

namespace ESThrustKiller.Notification
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class Notification : MySessionComponentBase
    {
        const int DefaultTickCounter = 120; //2s
        const int DefaultRefreshPlayersCounter = 10; // in multiples of DefaultTickCounter

        public static Notification Instance; // the only way to access session comp from other classes and the only accepted static field.
        private int tickCounter;
        private NotificationConfig config;
        private int refreshPlayersCounter;
        private List<IMyPlayer> players = new List<IMyPlayer>();
        private Vector3D playerPosition;
        private List<NotificationConfig.GPS> zonePositions;
        private Dictionary<long, string> playerInZone = new Dictionary<long, string>();

        public override void LoadData()
        {
            Instance = this;
        }

        public override void BeforeStart()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            Log.Msg("ZoneNotification Start");
            tickCounter = DefaultTickCounter;
            config = new NotificationConfig();
            config = config.LoadSettings();
            zonePositions = new List<NotificationConfig.GPS>();
            foreach (var gps in config.GPSlocations)
            {
                //using squared radius to optimise distance checks
                zonePositions.Add(new NotificationConfig.GPS(gps.UniqueName, gps.Position, gps.AlertRadius * gps.AlertRadius, gps.AlertMessage, gps.AlertTimeMs));
                Log.Msg($"Adding Zone {gps.UniqueName}");
            }
        }

        protected override void UnloadData()
        {
            Instance = null; // important for avoiding this object to remain allocated in memory
        }

        public override void UpdateAfterSimulation()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            if (tickCounter > 0)
            {
                tickCounter--;
                return;
            }
            tickCounter = DefaultTickCounter;

            if (refreshPlayersCounter > 0)
            {
                refreshPlayersCounter--;
            }
            else
            {
                RefreshPlayers();
                refreshPlayersCounter = DefaultRefreshPlayersCounter;
            }

            CheckPlayerPositions();
        }

        private void RefreshPlayers()
        {
            //Log.Msg("RefreshPlayers");
            players.Clear();
            MyAPIGateway.Players.GetPlayers(players);
        }

        private void CheckPlayerPositions()
        {
            //Log.Msg("CheckPlayerPositions");
            foreach (var player in players)
            {
                CheckPlayerPosition(player);
            }
        }

        private void CheckPlayerPosition(IMyPlayer player)
        {
            playerPosition = player.GetPosition();

            double distanceSqr;
            double lastDistance = double.MaxValue;
            NotificationConfig.GPS closestZone = new NotificationConfig.GPS();
            string playerZoneName;
            bool inZone = false;
            bool sendMessage = false;
            //Log.Msg($"Position {playerPosition} zonePositions.count={zonePositions.Count}");
            foreach (var zone in zonePositions)
            {
                if (zone.AlertRadius == 0)
                    continue;

                //Log.Msg($"Zone Position {zone.Position}");
                if (playerInZone.TryGetValue(player.IdentityId, out playerZoneName))
                    inZone = playerZoneName == zone.UniqueName;

                distanceSqr = Vector3D.DistanceSquared(zone.Position, playerPosition);
                //Log.Msg($"Zone Position {zone.Position} distance={System.Math.Sqrt(distanceSqr)} inZone={inZone}");
                if (distanceSqr < zone.AlertRadius)
                {
                    if (inZone)
                    {
                        sendMessage = false;
                        break;
                    }
                    if (distanceSqr < lastDistance)
                    {
                        lastDistance = distanceSqr;
                        closestZone = zone;
                        sendMessage = true;
                        playerInZone.Add(player.IdentityId, zone.UniqueName);
                    }
                }
                else
                {
                    if (inZone)
                    {
                        playerInZone.Remove(player.IdentityId);
                        sendMessage = false;
                        break;
                    }
                }

            }

            if (sendMessage)
            {
                MyVisualScriptLogicProvider.ShowNotification(closestZone.AlertMessage, disappearTimeMs: closestZone.AlertTimeMs, font: MyFontEnum.Red, playerId: player.IdentityId);
            }
        }
    }
}
