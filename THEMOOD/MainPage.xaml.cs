using THEMOOD.ViewModels;
using THEMOOD.Services;
using CommunityToolkit.Maui.Views;  

namespace THEMOOD;

public partial class MainPage : ContentPage
{
    private View _currentView;
    private bool _isTransitioning;

    public MainPage()
    {
        InitializeComponent();

        // Initialize ConnectivityService
        _ = ConnectivityService.Instance;

        // Inject NavBar ViewModel singleton
        NavBar.SetViewModel(NavBarViewModel.Instance);

        PopupService.Initialize(
            popup => this.ShowPopup(popup),
            async popup => await this.ShowPopupAsync(popup)
        );

        // Set Chat view as the initial content
        var chatView = new THEMOOD.Pages.Chat();
        MainContentArea.Content = chatView;
        _currentView = chatView;

        // Hook up dynamic content loader
        NavBarViewModel.SetMainPageContent = SetContentWithAnimation;
    }

    private async void OnExitButtonClicked(object sender, EventArgs e)
    {
        try
        {
            // Show confirmation dialog
            bool answer = await DisplayAlert("Exit", "Are you sure you want to exit?", "Yes", "No");
            if (answer)
            {
                Application.Current.Quit();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error exiting application: {ex.Message}");
        }
    }

    private async void SetContentWithAnimation(View newView)
    {
        if (newView == _currentView || _isTransitioning) return;

        try
        {
            _isTransitioning = true;
            await PageTransitionService.AnimatePageTransition(_currentView, newView, MainContentArea);
            MainContentArea.Content = newView;
            _currentView = newView;
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_currentView != null && !_isTransitioning)
        {
            _currentView.Opacity = 0;
            await _currentView.FadeTo(1, 150, Easing.Linear);
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        if (_currentView != null && !_isTransitioning)
        {
            await PageTransitionService.AnimatePageExit(_currentView);
        }
    }
}
