using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Net.Http;

namespace AvailMonitor
{
    public class Worker: BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        private Timer _timer;

        public Worker(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(CheckAvailability, stoppingToken, 0, 300000);

            return Task.CompletedTask;
        }

        private void CheckAvailability(object? state)
        {
            var token = state is CancellationToken cancellationToken ? cancellationToken : default;

            if (token == default || token.IsCancellationRequested)
            {
                StopAsync(token).GetAwaiter().GetResult();
            }

            var targets = _configuration.GetSection("Monitors");

            Parallel.ForEach(
                targets.AsEnumerable(),
                () => _httpClientFactory.CreateClient(),
                (target) =>
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
                    results.TryAdd(target.Value, ex.Message.Replace(",", ""));
                }

            });

            foreach (var result in results.AsEnumerable())
            {
                Log.Information("Status: {code}, Call: {uri}", result.Value, result.Key);
            }

        }
    }
}
