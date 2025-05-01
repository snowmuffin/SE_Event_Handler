using System.Windows.Controls;
using Shared.Plugin;
using System;
using System.Windows;

namespace TorchPlugin
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once RedundantExtendsListEntry
    public partial class ConfigView : UserControl
    {
        private bool _isCustomInstanceRunning;

        public ConfigView()
        {
            InitializeComponent();
            DataContext = Common.Config;
        }

        private void LaunchCustomInstance_OnClick(object sender, RoutedEventArgs e)
        {
            var customInstance = CustomInstance.GetInstance();

            if (customInstance != null)
            {
                customInstance.Start();
            }
        }

        private void StopCustomInstance_OnClick(object sender, RoutedEventArgs e)
        {
            var customInstance = CustomInstance.GetInstance();

            if (customInstance != null)
            {
                customInstance.Stop();
            }
        }
    }
}