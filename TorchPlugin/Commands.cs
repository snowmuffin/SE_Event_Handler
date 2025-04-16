using Shared.Config;
using Shared.Plugin;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using Sandbox.Game.Multiplayer;
using Torch.Utils;
using System;
using System.Collections.Generic;
using Sandbox.Game.World; // Required for faction management

namespace TorchPlugin
{
    public class Commands : CommandModule
    {
        private static IPluginConfig Config => Common.Config;

        private void Respond(string message)
        {
            Context?.Respond(message);
        }

        // TODO: Replace cmd with the name of your chat command
        // TODO: Implement subcommands as needed
        private void RespondWithHelp()
        {
            Respond("EventHandler commands:");
            Respond("  !cmd help");
            Respond("  !cmd info");
            Respond("    Prints the current configuration settings.");
            Respond("  !cmd enable");
            Respond("    Enables the plugin");
            Respond("  !cmd disable");
            Respond("    Disables the plugin");
            Respond("  !cmd subcmd <name> <value>");
            Respond("    TODO Your subcommand");
        }

        private void RespondWithInfo()
        {
            var config = Plugin.Instance.Config;
            Respond($"{Plugin.PluginName} plugin is enabled: {Format(config.Enabled)}");
            // TODO: Respond with your plugin settings
            // For example:
            //Respond($"custom_setting: {Format(config.CustomSetting)}");
        }

        // Custom formatters

        private static string Format(bool value) => value ? "Yes" : "No";

        // Custom parsers

        private static bool TryParseBool(string text, out bool result)
        {
            switch (text.ToLower())
            {
                case "1":
                case "on":
                case "yes":
                case "y":
                case "true":
                case "t":
                    result = true;
                    return true;

                case "0":
                case "off":
                case "no":
                case "n":
                case "false":
                case "f":
                    result = false;
                    return true;
            }

            result = false;
            return false;
        }

        // ReSharper disable once UnusedMember.Global

        [Command("cmd help", "EventHandler: Help")]
        [Permission(MyPromoteLevel.None)]
        public void Help()
        {
            RespondWithHelp();
        }

        // ReSharper disable once UnusedMember.Global
        [Command("cmd info", "EventHandler: Prints the current settings")]
        [Permission(MyPromoteLevel.None)]
        public void Info()
        {
            RespondWithInfo();
        }

        // ReSharper disable once UnusedMember.Global
        [Command("cmd enable", "EventHandler: Enables the plugin")]
        [Permission(MyPromoteLevel.Admin)]
        public void Enable()
        {
            Config.Enabled = true;
            RespondWithInfo();
        }

        // ReSharper disable once UnusedMember.Global
        [Command("cmd disable", "EventHandler: Disables the plugin")]
        [Permission(MyPromoteLevel.Admin)]
        public void Disable()
        {
            Config.Enabled = false;
            RespondWithInfo();
        }

        // TODO: Subcommand
        // ReSharper disable once UnusedMember.Global
        [Command("cmd subcmd", "EventHandler: TODO: Subcommand")]
        [Permission(MyPromoteLevel.Admin)]
        public void SubCmd(string name, string value)
        {
            // TODO: Process command parameters (for example name and value)

            RespondWithInfo();
        }

        // ReSharper disable once UnusedMember.Global
        [Command("cmd factions", "EventHandler: Lists all factions in the game session with detailed information")]
        [Permission(MyPromoteLevel.None)]
        public void ListFactions()
        {
            if (Sandbox.Game.World.MySession.Static == null || Sandbox.Game.World.MySession.Static.Factions == null)
            {
                Respond("Factions data is not available. Ensure the game session is running.");
                return;
            }

            var factions = Sandbox.Game.World.MySession.Static.Factions;
            if (factions.Factions.Count == 0)
            {
                Respond("No factions found in the current game session.");
                return;
            }

            Respond("Detailed information about factions in the current game session:");
            foreach (var faction in factions.Factions)
            {
                var factionData = faction.Value;
                Respond($"- Tag: {factionData.Tag}");
                Respond($"  Name: {factionData.Name}");
                Respond($"  Founder ID: {factionData.FounderId}");
                Respond($"  Description: {factionData.Description}");
                Respond($"  Score: {factionData.Score}");
                Respond($"  Objective Completion: {factionData.ObjectivePercentageCompleted}%");
                Respond($"  Members Count: {factionData.Members.Count}");
                Respond($"  Accepts Humans: {factionData.AcceptHumans}");
                Respond($"  Auto Accept Peace: {factionData.AutoAcceptPeace}");
                Respond($"  Auto Accept Member: {factionData.AutoAcceptMember}");
                Respond($"  Custom Color: {factionData.CustomColor}");
                Respond($"  Icon Color: {factionData.IconColor}");
                Respond($"  Private Info: {factionData.PrivateInfo}");
                Respond($"  Faction Type: {factionData.FactionType}");
                Respond($"  Balance: {factionData.GetBalanceShortString()}");

                // Check if the faction has the last member
                Respond($"  Has Last Member: {factionData.HasLastMember()}");

                // Output members
                Respond($"  Members:");
                foreach (var member in factionData.Members)
                {
                    Respond($"    Member ID: {member.Key}, Member Info: {member.Value}");
                }

                // Output join requests
                Respond($"  Join Requests:");
                foreach (var joinRequest in factionData.JoinRequests)
                {
                    Respond($"    Request ID: {joinRequest.Key}, Request Info: {joinRequest.Value}");
                }
            }
        }

        // ReSharper disable once UnusedMember.Global
        [Command("cmd stations", "EventHandler: Lists all stations in the game session with detailed information")]
        [Permission(MyPromoteLevel.None)]
        public void ListStations()
        {
            if (Sandbox.Game.World.MySession.Static == null || Sandbox.Game.World.MySession.Static.Factions == null)
            {
                Respond("Stations data is not available. Ensure the game session is running.");
                return;
            }

            var factions = Sandbox.Game.World.MySession.Static.Factions.Factions;

            Respond("Detailed information about stations in the current game session:");
            foreach (var faction in factions)
            {
                var factionData = faction.Value;

                // Use the reflected getter to access the stations
                var stations = _stations((MyFaction)factionData);
                if (stations == null || stations.Count == 0)
                {
                    Respond($"Faction '{factionData.Tag}' has no stations.");
                    continue;
                }

                Respond($"Faction '{factionData.Tag}' Stations:");
                foreach (var station in stations)
                {
                    var stationData = station.Value;
                    Respond($"  Station ID: {station.Key}");
                    Respond($"    Faction ID: {factionData.FactionId}");
                    Respond($"    Prefab Name: {stationData.PrefabName}");
                    Respond($"    Type: {stationData.Type}");
                    Respond($"    Position: {stationData.Position}");
                    Respond($"    Is Deep Space Station: {stationData.IsDeepSpaceStation}");
                    Respond($"    Is On Planet With Atmosphere: {stationData.IsOnPlanetWithAtmosphere}");
                    Respond($"    Station Entity ID: {stationData.StationEntityId}");
                }
            }
        }

        // Reflected getter for accessing stations
        [ReflectedGetter(Name = "m_stations", Type = typeof(MyFaction))]
        private static Func<MyFaction, Dictionary<long, MyStation>> _stations;
    }
}