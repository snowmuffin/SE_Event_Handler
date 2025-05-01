using System;
using System.Windows;
using Shared.Plugin;

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
    }
}