using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Shared.Plugin;
using Sandbox.Game.World;
using Torch.Utils;

namespace TorchPlugin
{
    /// <summary>
    /// Interaction logic for CustomWindow.xaml
    /// </summary>
    public partial class CustomWindow : Window
    {
        private readonly CustomInstance _customInstance;

        [ReflectedGetter(Name = "m_stations", Type = typeof(MyFaction))]
        private static Func<MyFaction, Dictionary<long, MyStation>> _stations;

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

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (MySession.Static?.Factions == null)
            {
                MessageBox.Show("Factions data is not available. Ensure the game session is running.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var factions = MySession.Static.Factions.Factions.Values.Select(f => new
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

            FactionsList.ItemsSource = factions;
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
                                station.Value.IsDeepSpaceStation,
                                station.Value.IsOnPlanetWithAtmosphere
                            }).ToList();
                        }
                        else
                        {
                            StationsGrid.ItemsSource = null;
                        }
                    }
                }
            }
        }
    }
}