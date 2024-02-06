using Microsoft.Extensions.Configuration;
using Serilog;
using System.Net.Http;

namespace Monitor;

public class MonitorService : IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly long DelayExecTimeInMs;

    private System.Threading.Timer? _timer;
    private List<Task<AvailResult>> _tasks;
    private Status _status;

    public event EventHandler<Status>? Notification;

    public MonitorService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration) 
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        DelayExecTimeInMs = long.Parse(_configuration.GetSection("ExecEveryMs").Value!);
        _tasks = new List<Task<AvailResult>>();
        _status = Status.Success;
    }

    public void StartMonitor()
    {
        Log.Information("---Monitor Started---");
        _timer = new System.Threading.Timer(OnTimeJob, CancellationToken.None, 0, DelayExecTimeInMs);
    }

    public void StopMonitor()
    {
        Log.Information("---Monitor Stopped---");
        Dispose();
    }

    public void Dispose()
    {
        if (_timer != null)
        {
            _timer.Dispose();
        }
    }

    #region Private Methods

    public void OnTimeJob(object? state)
    {
        _tasks.Clear();

        var targets = _configuration.GetSection("Monitors").AsEnumerable();

        foreach (var target in targets)
        {
            if (target.Value == null) continue;

            _tasks.Add(Task<AvailResult>.Factory.StartNew(
            TriggerMonitorTask,
                new AvailParams(_httpClientFactory.CreateClient(), target),
                CancellationToken.None));
        }

        Task.WhenAll(_tasks).Wait(CancellationToken.None);

        _tasks.ForEach(task => {
            Log.Information("Status: {code}, Call: {uri}", task.Result.Result, task.Result.Url); 
        });

        _status = _tasks.Any(task => task.Result.Result != "OK") 
            ? Status.Failure 
            : Status.Success;       

        Notification?.Invoke(this, _status);
    }

    private AvailResult TriggerMonitorTask(object? states)
    {
        var param = states as AvailParams;
        try
        {
            var response = param!.HttpClient.GetAsync(param.Target.Value).GetAwaiter().GetResult();
            param.HttpClient.Dispose();
            return new AvailResult(param.Target.Value!, response.StatusCode.ToString());
        }
        catch (Exception e)
        {
            return new AvailResult(param!.Target.Value!, e.Message);
        }
    }

    private record AvailParams(HttpClient HttpClient, KeyValuePair<string, string?> Target);

    private record AvailResult(string Url, string Result);

    #endregion
}

