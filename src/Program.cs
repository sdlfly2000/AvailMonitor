using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace AvailMonitor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateDefaultHost(args).Run();
        }

        private static IHost CreateDefaultHost(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services
                .AddWindowsService(option => { option.ServiceName = "Health Monitor"; })
                .AddHostedService<Worker>()
                .AddSerilog(configure => configure.ReadFrom.Configuration(builder.Configuration))
                .AddHttpClient("Available Checker", httpClient => httpClient.Timeout = TimeSpan.FromSeconds(10))
                .ConfigureHttpMessageHandlerBuilder(
                (builder) => builder.PrimaryHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                });

            return builder.Build();
        }
    }
}
