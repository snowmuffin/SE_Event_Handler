using System;
using System.Collections.Generic;
using System.Windows;
using Shared.Plugin;
using Sandbox.Game.World;
using SpaceEngineers.Game.SessionComponents;
using VRage.Game;
using VRage.Game.ModAPI;
using Sandbox.Definitions;
using Torch.Utils;

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

        // Refresh 버튼 클릭 시 각 탭 데이터 로드 함수 호출
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
    }
}