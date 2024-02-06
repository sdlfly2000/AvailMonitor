using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Net.Http;
using System.Windows;
using Application = System.Windows.Application;

namespace Monitor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            services
                .AddSerilog(configure => configure.ReadFrom.Configuration(configuration))
                .AddSingleton<IConfiguration>(configuration)
                .AddSingleton<MainWindow>()
                .AddSingleton<MonitorService>()
                .AddSingleton<NotifyIconWrapper>()
                .AddSingleton<MainWindowViewModel>()
                .AddHttpClient("Available Checker", httpClient => httpClient.Timeout = TimeSpan.FromSeconds(10))
                .ConfigurePrimaryHttpMessageHandler(
                    (handlerBuilder) => new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                    });
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            var mainWindow = _serviceProvider.GetService<MainWindow>();
            mainWindow!.Show();
        }
    }
}
