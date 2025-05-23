using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using Sandbox.Game.World;
using SpaceEngineers.Game.SessionComponents;
using VRage.Game;

namespace TorchPlugin
{
    public partial class CustomWindow : Window
    {
        // Global Events 탭 관련 함수 및 이벤트 핸들러
        private void LoadGlobalEventFactoryData()
        {
            var eventFactoryType = typeof(MyGlobalEventFactory);
            var typesToHandlersField = eventFactoryType.GetField("m_typesToHandlers", BindingFlags.NonPublic | BindingFlags.Static);
            var globalEventFactoryField = eventFactoryType.GetField("m_globalEventFactory", BindingFlags.NonPublic | BindingFlags.Static);

            if (typesToHandlersField == null || globalEventFactoryField == null)
            {
                MessageBox.Show("Unable to retrieve data from MyGlobalEventFactory.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var typesToHandlers = typesToHandlersField.GetValue(null) as Dictionary<MyDefinitionId, MethodInfo>;
            var globalEventFactory = globalEventFactoryField.GetValue(null);

            if (typesToHandlers == null)
            {
                MessageBox.Show("No event handlers found.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var eventData = typesToHandlers.Select(entry => new
            {
                EventDefinitionId = entry.Key.ToString(),
                HandlerMethod = entry.Value.Name
            }).ToList();

            GlobalEventsGrid.ItemsSource = eventData;
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
    }
}
