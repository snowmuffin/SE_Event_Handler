using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using Sandbox.Game.World;
using SpaceEngineers.Game.SessionComponents;
using VRage.Game;
using Sandbox.Definitions;

namespace TorchPlugin
{
    public partial class CustomWindow : Window
    {
        // Custom Spawn 탭 관련 함수 및 이벤트 핸들러
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

        private void SpawnButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPlayer = PlayerDropdown.SelectedItem as MyPlayer;
            var selectedGroup = SpawnGroupDropdown.SelectedItem as MySpawnGroupDefinition;
            if (selectedPlayer == null)
            {
                MessageBox.Show("Please select both a player and a spawn group.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // ...스폰 로직 구현 필요...
        }
    }
}
