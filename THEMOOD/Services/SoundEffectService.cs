using Plugin.Maui.Audio;

namespace THEMOOD.Services
{
    public class SoundEffectService
    {
        private readonly IAudioManager _audioManager;
        private IAudioPlayer _buttonClickPlayer;
        private IAudioPlayer _navigationPlayer;
        private static SoundEffectService _instance;

        public static SoundEffectService Instance => _instance ??= new SoundEffectService();

        private SoundEffectService()
        {
            _audioManager = AudioManager.Current;
            InitializeAudioPlayers();
        }

        private async void InitializeAudioPlayers()
        {
            try
            {
                // Load sound effects from embedded resources
                var assembly = typeof(SoundEffectService).Assembly;

                using var buttonClickStream = assembly.GetManifestResourceStream("THEMOOD.Resources.Raw.button_click.wav");
                using var navigationStream = assembly.GetManifestResourceStream("THEMOOD.Resources.Raw.navigation.wav");

                if (buttonClickStream != null)
                {
                    var audioSource = await _audioManager.CreatePlayerAsync(buttonClickStream);
                    _buttonClickPlayer = audioSource;
                }

                if (navigationStream != null)
                {
                    var audioSource = await _audioManager.CreatePlayerAsync(navigationStream);
                    _navigationPlayer = audioSource;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing sound effects: {ex.Message}");
            }
        }

        public async Task PlayButtonClickSound()
        {
            try
            {
                if (_buttonClickPlayer != null && !_buttonClickPlayer.IsPlaying)
                {
                    _buttonClickPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing button click sound: {ex.Message}");
            }
        }

        public async Task PlayNavigationSound()
        {
            try
            {
                if (_navigationPlayer != null && !_navigationPlayer.IsPlaying)
                {
                    _navigationPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing navigation sound: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _buttonClickPlayer?.Dispose();
            _navigationPlayer?.Dispose();
        }
    }
} 