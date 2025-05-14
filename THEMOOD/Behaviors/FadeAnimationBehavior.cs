using Microsoft.Maui.Controls;

namespace THEMOOD.Behaviors
{
    public class FadeAnimationBehavior : Behavior<View>
    {
        private View _associatedObject;

        protected override void OnAttachedTo(View bindable)
        {
            base.OnAttachedTo(bindable);
            _associatedObject = bindable;
            StartAnimation();
        }

        protected override void OnDetachingFrom(View bindable)
        {
            base.OnDetachingFrom(bindable);
            _associatedObject = null;
        }

        private async void StartAnimation()
        {
            while (_associatedObject != null)
            {
                await _associatedObject.FadeTo(1, 400);
                await Task.Delay(100);
                await _associatedObject.FadeTo(0.3, 400);
                await Task.Delay(100);
            }
        }
    }
} 