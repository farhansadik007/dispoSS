using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DisposableScreenshots.Controls
{
    /// <summary>
    /// Adds the retro half-press motion to buttons. The motion is driven from code
    /// because a TranslateTransform inside a ControlTemplate can be frozen by WPF,
    /// which silently prevents storyboard animations from moving it.
    /// </summary>
    public static class ButtonMotion
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(ButtonMotion),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty PressOffsetProperty =
            DependencyProperty.RegisterAttached(
                "PressOffset",
                typeof(double),
                typeof(ButtonMotion),
                new PropertyMetadata(4.0));

        public static readonly DependencyProperty HoverMotionProperty =
            DependencyProperty.RegisterAttached(
                "HoverMotion",
                typeof(bool),
                typeof(ButtonMotion),
                new PropertyMetadata(true));

        private static readonly ConditionalWeakTable<Button, MotionState> States =
            new ConditionalWeakTable<Button, MotionState>();

        private const int HoverDurationMs = 200;
        private const int PressDurationMs = 100;
        private const int ReleaseDurationMs = 220;

        public static bool GetIsEnabled(DependencyObject element)
        {
            return (bool)element.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject element, bool value)
        {
            element.SetValue(IsEnabledProperty, value);
        }

        public static double GetPressOffset(DependencyObject element)
        {
            return (double)element.GetValue(PressOffsetProperty);
        }

        public static void SetPressOffset(DependencyObject element, double value)
        {
            element.SetValue(PressOffsetProperty, value);
        }

        public static bool GetHoverMotion(DependencyObject element)
        {
            return (bool)element.GetValue(HoverMotionProperty);
        }

        public static void SetHoverMotion(DependencyObject element, bool value)
        {
            element.SetValue(HoverMotionProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as Button;
            if (button == null)
            {
                return;
            }

            if (e.NewValue is bool enabled && enabled)
            {
                Attach(button);
            }
            else
            {
                Detach(button);
            }
        }

        private static void Attach(Button button)
        {
            var state = States.GetOrCreateValue(button);
            if (state.Attached)
            {
                return;
            }

            state.Attached = true;
            button.Loaded += OnLoaded;
            button.Unloaded += OnUnloaded;
            button.MouseEnter += OnMouseEnter;
            button.MouseLeave += OnMouseLeave;
            button.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            button.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            button.LostMouseCapture += OnLostMouseCapture;
            button.IsEnabledChanged += OnIsEnabledChangedEvent;

            if (button.IsLoaded)
            {
                Initialize(button, state);
            }
        }

        private static void Detach(Button button)
        {
            MotionState state;
            if (!States.TryGetValue(button, out state))
            {
                return;
            }

            state.Attached = false;
            button.Loaded -= OnLoaded;
            button.Unloaded -= OnUnloaded;
            button.MouseEnter -= OnMouseEnter;
            button.MouseLeave -= OnMouseLeave;
            button.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            button.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
            button.LostMouseCapture -= OnLostMouseCapture;
            button.IsEnabledChanged -= OnIsEnabledChangedEvent;

            if (state.Shift != null)
            {
                state.Shift.BeginAnimation(TranslateTransform.XProperty, null);
                state.Shift.BeginAnimation(TranslateTransform.YProperty, null);
                state.Shift = null;
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            MotionState state;
            if (States.TryGetValue(button, out state))
            {
                Initialize(button, state);
            }
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            MotionState state;
            if (States.TryGetValue(button, out state) && state.Shift != null)
            {
                state.Shift.BeginAnimation(TranslateTransform.XProperty, null);
                state.Shift.BeginAnimation(TranslateTransform.YProperty, null);
            }
        }

        private static void Initialize(Button button, MotionState state)
        {
            if (state.Shift != null || button.Template == null)
            {
                return;
            }

            var face = button.Template.FindName("Face", button) as FrameworkElement;
            if (face == null)
            {
                return;
            }

            // Never animate the template's frozen transform. Replace it with a
            // per-button instance so hover/press animation work reliably.
            var translate = new TranslateTransform();
            face.RenderTransform = translate;
            state.Shift = translate;

            SetImmediate(state, TargetOffset(button, state));
        }

        private static void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            MotionState state;
            if (States.TryGetValue(button, out state) && state.Shift != null)
            {
                if (state.Pressed)
                {
                    Animate(state, PressOffset(button), HoverDurationMs);
                }
                else if (GetHoverMotion(button))
                {
                    Animate(state, PressOffset(button) / 2.0, HoverDurationMs);
                }
                else
                {
                    Animate(state, 0, HoverDurationMs);
                }
            }
        }

        private static void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            MotionState state;
            if (States.TryGetValue(button, out state) && state.Shift != null)
            {
                Animate(state, 0, ReleaseDurationMs);
            }
        }

        private static void OnPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            MotionState state;
            if (States.TryGetValue(button, out state))
            {
                state.Pressed = true;
                if (state.Shift != null)
                {
                    Animate(state, PressOffset(button), PressDurationMs);
                }
            }
        }

        private static void OnPreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            MotionState state;
            if (States.TryGetValue(button, out state))
            {
                state.Pressed = false;
                if (state.Shift != null)
                {
                    Animate(state, GetHoverMotion(button) && button.IsMouseOver ? PressOffset(button) / 2.0 : 0, ReleaseDurationMs);
                }
            }
        }

        private static void OnLostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            MotionState state;
            if (States.TryGetValue(button, out state))
            {
                state.Pressed = false;
                if (state.Shift != null)
                {
                    Animate(state, GetHoverMotion(button) && button.IsMouseOver ? PressOffset(button) / 2.0 : 0, ReleaseDurationMs);
                }
            }
        }

        private static void OnIsEnabledChangedEvent(object sender, DependencyPropertyChangedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            MotionState state;
            if (States.TryGetValue(button, out state) && state.Shift != null)
            {
                Animate(state, TargetOffset(button, state), ReleaseDurationMs);
            }
        }

        private static double TargetOffset(Button button, MotionState state)
        {
            if (!button.IsEnabled)
            {
                return 0;
            }

            if (state.Pressed)
            {
                return PressOffset(button);
            }

            return GetHoverMotion(button) && button.IsMouseOver ? PressOffset(button) / 2.0 : 0;
        }

        private static double PressOffset(Button button)
        {
            return GetPressOffset(button);
        }

        private static void SetImmediate(MotionState state, double value)
        {
            if (state.Shift == null)
            {
                return;
            }

            state.Shift.BeginAnimation(TranslateTransform.XProperty, null);
            state.Shift.BeginAnimation(TranslateTransform.YProperty, null);
            state.Shift.X = value;
            state.Shift.Y = value;
        }

        private static void Animate(MotionState state, double value, int durationMs)
        {
            if (state.Shift == null)
            {
                return;
            }

            var easing = new SineEase { EasingMode = EasingMode.EaseOut };
            var duration = TimeSpan.FromMilliseconds(durationMs);
            var animationX = new DoubleAnimation(value, duration) { EasingFunction = easing };
            var animationY = new DoubleAnimation(value, duration) { EasingFunction = easing };
            state.Shift.BeginAnimation(TranslateTransform.XProperty, animationX, HandoffBehavior.SnapshotAndReplace);
            state.Shift.BeginAnimation(TranslateTransform.YProperty, animationY, HandoffBehavior.SnapshotAndReplace);
        }

        private sealed class MotionState
        {
            public bool Attached;
            public bool Pressed;
            public TranslateTransform? Shift;
        }
    }
}
