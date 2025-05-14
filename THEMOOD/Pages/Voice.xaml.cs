using Plugin.Maui.Audio;
using THEMOOD.Services;
using THEMOOD.ViewModels;

namespace THEMOOD.Pages;

public partial class Voice : ContentView
{
    private readonly IAudioManager _audioManager;
    private readonly OpenAIService _openAIService;
    private readonly VoiceViewModel _viewModel;

    public Voice(IAudioManager audioManager, OpenAIService openAIService)
    {
        try
        {
            InitializeComponent();
            _audioManager = audioManager;
            _openAIService = openAIService;
            _viewModel = new VoiceViewModel(_audioManager, _openAIService);
            BindingContext = _viewModel;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing Voice page: {ex}");
            // You might want to show an error message to the user here
        }
    }

    private async void ContentView_Unloaded(object sender, EventArgs e)
    {
        try
        {
            await _viewModel.CleanupAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during cleanup: {ex}");
        }
    }
}