using PaTransmitter.Code;
using PaTransmitter.Code.Transport;
using System.Diagnostics;
using System.Reflection;


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

app.Logger.LogInformation("Hello world");

// Initializes all components necessary for the operation of the Òransmitter.
var prepare = new PrepareConveyor(app);
Task.Run(prepare.Prepare);

app.Run();
