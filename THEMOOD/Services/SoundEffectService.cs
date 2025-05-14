using Plugin.Maui.Audio;

namespace THEMOOD.Services
{
    public class SoundEffectService
    {
        private readonly IAudioManager _audioManager;
        private IAudioPlayer _clickPlayer;

        public SoundEffectService(IAudioManager audioManager)
        {
            _audioManager = audioManager;
            InitializeClickSound();
        }

        private void InitializeClickSound()
        {
            var assembly = typeof(SoundEffectService).Assembly;
            using var stream = assembly.GetManifestResourceStream("THEMOOD.Resources.Raw.tap.mp3");
            
            if (stream != null)
            {
                _clickPlayer = _audioManager.CreatePlayer(stream);
            }
        }

        public void PlayClick()
        {
            if (_clickPlayer != null && !_clickPlayer.IsPlaying)
            {
                _clickPlayer.Play();
            }
        }
    }
} 