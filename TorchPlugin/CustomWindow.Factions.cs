using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Sandbox.Game.World;
using System.Reflection;
using SpaceEngineers.Game.SessionComponents;
using VRage.Game.ModAPI;
using VRage.Game;
using Sandbox.Definitions;

namespace TorchPlugin
{
    public partial class CustomWindow : Window
    {
        // Factions 탭 관련 함수 및 이벤트 핸들러
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
    }
}
