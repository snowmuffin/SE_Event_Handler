using System;
using System.Threading;

namespace TorchPlugin
{
    public class CustomInstance
    {
        private static CustomInstance _instance;
        private static readonly object Lock = new object();
        private CustomWindow _customWindow;
        private bool _isRunning;

        // Ensure the constructor is public
        public CustomInstance() { }

        public static CustomInstance GetInstance()
        {
            lock (Lock)
            {
                if (_instance == null)
                {
                    _instance = new CustomInstance();
                }
                return _instance;
            }
        }

        public void ShowWindow()
        {
            lock (Lock)
            {
                if (_isRunning)
                {
                    _customWindow?.Dispatcher.Invoke(() => _customWindow.Activate());
                    return;
                }

                Thread uiThread = new Thread(() =>
                {
                    _customWindow = new CustomWindow(this);
                    _customWindow.Show();
                    System.Windows.Threading.Dispatcher.Run();
                });

                uiThread.SetApartmentState(ApartmentState.STA);
                uiThread.Start();
                _isRunning = true;
            }
        }

        public void Start()
        {
            lock (Lock)
            {
                if (_isRunning)
                {
                    Console.WriteLine("CustomInstance is already running.");
                    return;
                }

                Console.WriteLine("CustomInstance started.");
                ShowWindow();
                _isRunning = true;
            }
        }

        public void Stop()
        {
            lock (Lock)
            {
                if (!_isRunning)
                {
                    Console.WriteLine("CustomInstance is not running.");
                    return;
                }

                _customWindow?.Dispatcher.Invoke(() => _customWindow.Close());
                _customWindow = null;
                _isRunning = false;
                Console.WriteLine("CustomInstance stopped.");
            }
        }

        public void Communicate(string message)
        {
            Console.WriteLine($"Message from Plugin: {message}");
        }
    }
}