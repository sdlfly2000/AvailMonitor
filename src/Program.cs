using Microsoft.Extensions.Configuration;
using Serilog;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Logging;

namespace AvailMonitor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, false)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            var targets = configuration.GetSection("Monitors");

            using var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            using var httpClient = new HttpClient(clientHandler);
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            var results = new ConcurrentDictionary<string, string>();

            Parallel.ForEach(targets.AsEnumerable(), (target) =>
            {
                if (target.Value == null)
                {
                    return;
                }

                try
                {
                    var response = httpClient.GetAsync(target.Value).GetAwaiter().GetResult();
                    results.TryAdd(target.Value, response.StatusCode.ToString());
                }
                catch (Exception ex)
                {
                    results.TryAdd(target.Value, ex.Message.Replace(",",""));
                }

            });

            foreach (var result in results.AsEnumerable())
            {
                Log.Information("Status: {code}, Call: {uri}", result.Value, result.Key);
            }
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
