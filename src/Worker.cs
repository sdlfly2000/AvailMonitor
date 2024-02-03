using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Collections.Concurrent;

namespace AvailMonitor
{
    public class Worker: BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        private Timer? _timer;
        private IList<Task> _tasks;
        private ConcurrentDictionary<string, string> _results;

        public Worker(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _tasks = new List<Task>();
            _results = new ConcurrentDictionary<string, string>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(OnTimeJob, stoppingToken, 0, 300000);

            return Task.CompletedTask;
        }

        private void OnTimeJob(object? state)
        {
            var token = state is CancellationToken cancellationToken ? cancellationToken : default;

            if (token == default || token.IsCancellationRequested)
            {
                StopAsync(token).GetAwaiter().GetResult();
            }

            var targets = _configuration.GetSection("Monitors");

            foreach(var target in targets.AsEnumerable())
            {
                _tasks.Add(new Task((states) =>
                {
                    var param = states as AvailParams;

                    var response = param!.HttpClient.GetAsync(param.Target.Value).GetAwaiter().GetResult();
                    _results.TryAdd(param.Target.Value!, response.StatusCode.ToString());
                    param.HttpClient.Dispose();
                },
                new AvailParams { HttpClient = _httpClientFactory.CreateClient(), Target = target}));
            }

            foreach(var task in _tasks)
            {
                task.Start();
            }

            Task.WhenAll(_tasks);

            foreach (var result in _results.AsEnumerable())
            {
                Log.Information("Status: {code}, Call: {uri}", result.Value, result.Key);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            if (_timer != null)
            {
                _timer.Dispose();
            }

            return base.StopAsync(cancellationToken);
        }

        private class AvailParams
        {
            public required HttpClient HttpClient { get; set; }
            public KeyValuePair<string, string?> Target { get; set; }
        }
    }
}
