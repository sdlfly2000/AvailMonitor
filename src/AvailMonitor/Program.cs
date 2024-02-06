using AvailMonitor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddWindowsService(option => option.ServiceName = builder.Configuration["Serilog:Properties:Application"]! )
    .AddSerilog(configure => configure.ReadFrom.Configuration(builder.Configuration))
    .AddHostedService<Worker>()
    .AddHttpClient("Available Checker", httpClient => httpClient.Timeout = TimeSpan.FromSeconds(10))
    .ConfigurePrimaryHttpMessageHandler(
        (handlerBuilder) => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        });

var host = builder.Build();
host.Run();