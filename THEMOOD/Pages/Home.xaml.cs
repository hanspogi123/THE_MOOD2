using THEMOOD.Services;
using Microsoft.Maui.Controls;
using System;

namespace THEMOOD.Pages;

public partial class Home : ContentView
{
	private readonly FirebaseAuthService _authService;

	public Home()
	{
		InitializeComponent();
		_authService = new FirebaseAuthService();
	}

	private async void OnLogoutClicked(object sender, EventArgs e)
	{
		try
		{
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