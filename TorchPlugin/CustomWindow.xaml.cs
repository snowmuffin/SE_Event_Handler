using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Shared.Plugin;
using Sandbox.Game.World;
using Torch.Utils;
using Sandbox.Definitions;
using VRage.Game.ModAPI;
using System.Reflection;
using VRage.Game;
using SpaceEngineers.Game.SessionComponents;
using System.Collections.Concurrent;
using SpaceEngineers.Game.EntityComponents.GameLogic;
namespace TorchPlugin
{
    public class FactionReputation
    {
        public long PlayerId { get; set; }
        public string PlayerName { get; set; }
        public int Reputation { get; set; }
    }
    /// <summary>
    /// Interaction logic for CustomWindow.xaml
    /// </summary>
    public partial class CustomWindow : Window
    {
        private readonly CustomInstance _customInstance;

        [ReflectedGetter(Name = "m_stations", Type = typeof(MyFaction))]
        private static Func<MyFaction, Dictionary<long, MyStation>> _stations;

        public MyPlayer SelectedPlayer { get; set; }
        public MySpawnGroupDefinition SelectedSpawnGroup { get; set; }

        public CustomWindow(CustomInstance customInstance)
        {
            _customInstance = customInstance;
            InitializeComponent();
            this.Closed += OnWindowClosed;
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            _customInstance?.Stop();
        }

        // Refresh 버튼 클릭 시 플레이어와 스폰 그룹 목록을 갱신합니다.
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadFactionsData();
                LoadGlobalEventFactoryData();
                LoadMyGlobalEventsData();
                LoadGlobalEncountersData();
                LoadNeutralShipSpawnerData();
                LoadPlayerDropdownData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Exception in RefreshButton_Click: {ex}");
                MessageBox.Show($"Exception: {ex.Message}\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            LoadActiveEncountersData();
        }

        // Factions 데이터 로드 분리
        private void LoadFactionsData()
        {
            if (MySession.Static?.Factions == null)
            {
                MessageBox.Show("Factions data is not available. Ensure the game session is running.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine("[DEBUG] MySession.Static.Factions is null");
                return;
            }
            System.Diagnostics.Debug.WriteLine("[DEBUG] Factions loaded: " + MySession.Static.Factions.Factions.Count);
            var factionsRaw = MySession.Static.Factions.Factions.Values;
            if (factionsRaw == null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Factions.Values is null");
                FactionsList.ItemsSource = null;
            }
            else
            {
                var factions = factionsRaw.Select(f => new
                {
                    f.FactionId,
                    f.Tag,
                    f.Name,
                    f.FounderId,
                    f.Description,
                    f.Score,
                    ObjectivePercentageCompleted = f.ObjectivePercentageCompleted,
                    MembersCount = f.Members.Count,
                    f.AcceptHumans,
                    f.AutoAcceptPeace,
                    f.AutoAcceptMember,
                    f.CustomColor,
                    f.IconColor,
                    f.FactionType
                }).ToList();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] FactionsList.ItemsSource set: {factions.Count}");
                FactionsList.ItemsSource = factions;
            }
        }

        // Player 목록 로드 분리
        private void LoadPlayerDropdownData()
        {
            Type m_playersBufferType = typeof(MyNeutralShipSpawner);
            FieldInfo m_playersBufferfield = m_playersBufferType.GetField("m_playersBuffer", BindingFlags.NonPublic | BindingFlags.Static);
            if (m_playersBufferfield == null)
            {
                MessageBox.Show("m_playersBuffer field not found.");
                System.Diagnostics.Debug.WriteLine("[DEBUG] m_playersBuffer field not found");
                return;
            }
            List<MyPlayer> m_playersBuffer = m_playersBufferfield.GetValue(null) as List<MyPlayer>;
            if (m_playersBuffer == null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] m_playersBuffer is null");
                PlayerDropdown.ItemsSource = null;
            }
            else
            {
                var players = m_playersBuffer.Select(player => new
                {
                    player.DisplayName,
                    player.IsBot,
                }).ToList();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] PlayerDropdown.ItemsSource set: {players.Count}");
                PlayerDropdown.ItemsSource = players;
                PlayerDropdown.DisplayMemberPath = "DisplayName";
            }
        }

        private void FactionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedFaction = FactionsList.SelectedItem;
            if (selectedFaction != null)
            {
                var factionId = (long)selectedFaction.GetType().GetProperty("FactionId")?.GetValue(selectedFaction);
                var faction = MySession.Static.Factions.Factions.Values.FirstOrDefault(f => f.FactionId == factionId);
                if (faction != null)
                {
                    MembersGrid.ItemsSource = faction.Members.Select(m => new
                    {
                        MemberId = m.Key,
                        IsLeader = m.Value.IsLeader,
                        IsFounder = m.Value.IsFounder
                    }).ToList();

                    if (MySession.Static?.Factions != null)
                    {
                        var stations = _stations((MyFaction)faction);
                        if (stations != null && stations.Count > 0)
                        {
                            StationsGrid.ItemsSource = stations.Select(station => new
                            {
                                StationId = station.Key,
                                FactionId = faction.FactionId,
                                station.Value.PrefabName,
                                station.Value.Type,
                                station.Value.Position,
                                station.Value.Up,
                                station.Value.Forward,
                                station.Value.IsDeepSpaceStation,
                                station.Value.IsOnPlanetWithAtmosphere,
                                station.Value.StationEntityId,
                                SAFEZONE_SIZE = MyStation.SAFEZONE_SIZE
                            }).ToList();
                        }
                        else
                        {
                            StationsGrid.ItemsSource = null;
                        }
                    }

                    // Display reputation data
                    DisplayFactionReputation(factionId);
                }
            }
        }

        private void StationsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedStation = StationsGrid.SelectedItem;
            if (selectedStation != null)
            {
                var stationId = (long)selectedStation.GetType().GetProperty("StationId")?.GetValue(selectedStation);
                var factionId = (long)selectedStation.GetType().GetProperty("FactionId")?.GetValue(selectedStation);

                var faction = MySession.Static.Factions.Factions.Values.FirstOrDefault(f => f.FactionId == factionId);
                if (faction != null)
                {
                    var stations = _stations((MyFaction)faction);
                    if (stations != null && stations.TryGetValue(stationId, out var station))
                    {
                        StoreItemsGrid.ItemsSource = station.StoreItems.Select(item => new
                        {
                            ItemId = item.Id,
                            ItemName = item.Item.HasValue ? item.Item.Value.SubtypeId.ToString() : "",
                            ItemType = item.ItemType,
                            PricePerUnit = item.PricePerUnit,
                            Amount = item.Amount,
                            IsActive = item.IsActive,
                            StoreItemType = item.StoreItemType,
                            PrefabName = item.PrefabName,
                            PrefabTotalPcu = item.PrefabTotalPcu,
                            PricePerUnitDiscount = item.PricePerUnitDiscount,
                            RemovedAmount = item.RemovedAmount,
                            UpdateCount = item.UpdateCount,
                            IsCustomStoreItem = item.IsCustomStoreItem
                        }).ToList();
                    }
                }
            }
        }

        // Updated the data binding to use the FactionReputation model instead of an anonymous type.
        private void DisplayFactionReputation(long factionId)
        {
            if (MySession.Static?.Players == null || MySession.Static?.Factions == null)
            {
                MessageBox.Show("Player or Faction data is not available. Ensure the game session is running.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var allPlayers = MySession.Static.Players.GetAllIdentities();
            var reputationData = allPlayers.Select(player =>
            {
                var identityId = player.IdentityId;
                var reputation = MySession.Static.Factions.GetRelationBetweenPlayerAndFaction(identityId, factionId);
                return new FactionReputation
                {
                    PlayerId = identityId,
                    PlayerName = player.DisplayName,
                    Reputation = reputation.Item2 // Extracting the reputation value
                };
            }).ToList();

            ReputationGrid.ItemsSource = reputationData;
        }

        private void ReputationGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header.ToString() == "Reputation")
            {
                var editedElement = e.EditingElement as TextBox;
                if (editedElement != null && long.TryParse(editedElement.Text, out var newReputation))
                {
                    var selectedItem = e.Row.Item;
                    var playerId = (long)selectedItem.GetType().GetProperty("PlayerId")?.GetValue(selectedItem);
                    var factionId = (long)FactionsList.SelectedItem.GetType().GetProperty("FactionId")?.GetValue(FactionsList.SelectedItem);

                    // Update the reputation in the game with 'None' as the reason
                    MySession.Static.Factions.SetReputationBetweenPlayerAndFaction(playerId, factionId, (int)newReputation, ReputationChangeReason.None);
                }
            }
        }

        private void LoadMyGlobalEventsData()
        {
            var globalEvents = typeof(MyGlobalEvents).GetField("m_globalEvents", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as SortedSet<MyGlobalEventBase>;

            if (globalEvents == null)
            {
                MessageBox.Show("Unable to retrieve data from MyGlobalEvents.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var eventData = globalEvents.Select(e => new
            {
                EventId = e.Definition.Id.ToString(),
                EventName = e.Definition.DisplayNameString ?? e.Definition.Id.SubtypeName,
                ActivationTime = e.ActivationTime,
                IsEnabled = e.Enabled,
                IsPeriodic = e.IsPeriodic
            }).ToList();

            MyGlobalEventsGrid.ItemsSource = eventData;
        }

        private void LoadGlobalEventFactoryData()
        {
            var eventFactoryType = typeof(MyGlobalEventFactory);

            // Retrieve the private static fields using reflection
            var typesToHandlersField = eventFactoryType.GetField("m_typesToHandlers", BindingFlags.NonPublic | BindingFlags.Static);
            var globalEventFactoryField = eventFactoryType.GetField("m_globalEventFactory", BindingFlags.NonPublic | BindingFlags.Static);

            if (typesToHandlersField == null || globalEventFactoryField == null)
            {
                MessageBox.Show("Unable to retrieve data from MyGlobalEventFactory.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Get the values of the fields
            var typesToHandlers = typesToHandlersField.GetValue(null) as Dictionary<MyDefinitionId, MethodInfo>;
            var globalEventFactory = globalEventFactoryField.GetValue(null);

            if (typesToHandlers == null)
            {
                MessageBox.Show("No event handlers found.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Prepare data for display
            var eventData = typesToHandlers.Select(entry => new
            {
                EventDefinitionId = entry.Key.ToString(),
                HandlerMethod = entry.Value.Name
            }).ToList();

            GlobalEventsGrid.ItemsSource = eventData;
        }

        private void LoadGlobalEncountersData()
        {
            var generatorInstance = MySession.Static.GetComponent<MyGlobalEncountersGenerator>();

            if (generatorInstance == null)
            {
                MessageBox.Show("MyGlobalEncountersGenerator instance not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Use reflection to retrieve the m_spawnGroups field
            var spawnGroupsField = typeof(MyGlobalEncountersGenerator).GetField("m_spawnGroups", BindingFlags.NonPublic | BindingFlags.Instance);

            if (spawnGroupsField == null)
            {
                MessageBox.Show("Unable to retrieve m_spawnGroups field.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var spawnGroups = spawnGroupsField.GetValue(generatorInstance) as MySpawnGroupDefinition[];

            if (spawnGroups == null || spawnGroups.Length == 0)
            {
                MessageBox.Show("No spawn groups found.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Prepare data for display with detailed columns
            var spawnGroupData = spawnGroups.Select(group => new
            {
                GroupId = group.Id.ToString(),
                GroupName = group.DisplayNameText,
                Frequency = group.Frequency,
                IsEncounter = group.IsEncounter,
                IsGlobalEncounter = group.IsGlobalEncounter,
                IsCargoShip = group.IsCargoShip,
                EnableTradingStationVisit = group.EnableTradingStationVisit,
                ReactorsOn = group.ReactorsOn,
                EnableNpcResources = group.EnableNpcResources,
                RandomizedPaint = group.RandomizedPaint,
                MinFactionSubEncounters = group.MinFactionSubEncounters,
                MaxFactionSubEncounters = group.MaxFactionSubEncounters,
                MinHostileSubEncounters = group.MinHostileSubEncounters,
                MaxHostileSubEncounters = group.MaxHostileSubEncounters
            }).ToList();

            GlobalEncountersGrid.ItemsSource = spawnGroupData;
        }

        private void LoadNeutralShipSpawnerData()
        {
            FieldInfo spawnGroupsField = typeof(MyNeutralShipSpawner).GetField("m_spawnGroups", BindingFlags.NonPublic | BindingFlags.Static);

            if (spawnGroupsField == null)
            {
                MessageBox.Show("Field 'm_spawnGroups' not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            List<MySpawnGroupDefinition>  spawnGroups = spawnGroupsField?.GetValue(null) as List<MySpawnGroupDefinition>;

            if (spawnGroups == null || spawnGroups.Count == 0)
            {
                MessageBox.Show("No spawn groups found.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var spawnGroupData = spawnGroups.Select(group => new
            {
                GroupId = group.Id.ToString(),
                GroupName = group.DisplayNameText,
                Frequency = group.Frequency,
                IsEncounter = group.IsEncounter,
                IsGlobalEncounter = group.IsGlobalEncounter,
                IsCargoShip = group.IsCargoShip,
                EnableTradingStationVisit = group.EnableTradingStationVisit,
                ReactorsOn = group.ReactorsOn,
                EnableNpcResources = group.EnableNpcResources,
                RandomizedPaint = group.RandomizedPaint,
                MinFactionSubEncounters = group.MinFactionSubEncounters,
                MaxFactionSubEncounters = group.MaxFactionSubEncounters,
                MinHostileSubEncounters = group.MinHostileSubEncounters,
                MaxHostileSubEncounters = group.MaxHostileSubEncounters,
            }).ToList();

            SpawnGroupGrid.ItemsSource = spawnGroupData;

            SpawnGroupDropdown.ItemsSource = spawnGroupData;
            SpawnGroupDropdown.DisplayMemberPath = "GroupName";
            

        }

        private void LoadActiveEncountersData()
        {
            var generatorInstance = MySession.Static.GetComponent<MyGlobalEncountersGenerator>();
            if (generatorInstance == null)
            {
                MessageBox.Show("MyGlobalEncountersGenerator instance not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // m_encounterComponents: ConcurrentDictionary<long, HashSet<MyGlobalEncounterComponent>>
            var encounterComponentsField = typeof(MyGlobalEncountersGenerator).GetField("m_encounterComponents", BindingFlags.NonPublic | BindingFlags.Instance);
            if (encounterComponentsField == null)
            {
                MessageBox.Show("Unable to retrieve m_encounterComponents field.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var encounterComponents = encounterComponentsField.GetValue(generatorInstance) as ConcurrentDictionary<long, HashSet<MyGlobalEncounterComponent>>;
            if (encounterComponents == null || encounterComponents.Count == 0)
            {
                GlobalEncountersGrid.ItemsSource = null;
                return;
            }
            var encounterList = new List<object>();
            foreach (var kv in encounterComponents)
            {
                long encounterId = kv.Key;
                foreach (var comp in kv.Value)
                {
                    var entity = comp.Entity;
                    encounterList.Add(new {
                        EncounterId = encounterId,
                        comp.SpawnGroupName,
                        EntityId = entity?.EntityId ?? 0,
                        EntityType = entity?.GetType().Name ?? "-",
                        Position = entity is VRage.Game.Entity.MyEntity ent ? ent.PositionComp?.GetPosition().ToString() : "-"
                    });
                }
            }
            ActiveGlobalEncountersGrid.ItemsSource = encounterList;
        }

        private void SpawnButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPlayer = PlayerDropdown.SelectedItem as MyPlayer;
            var selectedGroup = SpawnGroupDropdown.SelectedItem as MySpawnGroupDefinition;
            // Check if both selections are valid
            if (selectedPlayer == null)
            {
                MessageBox.Show("Please select both a player and a spawn group.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }



        }

        private void RemoveEncounterButton_Click(object sender, RoutedEventArgs e)
        {
            var generatorInstance = MySession.Static.GetComponent<MyGlobalEncountersGenerator>();

            if (sender is Button btn && btn.Tag is long encounterId)
            {
                RemoveGlobalEncounter(encounterId);
                LoadActiveEncountersData(); // Refresh the grid after removal
            }
        }

        // Use reflection to call the private RemoveGlobalEncounter method in MyGlobalEncountersGenerator
        private void RemoveGlobalEncounter(long encounterId)
        {
            var generatorInstance = MySession.Static.GetComponent<MyGlobalEncountersGenerator>();
            if (generatorInstance == null)
            {
                MessageBox.Show("MyGlobalEncountersGenerator instance not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var method = typeof(MyGlobalEncountersGenerator).GetMethod("RemoveGlobalEncounter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method == null)
            {
                MessageBox.Show("RemoveGlobalEncounter method not found via reflection.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            method.Invoke(generatorInstance, new object[] { encounterId });
        }
    }
}