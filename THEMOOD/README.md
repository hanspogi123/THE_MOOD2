# THE MOOD App

A .NET MAUI application for mood tracking and meditation.

## Prerequisites

- Visual Studio 2022 (17.8 or later)
- .NET 8.0 SDK
- MAUI workload installed

## Setup Instructions

1. Clone the repository
2. Open the solution in Visual Studio 2022
3. Create an `appsettings.json` file in the root directory with the following structure:
   ```json
   {
       "OpenAIApiKey": "your-api-key-here"
   }
   ```
4. Restore NuGet packages
5. Build and run the solution

## Required NuGet Packages

- CommunityToolkit.Maui (8.0.1)
- CommunityToolkit.Maui.MediaElement (2.0.0)
- CommunityToolkit.Mvvm (8.4.0)
- Firebase.Auth (1.0.0)
- FirebaseDatabase.net (4.2.0)
- Microcharts.Maui (1.0.0)
- Microsoft.Extensions.Configuration.Json (8.0.0)
- Microsoft.Extensions.Logging.Debug (8.0.1)
- Microsoft.Maui.Controls (8.0.100)
- Plugin.Maui.Audio (3.1.1)
- SkiaSharp.Views.Maui.Controls (3.116.1)
- Websocket.Client (5.1.2)

## Troubleshooting

If you encounter any issues:
1. Make sure you have the MAUI workload installed: `dotnet workload install maui`
2. Clean and rebuild the solution
3. Delete the `bin` and `obj` folders and restore NuGet packages
4. Make sure you have the correct .NET SDK version installed 