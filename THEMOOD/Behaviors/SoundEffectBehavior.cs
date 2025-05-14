using THEMOOD.Services;

namespace THEMOOD.Behaviors
{
    public class SoundEffectBehavior : Behavior<Button>
    {
        public static readonly BindableProperty SoundTypeProperty =
            BindableProperty.Create(nameof(SoundType), typeof(string), typeof(SoundEffectBehavior), "button");

        public string SoundType
        {
            get => (string)GetValue(SoundTypeProperty);
            set => SetValue(SoundTypeProperty, value);
        }

        protected override void OnAttachedTo(Button button)
        {
            base.OnAttachedTo(button);
            button.Clicked += OnButtonClicked;
        }

        protected override void OnDetachingFrom(Button button)
        {
            base.OnDetachingFrom(button);
            button.Clicked -= OnButtonClicked;
        }

        private async void OnButtonClicked(object sender, EventArgs e)
        {
            if (SoundType.ToLower() == "navigation")
            {
                await SoundEffectService.Instance.PlayNavigationSound();
            }
            else
            {
                await SoundEffectService.Instance.PlayButtonClickSound();
            }
        }
    }
} 