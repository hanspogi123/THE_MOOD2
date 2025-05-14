using THEMOOD.Services;
using Microsoft.Maui.Controls;

namespace THEMOOD.Extensions
{
    public static class ClickSoundExtensions
    {
        public static void AddClickSound(this Button button, SoundEffectService soundService)
        {
            button.Clicked += (s, e) => soundService.PlayClick();
        }

        public static void AddClickSound(this TapGestureRecognizer tap, SoundEffectService soundService)
        {
            tap.Tapped += (s, e) => soundService.PlayClick();
        }
    }
} 