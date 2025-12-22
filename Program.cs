using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using BKBToolClient;
using BKBToolClient.Services;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<FunctionConfigService>();
builder.Services.AddScoped<FileProcessingService>();

var host = builder.Build();

// Versuche ein paar Status‑Updates an die Startup‑Anzeige zu senden.
// Fehler werden gefangen, damit Start nicht scheitert, wenn das JS noch nicht verfügbar ist.
try
{
    var js = host.Services.GetRequiredService<IJSRuntime>();
    // Schrittweise Hinweise — anpassen nach Bedarf
    await js.InvokeVoidAsync("updateStartupProgress", 10, "Starte Anwendung...");
    await Task.Delay(50);
    await js.InvokeVoidAsync("updateStartupProgress", 35, "Initialisiere Dienste...");
    await Task.Delay(50);
    await js.InvokeVoidAsync("updateStartupProgress", 65, "Lade Konfigurationen...");
    await Task.Delay(50);
    await js.InvokeVoidAsync("updateStartupProgress", 85, "Finalisiere Start...");
}
catch
{
    // Ignore — continue startup
}

await host.RunAsync();