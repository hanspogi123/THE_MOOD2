using Microsoft.Extensions.Logging;
using THEMOOD.Services;
using THEMOOD.ViewModels;
using THEMOOD.Pages;
using THEMOOD.Converters;
using CommunityToolkit.Maui;
using Microcharts.Maui;
using CommunityToolkit.Maui.Media;
using Plugin.Maui.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.IO;

namespace THEMOOD;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMicrocharts()
            .UseMauiCommunityToolkitMediaElement()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("Genty-Sans-Regular.ttf", "GentySans");
            });

        // Add configuration
        var assembly = typeof(MauiProgram).Assembly;
        using var stream = assembly.GetManifestResourceStream("THEMOOD.appsettings.json");
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        // Register services
        builder.Services.AddSingleton<ConnectivityService>();
        builder.Services.AddSingleton(AudioManager.Current);
        builder.Services.AddSingleton<OpenAIService>(sp => 
            new OpenAIService(configuration["OpenAIApiKey"]));

        // Register ViewModels
        builder.Services.AddTransient<MoodEntry_VM>();

        // Register Pages
        builder.Services.AddTransient<MoodEntryPage>();
        builder.Services.AddTransient<Voice>();

        // Register Converters
        builder.Services.AddSingleton<AudioInverseBoolConverter>();
        builder.Services.AddSingleton<AudioStartStopConverter>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}