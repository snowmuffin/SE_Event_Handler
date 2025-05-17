// Improved readability and structure for better understanding

using Shared.Config;
using Shared.Plugin;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using Sandbox.Game.SessionComponents;
using Torch.Utils;
using System;
using System.Collections.Generic;
using Sandbox.Game.World;
using System.Linq;
using Sandbox.Game.Entities;
using VRageMath;
using VRage.Game;
using System.Reflection;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRage;

namespace TorchPlugin
{
    public class Commands : CommandModule
    {
        private static IPluginConfig Config => Common.Config;

        private void Respond(string message) => Context?.Respond(message);

 

        private void RespondWithInfo()
        {
            var config = Plugin.Instance.Config;
            Respond($"{Plugin.PluginName} plugin is enabled: {Format(config.Enabled)}");
        }

        private static string Format(bool value) => value ? "Yes" : "No";

        private static bool TryParseBool(string text, out bool result)
        {
            switch (text.ToLower())
            {
                case "1": case "on": case "yes": case "y": case "true": case "t":
                    result = true; return true;
                case "0": case "off": case "no": case "n": case "false": case "f":
                    result = false; return true;
            }
            result = false; return false;
        }


        [Command("cmd enable", "EventHandler: Enables the plugin")]
        [Permission(MyPromoteLevel.Admin)]
        public void Enable()
        {
            Config.Enabled = true;
            RespondWithInfo();
        }

        [Command("cmd disable", "EventHandler: Disables the plugin")]
        [Permission(MyPromoteLevel.Admin)]
        public void Disable()
        {
            Config.Enabled = false;
            RespondWithInfo();
        }

        [Command("cmd subcmd", "EventHandler: TODO: Subcommand")]
        [Permission(MyPromoteLevel.Admin)]
        public void SubCmd(string name, string value)
        {
            RespondWithInfo();
        }

        [Command("cmd factions", "EventHandler: Lists all factions in the game session with detailed information")]
        [Permission(MyPromoteLevel.Admin)]
        public void ListFactions()
        {
            if (MySession.Static?.Factions == null)
            {
                Respond("Factions data is not available. Ensure the game session is running.");
                return;
            }

            var factions = MySession.Static.Factions;
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
                Respond($"  Faction Type: {factionData.FactionType}");
                Respond($"  Members:");
                foreach (var member in factionData.Members)
                {
                    Respond($"    Member ID: {member.Key}, Member Info: {member.Value}");
                }

                Respond($"  Join Requests:");
                foreach (var joinRequest in factionData.JoinRequests)
                {
                    Respond($"    Request ID: {joinRequest.Key}, Request Info: {joinRequest.Value}");
                }
            }
        }

        [Command("cmd stations", "EventHandler: Lists all stations in the game session with detailed information")]
        [Permission(MyPromoteLevel.Admin)]
        public void ListStations()
        {
            if (MySession.Static?.Factions == null)
            {
                Respond("Stations data is not available. Ensure the game session is running.");
                return;
            }

            var factions = MySession.Static.Factions.Factions;
            Respond("Detailed information about stations in the current game session:");
            foreach (var faction in factions)
            {
                var factionData = faction.Value;
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
                }
            }
        }
        [Command("cmd stations gps", "Lists current stations in gps")]
        [Permission(MyPromoteLevel.Admin)]
        public void ViewStations(string factionTag = null)
        {
            if (Context.Player == null || Context.Player.SteamUserId == 0)
            {
                Context.Respond("Command cannot be used in this method.");
                return;
            }

            var factions = MySession.Static.Factions.Factions;
            foreach (var faction in factions)
            {
                var factionData = faction.Value;

                // If a faction tag is provided, skip factions that don't match
                if (!string.IsNullOrEmpty(factionTag) && !factionData.Tag.Equals(factionTag, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var stations = _stations((MyFaction)factionData);
                foreach (var station in stations)
                {
                    var stationData = station.Value;
                    var gps = MyAPIGateway.Session?.GPS.Create(
                        ($"[Station]{factionData.Name} "),
                        ($"{station.Key} "),
                        stationData.Position,
                        true
                    );
                    MyAPIGateway.Session?.GPS.AddGps(Context.Player.IdentityId, gps);
                }
            }
        }
        [Command("cmd addspacestation", "EventHandler: Adds a new station to a specific faction")]
        [Permission(MyPromoteLevel.Admin)]
        public void AddSpaceStation()
        {
            if (MySession.Static?.Factions == null)
            {
                Respond("Stations data is not available. Ensure the game session is running.");
                return;
            }
            var factions = MySession.Static.Factions.Factions;
            if (factions.Count == 0)
            {
                Respond("No factions found in the current game session.");
                return;
            }

            var random = new Random();
            var validFactions = factions.Where(f => f.Value.FactionType == MyFactionTypes.Military ||
                                                    f.Value.FactionType == MyFactionTypes.Builder ||
                                                    f.Value.FactionType == MyFactionTypes.Trader ||
                                                    f.Value.FactionType == MyFactionTypes.Miner).ToList();

            if (!validFactions.Any())
            {
                Respond("No valid factions found with the specified types.");
                return;
            }

            var randomFaction = validFactions.ElementAt(random.Next(validFactions.Count)).Value;
            var player = Context.Player;

            var controlledEntity = player.Controller.ControlledEntity?.Entity;
            if (controlledEntity == null)
            {
                Respond("You are not controlling any entity.");
                return;
            }

            var position = controlledEntity.GetPosition();

            var prefabNames = new[] { "Economy_SpaceStation_3", "Economy_SpaceStation_2", "Economy_SpaceStation_1" };
            var randomPrefabName = prefabNames[random.Next(prefabNames.Length)];

            try
            {
                var stationPosition = position + new Vector3D(
                    random.Next(400, 501),
                    random.Next(400, 501),
                    random.Next(400, 501)
                );

                if (stationPosition == Vector3D.Zero)
                {
                    Respond("Position cannot be (0,0,0). Please provide a valid position.");
                    return;
                }

                var upVector = new Vector3(0, 1, 0);
                var forwardVector = new Vector3(0, 0, 1);
                var isDeepSpace = false; 

                var newStationId = VRage.MyEntityIdentifier.AllocateId();
                var newStation = new MyStation(
                    newStationId,
                    stationPosition,
                    MyStationTypeEnum.SpaceStation,
                    (MyFaction)randomFaction,
                    randomPrefabName,
                    null,
                    upVector,
                    forwardVector,
                    isDeepSpace
                );

                var stations = _stations((MyFaction)randomFaction);
                stations[newStationId] = newStation;
                var economyComponent = MySession.Static.GetComponent<MySessionComponentEconomy>();
                if (economyComponent != null)
                {
                    var updateStationsMethod = typeof(MySessionComponentEconomy).GetMethod("UpdateStations", System.Reflection.BindingFlags.NonPublic | BindingFlags.Instance);
                    if (updateStationsMethod != null)
                    {
                        updateStationsMethod.Invoke(economyComponent, null);
                        Respond("UpdateStations method invoked successfully.");
                    }
                    else
                    {
                        Respond("Failed to find the UpdateStations method.");
                    }
                }
                else
                {
                    Respond("MySessionComponentEconomy component is not available.");
                }
                Respond($"Added new station to faction '{randomFaction.Tag}':");
                Respond($"  Station ID: {newStationId}");
                Respond($"  Prefab Name: {randomPrefabName}");
                Respond($"  Position: {stationPosition}");
                Respond($"  Up Vector: {upVector}");
                Respond($"  Forward Vector: {forwardVector}");
                Respond($"  Is Deep Space Station: {isDeepSpace}");
            }
            catch (FormatException)
            {
                Respond($"Invalid position format: '{position}'. Ensure the values are numeric and in the format 'X:<value> Y:<value> Z:<value>'.");
            }
        }

        [Command("cmd removespacestation", "EventHandler: Removes a station from a specific faction")]
        [Permission(MyPromoteLevel.Admin)]
        public void RemoveSpaceStation(long stationId)
        {
            if (MySession.Static?.Factions == null)
            {
                Respond("Stations data is not available. Ensure the game session is running.");
                return;
            }

            var factions = MySession.Static.Factions.Factions;
            if (factions.Count == 0)
            {
                Respond("No factions found in the current game session.");
                return;
            }

            foreach (var faction in factions)
            {
                var factionData = faction.Value;
                var stations = _stations((MyFaction)factionData);
                if (stations != null && stations.ContainsKey(stationId))
                {
                    stations.Remove(stationId);

                    var economyComponent = MySession.Static.GetComponent<MySessionComponentEconomy>();
                    if (economyComponent != null)
                    {
                        var updateStationsMethod = typeof(MySessionComponentEconomy).GetMethod("UpdateStations", System.Reflection.BindingFlags.NonPublic | BindingFlags.Instance);
                        if (updateStationsMethod != null)
                        {
                            updateStationsMethod.Invoke(economyComponent, null);
                            Respond("Station removed successfully and UpdateStations method invoked.");
                        }
                        else
                        {
                            Respond("Failed to find the UpdateStations method.");
                        }
                    }
                    else
                    {
                        Respond("MySessionComponentEconomy component is not available.");
                    }

                    Respond($"Removed station with ID: {stationId} from faction '{factionData.Tag}'.");
                    return;
                }
            }

            Respond($"No station found with ID: {stationId}.");
        }

        [Command("cmd prefabs", "EventHandler: Lists all prefabs in the game session")]
        [Permission(MyPromoteLevel.Admin)]
        public void ListPrefabs()
        {
            if (MySession.Static == null)
            {
                Respond("Game session is not running. Unable to retrieve prefabs.");
                return;
            }

            var prefabs = Sandbox.Definitions.MyDefinitionManager.Static.GetPrefabDefinitions();
            if (prefabs.Count == 0)
            {
                Respond("No prefabs found in the current game session.");
                return;
            }

            Respond("List of prefabs in the current game session:");
            foreach (var prefab in prefabs)
            {
                Respond($"- Prefab Name: {prefab.Value.Id.SubtypeName}");
                Respond($"  Display Name: {prefab.Value.DisplayNameString}"); 
                Respond($"  Cube Grids Count: {prefab.Value.CubeGrids?.Length ?? 0}");
            }
        }

        [Command("cmd updatestations", "EventHandler: Invokes the UpdateStations method in MySessionComponentEconomy")] 
        [Permission(MyPromoteLevel.Admin)]
        public void UpdateStationsCommand()
        {
            var economyComponent = MySession.Static.GetComponent<MySessionComponentEconomy>();
            if (economyComponent != null)
            {
                var updateStationsMethod = typeof(MySessionComponentEconomy).GetMethod("UpdateStations", System.Reflection.BindingFlags.NonPublic | BindingFlags.Instance);
                if (updateStationsMethod != null)
                {
                    updateStationsMethod.Invoke(economyComponent, null);
                    Respond("UpdateStations method invoked successfully.");
                }
                else
                {
                    Respond("Failed to find the UpdateStations method.");
                }
            }
            else
            {
                Respond("MySessionComponentEconomy component is not available.");
            }
        }

        [Command("cmd neareststation gps", "Lists the nearest station to the player's position in gps")]
        [Permission(MyPromoteLevel.None)]
        public void NearestStationGps()
        {
            if (Context.Player == null || Context.Player.SteamUserId == 0)
            {
                Context.Respond("Command cannot be used in this method.");
                return;
            }

            var playerPosition = Context.Player.GetPosition();
            var factions = MySession.Static.Factions.Factions;

            MyStation nearestStation = null;
            double nearestDistance = double.MaxValue;

            foreach (var faction in factions)
            {
                var factionData = faction.Value;
                var stations = _stations((MyFaction)factionData);
                foreach (var station in stations)
                {
                    var stationData = station.Value;
                    var distance = Vector3D.Distance(playerPosition, stationData.Position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestStation = stationData;
                    }
                }
            }

            if (nearestStation != null)
            {
                var gps = MyAPIGateway.Session?.GPS.Create(
                    ($"[Station] {nearestStation.PrefabName}"),
                    ($"Station ID: {nearestStation.Id}"),
                    nearestStation.Position,
                    true
                );
                MyAPIGateway.Session?.GPS.AddGps(Context.Player.IdentityId, gps);
                Context.Respond($"Nearest station added to GPS: {nearestStation.PrefabName} at {nearestStation.Position}");
            }
            else
            {
                Context.Respond("No stations found in the current game session.");
            }
        }

        [Command("cmd fix station", "EventHandler: Resets all station entity IDs to 0")]
        [Permission(MyPromoteLevel.Admin)]
        public void ResetStationEntityIds()
        {
            if (MySession.Static?.Factions == null)
            {
                Respond("Stations data is not available. Ensure the game session is running.");
                return;
            }
            var myEntitiesInstance = MyAPIGateway.Entities;
            var factions = MySession.Static.Factions.Factions;
            Respond("Resetting StationEntityId to 0 for all stations in the current game session:");
            foreach (var faction in factions)
            {
                var factionData = faction.Value;
                Respond($"Processing faction: {factionData.Tag}");
                var stations = _stations((MyFaction)factionData);
                if (stations == null || stations.Count == 0)
                {
                    continue;
                }
                
                foreach (var station in stations)
                {
                    var stationobjectbuilder = station.Value.GetObjectBuilder();//Station GetObjectBuilder();
                    var station_safezone_entityid = stationobjectbuilder.SafeZoneEntityId;//Station SafeZone Entity Id;
                    long stationEntitiyId = station.Value.StationEntityId;// Station EntityId;
                    IMyEntity stationEntity = myEntitiesInstance.GetEntityById(stationEntitiyId);
                    IMyEntity safezoneEntity = myEntitiesInstance.GetEntityById(station_safezone_entityid);
                    stationEntity?.Close();
                    safezoneEntity?.Close();
                    station.Value.StationEntityId = 0;
                }
            }

            var economyComponent = MySession.Static.GetComponent<MySessionComponentEconomy>();
            if (economyComponent != null)
            {
                var updateStationsMethod = typeof(MySessionComponentEconomy).GetMethod("UpdateStations", System.Reflection.BindingFlags.NonPublic | BindingFlags.Instance);
                if (updateStationsMethod != null)
                {
                    updateStationsMethod.Invoke(economyComponent, null);
                    Respond("UpdateStations method invoked successfully.");
                }
                else
                {
                    Respond("Failed to find the UpdateStations method.");
                }
            }
            else
            {
                Respond("MySessionComponentEconomy component is not available.");
            }
        }

        [Command("cmd deleteentity", "EventHandler: Deletes an entity by its ID")]
        [Permission(MyPromoteLevel.Admin)]
        public void DeleteEntityById(long entityId)
        {
            var myEntitiesInstance = MyAPIGateway.Entities;
            if (myEntitiesInstance == null)
            {
                Respond("Entity system is not available. Ensure the game session is running.");
                return;
            }

            IMyEntity entity = myEntitiesInstance.GetEntityById(entityId);
            if (entity != null)
            {
                Respond($"Found entity with ID: {entityId}, closing and deleting...");
                entity.Close();
                entity.Delete();
                Respond($"Entity with ID: {entityId} has been successfully deleted.");
            }
            else
            {
                Respond($"No entity found with ID: {entityId}.");
            }
        }

        [ReflectedGetter(Name = "m_stations", Type = typeof(MyFaction))]
        private static Func<MyFaction, Dictionary<long, MyStation>> _stations;
    }
}