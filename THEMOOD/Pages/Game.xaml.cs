using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using THEMOOD.Services;

namespace THEMOOD.Pages;

public partial class Game : ContentView
{
	public Game()
	{
		InitializeComponent();
	}

	private async void StartButton_Clicked(object sender, EventArgs e)
	{
        await Shell.Current.GoToAsync("gamepage");
    }
}