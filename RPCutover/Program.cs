using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RPCutover;
using System.CommandLine.Invocation;

// Build the app
var builder = new ConfigurationBuilder();
var absolutePath = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
var provider = new PhysicalFileProvider(absolutePath);
var config = builder
    .SetBasePath(absolutePath)
    .AddJsonFile(@$"{absolutePath}\appsettings.json", false, true)
    .AddJsonFile(@$"{absolutePath}\appsettings.Development.json", true, true)
    .AddEnvironmentVariables()
    .Build();

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton(context.Configuration);
        services.AddSingleton<AppService>();
    })
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.SetBasePath(absolutePath)
        .AddJsonFile(@$"{absolutePath}\appsettings.json", false, true)
        .AddJsonFile(@$"{absolutePath}\appsettings.{context.HostingEnvironment.EnvironmentName}.json", true, true)
        .AddEnvironmentVariables()
        .Build();
    })
    .Build();

var svc = ActivatorUtilities.CreateInstance<AppService>(host.Services);

return await svc.Run(args);