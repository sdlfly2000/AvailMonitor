using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices(
                    (context, services) =>
                    {
                        services
                            .AddSerilog(configure => configure.ReadFrom.Configuration(context.Configuration))
                            .AddHttpClient("Available Checker",
                                httpClient => httpClient.Timeout = TimeSpan.FromSeconds(10))
                            .ConfigureHttpMessageHandlerBuilder(
                                (builder) => builder.PrimaryHandler = new HttpClientHandler 
                                {
                                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                                });

                    })
                .Build();
        }
    }
}
