using Microsoft.Maui.Controls;
using THEMOOD.ViewModels;
using THEMOOD.Services;
using THEMOOD.Extensions;

namespace THEMOOD.Controls
{
    public partial class CustomNavBar : ContentView
    {
        private NavBarViewModel _viewModel;
        private readonly SoundEffectService _soundService;

        public CustomNavBar()
        {
            InitializeComponent();

            // Get the sound service from DI
            _soundService = Application.Current.Handler.MauiContext.Services.GetService<SoundEffectService>();

            // Assign the ViewModel or retrieve it from parent page
            _viewModel = NavBarViewModel.Instance;
            BindingContext = _viewModel;
            SetupGestureRecognizers();
        }

        private void SetupGestureRecognizers()
        {
            // Add gesture recognizers directly to the VerticalStackLayouts
            // Get each column's VerticalStackLayout
            if (NavGrid.Children[0] is VerticalStackLayout homeStack)
            {
                var homeRecognizer = new TapGestureRecognizer();
                homeRecognizer.Tapped += (s, e) => 
                {
                    _soundService?.PlayClick();
                    _viewModel.NavigateToHomeCommand.Execute(null);
                };
                homeStack.GestureRecognizers.Add(homeRecognizer);
            }

            if (NavGrid.Children[1] is VerticalStackLayout walletStack)
            {
                var walletRecognizer = new TapGestureRecognizer();
                walletRecognizer.Tapped += (s, e) => 
                {
                    _soundService?.PlayClick();
                    _viewModel.NavigateToWalletCommand.Execute(null);
                };
                walletStack.GestureRecognizers.Add(walletRecognizer);
            }

            // Frame for the transfer button
            if (NavGrid.Children[2] is Frame transferFrame)
            {
                var transferRecognizer = new TapGestureRecognizer();
                transferRecognizer.Tapped += (s, e) => 
                {
                    _soundService?.PlayClick();
                    _viewModel.NavigateToChatCommand.Execute(null);
                };
                transferFrame.GestureRecognizers.Add(transferRecognizer);
            }

            if (NavGrid.Children[3] is VerticalStackLayout activityStack)
            {
                var activityRecognizer = new TapGestureRecognizer();
                activityRecognizer.Tapped += (s, e) => 
                {
                    _soundService?.PlayClick();
                    _viewModel.NavigateToActivityCommand.Execute(null);
                };
                activityStack.GestureRecognizers.Add(activityRecognizer);
            }

            if (NavGrid.Children[4] is VerticalStackLayout profileStack)
            {
                var profileRecognizer = new TapGestureRecognizer();
                profileRecognizer.Tapped += (s, e) => 
                {
                    _soundService?.PlayClick();
                    _viewModel.NavigateToProfileCommand.Execute(null);
                };
                profileStack.GestureRecognizers.Add(profileRecognizer);
            }
        }
        

        // Add method to set the binding context from parent page
        public void SetViewModel(NavBarViewModel viewModel)
        {
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }
    }
}