using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Reflection;
using Sandbox.Game.World;
using SpaceEngineers.Game.SessionComponents;
using VRage.Game;
using Sandbox.Definitions;
using SpaceEngineers.Game.EntityComponents.GameLogic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace TorchPlugin
{
    public partial class CustomWindow : Window
    {
        private ObservableCollection<object> _activeEncounterCollection = new ObservableCollection<object>();
        private CollectionViewSource _activeEncounterViewSource;

        // Encounters 탭 관련 함수 및 이벤트 핸들러
        private void LoadGlobalEncountersData()
        {
            var generatorInstance = MySession.Static.GetComponent<MyGlobalEncountersGenerator>();

            if (generatorInstance == null)
            {
                MessageBox.Show("MyGlobalEncountersGenerator instance not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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
        private void RemoveEncounterButton_Click(object sender, RoutedEventArgs e)
        {
            var generatorInstance = MySession.Static.GetComponent<MyGlobalEncountersGenerator>();

            if (sender is Button btn && btn.Tag is long encounterId)
            {
                RemoveGlobalEncounter(encounterId);
                LoadActiveGlobalEncountersData(); // Refresh the grid after removal
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

        private void LoadActiveGlobalEncountersData()
        {
            var generatorInstance = MySession.Static.GetComponent<MyGlobalEncountersGenerator>();
            if (generatorInstance == null)
            {
                MessageBox.Show("MyGlobalEncountersGenerator instance not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var encounterComponentsField = typeof(MyGlobalEncountersGenerator).GetField("m_encounterComponents", BindingFlags.NonPublic | BindingFlags.Instance);
            var encounterTimerField = typeof(MyGlobalEncountersGenerator).GetField("m_encountersTimer", BindingFlags.NonPublic | BindingFlags.Instance);

            if (encounterComponentsField == null)
            {
                MessageBox.Show("Unable to retrieve m_encounterComponents field.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var encounterComponents = encounterComponentsField.GetValue(generatorInstance) as ConcurrentDictionary<long, HashSet<MyGlobalEncounterComponent>>;
            var encounterTimer = encounterTimerField.GetValue(generatorInstance) as ConcurrentDictionary<long, long>;
            if (encounterComponents == null || encounterComponents.Count == 0)
            {
                return;
            }
            _activeEncounterCollection.Clear();
            foreach (var kv in encounterComponents)
            {
                long encounterId = kv.Key;
                long timer = 0;
                if (encounterTimer != null && encounterTimer.TryGetValue(encounterId, out var t))
                    timer = t;
                foreach (var comp in kv.Value)
                {
                    var entity = comp.Entity;
                    _activeEncounterCollection.Add(new {
                        EncounterId = encounterId,
                        comp.SpawnGroupName,
                        EntityId = entity?.EntityId ?? 0,
                        EntityType = entity?.GetType().Name ?? "-",
                        Position = entity is VRage.Game.Entity.MyEntity ent ? ent.PositionComp?.GetPosition().ToString() : "-",
                        Timer = timer
                    });
                }
            }
            ActiveGlobalEncountersGrid.ItemsSource = _activeEncounterCollection;
        }
        
    }
}
