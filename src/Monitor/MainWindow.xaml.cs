using Microsoft.Toolkit.Mvvm.Input;
using System.Windows;
using Application = System.Windows.Application;

namespace Monitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MonitorService _monitorService;

        private bool _isStartMonitor = false;

        public MainWindow(MonitorService service)
        {
            _monitorService = service;
            _monitorService.Notification += OnStatusChanged;
            InitializeComponent();
        }

        private void StartMonitor(object sender, RoutedEventArgs e)
        {
            _isStartMonitor = true;

            btnStartMonitor.IsEnabled = !_isStartMonitor;
            btnStopMonitor.IsEnabled = _isStartMonitor;

            _monitorService.StartMonitor();
        }

        private void StopMonitor(object sender, RoutedEventArgs e)
        {
            _isStartMonitor = false;

            btnStartMonitor.IsEnabled = !_isStartMonitor;
            btnStopMonitor.IsEnabled = _isStartMonitor;

            _monitorService.StopMonitor();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowState = WindowState.Minimized;
            WindowViewModel.ShowInTaskbar = false;
            e.Cancel = true;
        }

        private void ExitApp(object sender, RoutedEventArgs e)
        {
            if (_isStartMonitor)
            {
                _monitorService!.StopMonitor();
            }

            Application.Current.Shutdown();
        }

        private void OnStatusChanged(object? sender, Status status)
        {
            if(status == Status.Success)
            {
                TrayNotifier.SetIcon("pass.ico");
            }
            else
            {
                TrayNotifier.SetIcon("warning.ico");
            }
        }
    }
}