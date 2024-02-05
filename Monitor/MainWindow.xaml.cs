using System.Windows;

namespace Monitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnStartMonitor(object sender, RoutedEventArgs e)
        {

        }

        private void StartMonitor(object sender, RoutedEventArgs e)
        {

        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowState = WindowState.Minimized;
            e.Cancel = true;
        }
    }
}