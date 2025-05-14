using THEMOOD.Services;
using Microsoft.Maui.Controls;

namespace THEMOOD.Behaviors
{
    public class SoundEffectBehavior : Behavior<View>
    {
        private readonly SoundEffectService _soundService;

        public SoundEffectBehavior()
        {
            // Get the sound service from DI
            _soundService = Application.Current.Handler.MauiContext.Services.GetService<SoundEffectService>();
        }

        public static readonly BindableProperty SoundTypeProperty =
            BindableProperty.Create(nameof(SoundType), typeof(string), typeof(SoundEffectBehavior), "button");

        public string SoundType
        {
            get => (string)GetValue(SoundTypeProperty);
            set => SetValue(SoundTypeProperty, value);
        }

        protected override void OnAttachedTo(View element)
        {
            base.OnAttachedTo(element);
            
            // For Buttons
            if (element is Button button)
            {
                button.Clicked += OnElementTapped;
            }
            // For other elements
            else
            {
                element.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () => await OnElementTappedAsync())
                });
            }
        }

        protected override void OnDetachingFrom(View element)
        {
            base.OnDetachingFrom(element);
            
            if (element is Button button)
            {
                button.Clicked -= OnElementTapped;
            }
            else
            {
                var existingGesture = element.GestureRecognizers.OfType<TapGestureRecognizer>().FirstOrDefault();
                if (existingGesture != null)
                {
                    element.GestureRecognizers.Remove(existingGesture);
                }
            }
        }

        private async void OnElementTapped(object sender, EventArgs e)
        {
            await OnElementTappedAsync();
        }

        private Task OnElementTappedAsync()
        {
            _soundService?.PlayClick();
            return Task.CompletedTask;
        }
    }
} 