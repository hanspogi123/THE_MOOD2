using THEMOOD.Services;
using Microsoft.Maui.Controls;
using System;
using Firebase.Auth;

namespace THEMOOD.Pages;

public partial class Home : ContentView
{
	private readonly FirebaseAuthService _authService;

	public Home()
	{
		InitializeComponent();
		_authService = new FirebaseAuthService();
		LoadUserInfo();
	}

	private void LoadUserInfo()
	{
		try
		{
			var user = UserService.Instance.GetCurrentUser();
			if (user != null)
			{
				UserNameLabel.Text = string.IsNullOrEmpty(user.DisplayName) 
					? "Welcome!" 
					: $"Welcome, {user.DisplayName}!";
				UserEmailLabel.Text = user.Email;
			}
			else
			{
				UserNameLabel.Text = "Welcome!";
				UserEmailLabel.Text = "Please sign in to see your information";
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error loading user info: {ex.Message}");
			UserNameLabel.Text = "Welcome!";
			UserEmailLabel.Text = "Error loading user information";
		}
	}

	private async void OnTrackMoodClicked(object sender, EventArgs e)
	{
		// TODO: Implement mood tracking navigation
		await Application.Current.MainPage.DisplayAlert("Coming Soon", "Mood tracking feature will be available soon!", "OK");
	}

	private async void OnLogoutClicked(object sender, EventArgs e)
	{
		try
		{
			// Clear user information
			UserService.Instance.ClearUser();
			
			// Sign out from Firebase
			await _authService.SignOut();
			
			// Navigate to login page
			await Shell.Current.GoToAsync("//login");
		}
		catch (Exception ex)
		{
			await Application.Current.MainPage.DisplayAlert("Error", $"Failed to log out: {ex.Message}", "OK");
		}
	}

	private async void OnExitClicked(object sender, EventArgs e)
	{
		try
		{
			// Show confirmation dialog
			bool answer = await Application.Current.MainPage.DisplayAlert("Exit", "Are you sure you want to exit?", "Yes", "No");
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
}