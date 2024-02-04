﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AvailMonitor
{
    public class Worker: BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly long DelayExecTimeInMs;

        private Timer? _timer;
        private List<Task<AvailResult>> _tasks;

        public Worker(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            DelayExecTimeInMs = long.Parse(_configuration.GetSection("ExecEveryMs").Value!);
            _tasks = new List<Task<AvailResult>>();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(OnTimeJob, cancellationToken, DelayExecTimeInMs, DelayExecTimeInMs);
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            if (_timer != null)
            {
                _timer.Dispose();
            }

            return base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _tasks.Clear();
            return Task.CompletedTask;
        }

        private void OnTimeJob(object? state)
        {
            var token = state is CancellationToken cancellationToken ? cancellationToken : default;
            _tasks.Clear();

            if (token == default || token.IsCancellationRequested)
            {
                StopAsync(token).GetAwaiter().GetResult();
            }

            var targets = _configuration.GetSection("Monitors").AsEnumerable();

            foreach(var target in targets)
            {
                if (target.Value == null) continue;

                _tasks.Add(Task<AvailResult>.Factory.StartNew((states) =>
                {
                    var param = states as AvailParams;
                    try
                    {
                        var response = param!.HttpClient.GetAsync(param.Target.Value).GetAwaiter().GetResult();
                        param.HttpClient.Dispose();
                        return new AvailResult(param.Target.Value!, response.StatusCode.ToString());
                    }
                    catch(Exception e)
                    {
                        return new AvailResult(param!.Target.Value!, e.Message);
                    }
                },
                new AvailParams(_httpClientFactory.CreateClient(), target),
                token));
            }

            Task.WhenAll(_tasks).Wait(token);

            _tasks.ForEach(task => Log.Information("Status: {code}, Call: {uri}", task.Result.Result, task.Result.Url));
        }

        private record AvailParams(HttpClient HttpClient, KeyValuePair<string, string?> Target);

        private record AvailResult(string Url, string Result);
    }
}
