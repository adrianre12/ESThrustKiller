using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace ESThrustKiller.ZoneJetpack
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_VirtualMass), false, new[] { "ESJetpackKiller" })]
    internal class ZoneJetpack : MyGameLogicComponent
    {
        const int RefreshPlayersPeriod = 10;
        const string DefaultDangerMessage = "Jetpack contamination, descend imediatly!";

        private IMyFunctionalBlock block;
        private List<IMyPlayer> players = new List<IMyPlayer>();
        private MyIni config;

        private int refreshPlayersCounter = 0;
        private MyPlanet closestPlanet;

        private double maxAllowedHieght = 0;
        private double minZoneAltitude = 0;
        private double maxZoneAltitude = 0;
        private double DefaultMinZoneAltitude = 0;
        private double DefaultMaxZoneAltitude = 0;
        private bool disableForAdmins;
        private Vector3D playerPosition;
        private double currentAltitude;
        private double currentHeight;
        private double currentAllowedHeight;
        private double blockHeight;
        private string dangerMessage;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            block = Entity as IMyFunctionalBlock;

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

        }

        private void EnabledChanged(IMyTerminalBlock obj)
        {
            if (block.Enabled)
            {
                //Log.Msg("NoJetpack Enabled");
                LoadConfig();
            }
            else
            {
                //Log.Msg("NoJetpack Disabled");
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (!MyAPIGateway.Session.IsServer || block?.CubeGrid?.Physics == null)
                return;
            if (!block.CubeGrid.IsStatic)
            {
                Log.Msg($"Error: ZoneJetpack block must be on a static grid  Grid={block.CubeGrid.DisplayName} Block={block.DisplayNameText}");
                return;
            }

            closestPlanet = MyGamePruningStructure.GetClosestPlanet(block.GetPosition());
            if (closestPlanet == null)
            {
                Log.Msg($"Error No Planet Grid={block.CubeGrid.DisplayName} Block={block.DisplayNameText}");
            }

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            block.EnabledChanged += EnabledChanged;
            blockHeight = 0;
        }
        public override void UpdateAfterSimulation100()
        {
            if (!MyAPIGateway.Session.IsServer || !block.IsFunctional || !block.Enabled)
                return;

            if (config == null)
            {
                config = new MyIni();
                LoadConfig();
            }

            //Log.Msg($"Tick {block.CubeGrid.DisplayName}");
            if (refreshPlayersCounter == 0)
            {
                RefreshPlayers();
                refreshPlayersCounter = RefreshPlayersPeriod;
            }
            else
            {
                refreshPlayersCounter--;
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
                if (disableForAdmins && player.PromoteLevel >= MyPromoteLevel.Admin)
                {
                    //Log.Msg("Disabled for Admin");
                    continue;
                }

                if (!IsPlayerStandingCharacter(player))
                    continue;

                CheckPlayerPosition(player);
            }
        }

        private void CheckPlayerPosition(IMyPlayer player)
        {
            if (!player.Character.EnabledThrusts)
                return;

            playerPosition = player.GetPosition();
            currentAltitude = Vector3D.Distance(closestPlanet.WorldMatrix.Translation, playerPosition) - blockHeight;
            if (currentAltitude > maxZoneAltitude)
            {
                //Log.Msg($"In Space currentAltitude={currentAltitude}");
                return;
            }
            currentHeight = closestPlanet.GetHeightFromSurface(playerPosition);
            currentAllowedHeight = currentAltitude < (minZoneAltitude - maxAllowedHieght) ? maxAllowedHieght : maxAllowedHieght - currentAltitude + minZoneAltitude;

            //Log.Msg($"currentAltitude={currentAltitude} minZoneAltitude={minZoneAltitude} currentHeight={currentHeight} currentAllowedHeight={currentAllowedHeight}");

            if (currentHeight < currentAllowedHeight)
            {
                //Log.Msg($"NoFly zone currentHeight={currentHeight} currentAltitude={currentAltitude}");
                if (currentHeight > 0.6 * currentAllowedHeight || currentHeight > currentAllowedHeight - 5)
                {
                    MyVisualScriptLogicProvider.ShowNotification(dangerMessage, 1200, "Red", player.IdentityId);
                    //Log.Msg($"Show notification height={currentHeight} currentAltitude={currentAltitude}");
                }
                return;
            }

            Log.Msg($"Boom {player.Character.DisplayName} currentAllowedHeight={currentAllowedHeight} height={currentHeight} currentAltitude={currentAltitude} ");
            if (player.Character.EnabledThrusts && MyVisualScriptLogicProvider.GetPlayersHydrogenLevel(player.IdentityId) > 0f)
            {
                //player.Character.Synchronized = true;
                //player.Character.SwitchThrusts();
                MyVisualScriptLogicProvider.SetPlayersHydrogenLevel(player.IdentityId, 0f);
                MyVisualScriptLogicProvider.CreateExplosion(player.GetPosition(), 0.1f, 10);
            }
        }

        public bool IsPlayerStandingCharacter(IMyPlayer player)
        {
            if (player?.Character == null || player.Controller?.ControlledEntity?.Entity == null)
                return false;
            return player.Character == player.Controller.ControlledEntity.Entity;
        }

        private void LoadConfig()
        {
            blockHeight = System.Math.Floor(Vector3D.Distance(closestPlanet.WorldMatrix.Translation, block.GetPosition()));
            if (!LoadConfigFromCD())
            {
                Log.Msg("Error in CD, creating new");
                DefaultMinZoneAltitude = 200;
                DefaultMaxZoneAltitude = closestPlanet.AtmosphereAltitude;
                CreateCDConfig();
            }
        }

        private void CreateCDConfig()
        {
            Log.Msg("Creating CD config");

            config.Clear();
            var sb = new StringBuilder();
            sb.AppendLine("The altitude of the block is the base for all altitude measurements.");
            sb.AppendLine("DangerMessage: Don't use square brackets.");
            sb.AppendLine("MaxAllowedHieght: 0<value<1000 Height allowed to fly above surface.");
            sb.AppendLine("MinZoneAltitude: 0<value<15000 Altitude at which the fly zone is capped.");
            sb.AppendLine("MaxZoneAltitude: MinZoneAltitude<value<150000 Default: Atmosphere limit. Max altitude that the block controls.");
            sb.AppendLine("Use MinZoneAltiude to stop flying on high land.");
            sb.AppendLine("Use MaxZoneAltiude to allow flying in space.");
            sb.AppendLine("Toggle enabled off/on to reload the configuration.");


            config.AddSection("Settings");
            config.SetSectionComment("Settings", sb.ToString());

            dangerMessage = DefaultDangerMessage;
            config.Set("Settings", "DangerMessage", dangerMessage);
            maxAllowedHieght = 10d;
            config.Set("Settings", "MaxAllowedHeight", maxAllowedHieght);
            minZoneAltitude = DefaultMinZoneAltitude;
            config.Set("Settings", "MinZoneAltitude", minZoneAltitude);
            maxZoneAltitude = DefaultMaxZoneAltitude;
            config.Set("Settings", "MaxZoneAltitude", maxZoneAltitude);
            disableForAdmins = false;
            config.Set("Settings", "DisableForAdmins", disableForAdmins);

            config.Invalidate();
            block.CustomData = config.ToString();
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
            Log.Msg("LoadConfigFromCD");
            if (config.TryParse(block.CustomData))
            {
                if (!config.ContainsSection("Settings"))
                    return false;

                if (!config.Get("Settings", "DangerMessage").TryGetString(out dangerMessage))
                    return false;

                if (!config.Get("Settings", "MaxAllowedHeight").TryGetDouble(out maxAllowedHieght))
                    return false;
                maxAllowedHieght = ClampDouble(maxAllowedHieght, 0, 1000);

                if (!config.Get("Settings", "MinZoneAltitude").TryGetDouble(out minZoneAltitude))
                    return false;
                minZoneAltitude = ClampDouble(minZoneAltitude, 0, 15000);

                if (!config.Get("Settings", "MaxZoneAltitude").TryGetDouble(out maxZoneAltitude))
                    return false;
                maxZoneAltitude = ClampDouble(maxZoneAltitude, minZoneAltitude, 65000);

                if (!config.Get("Settings", "DisableForAdmins").TryGetBoolean(out disableForAdmins))
                    return false;

                return true;


            }
            Log.Msg("Error: Failed to load config");
            return false;
        }
    }
}