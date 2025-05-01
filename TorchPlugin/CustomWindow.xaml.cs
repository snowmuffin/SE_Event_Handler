using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Shared.Plugin;
using Sandbox.Game.World;

namespace TorchPlugin
{
    /// <summary>
    /// Interaction logic for CustomWindow.xaml
    /// </summary>
    public partial class CustomWindow : Window
    {
        private readonly CustomInstance _customInstance;

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
    }
}