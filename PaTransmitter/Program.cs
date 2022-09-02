using PaTransmitter.Code;
using PaTransmitter.Code.Transport;
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Diagnostics;


var builder = WebApplication.CreateBuilder();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddLogging(config =>
{
        // Clear out default configuration.
    config.ClearProviders();

    config.AddConfiguration(builder.Configuration.GetSection("Logging"));
    config.AddDebug();
    config.AddEventSourceLogger();
    config.AddFile("logs/logs.txt");

    //config.AddConsole();
#if DEBUG

    config.AddConsole();

#endif
});

var app = builder.Build();

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseExceptionHandler(excApp =>
{
    excApp.Run(async context =>
    {
        var exceptionHandlerPathFeature =
            context.Features.Get<IExceptionHandlerPathFeature>();

        if (exceptionHandlerPathFeature?.Error is IOException ex)
        {   
            app.Logger.LogError("[App] Global IO exception: {Exception}", ex.Message);
            app.Logger.LogError("[App] App will try to reset connection transports");
            await TransportsManager.Instance.InitializeTransports(app);
        }
    });
});

app.Logger.LogInformation("Hello world");

// Initializes all components necessary for the operation of the Òransmitter.
var prepare = new PrepareConveyor(app);
Task.Run(prepare.Prepare);

app.Run();
