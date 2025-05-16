using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.Maui.LifecycleEvents;
using WinUIWindow = Microsoft.UI.Xaml.Window;
using System;
using Microsoft.UI.Xaml.Controls;
using WinUIButton = Microsoft.UI.Xaml.Controls.Button;
using WinUIColor = Microsoft.UI.Colors;
using WinUIBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace THEMOOD.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected override MauiApp CreateMauiApp()
    {
        var app = MauiProgram.CreateMauiApp();
        
        // Configure the window to start in full screen mode
        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, window) =>
        {
            try
            {
                var nativeWindow = handler.PlatformView;
                if (nativeWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("Warning: Native window is null");
                    return;
                }

                nativeWindow.Activate();

                var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
                if (windowHandle == IntPtr.Zero)
                {
                    System.Diagnostics.Debug.WriteLine("Warning: Window handle is invalid");
                    return;
                }

                var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                // Set the window to full screen mode
                if (appWindow != null)
                {
                    // Create custom title bar
                    var titleBar = appWindow.TitleBar;
                    titleBar.ExtendsContentIntoTitleBar = true;
                    titleBar.ButtonBackgroundColor = WinUIColor.Transparent;
                    titleBar.ButtonForegroundColor = WinUIColor.White;
                    titleBar.ButtonHoverBackgroundColor = WinUIColor.DarkGray;
                    titleBar.ButtonHoverForegroundColor = WinUIColor.White;
                    titleBar.ButtonPressedBackgroundColor = WinUIColor.Gray;
                    titleBar.ButtonPressedForegroundColor = WinUIColor.White;

                    // Add exit button to title bar
                    var exitButton = new WinUIButton
                    {
                        Content = "✕",
                        FontSize = 14,
                        Width = 46,
                        Height = 32,
                        Background = new WinUIBrush(WinUIColor.Transparent),
                        Foreground = new WinUIBrush(WinUIColor.White)
                    };

                    exitButton.Click += (s, e) =>
                    {
                        try
                        {
                            // Close the application using the native window
                            nativeWindow.Close();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error closing window: {ex.Message}");
                        }
                    };

                    // Add the exit button to the title bar
                    titleBar.ButtonBackgroundColor = WinUIColor.Transparent;
                    titleBar.ButtonForegroundColor = WinUIColor.White;
                    titleBar.ButtonHoverBackgroundColor = WinUIColor.DarkGray;
                    titleBar.ButtonHoverForegroundColor = WinUIColor.White;
                    titleBar.ButtonPressedBackgroundColor = WinUIColor.Gray;
                    titleBar.ButtonPressedForegroundColor = WinUIColor.White;

                    // Set the window to full screen mode
                    appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Warning: AppWindow is null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up full screen mode: {ex.Message}");
                // Continue with normal window creation if full screen setup fails
            }
        });

        return app;
    }
}