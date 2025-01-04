using Microsoft.Xaml.Behaviors;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace PlayniteSounds.Common
{
    // From https://stackoverflow.com/questions/949458/how-to-have-a-wpf-binding-update-every-second
    public class PeriodicBindingUpdateBehavior : Behavior<DependencyObject>
    {
        public TimeSpan Interval { get; set; }
        public DependencyProperty Property { get; set; }
        public PeriodicBindingUpdateMode Mode { get; set; } = PeriodicBindingUpdateMode.UpdateTarget;
        private WeakTimer timer;
        private TimerCallback timerCallback;
        protected override void OnAttached()
        {
            if (Property == null) throw new ArgumentNullException(nameof(Property));
            //Save a reference to the callback of the timer so this object will keep the timer alive but not vice versa.
            timerCallback = s =>
            {
                try
                {
                    Action<BindingExpression> action;
                    if (Mode == PeriodicBindingUpdateMode.UpdateTarget) /* Then */ action = e => e?.UpdateTarget();
                    else                                                /* Then */ action = e => e?.UpdateSource();

                    Dispatcher.Invoke(() => action(BindingOperations.GetBindingExpression(AssociatedObject, Property)));
                }
                catch (TaskCanceledException) { }//This exception will be thrown when application is shutting down.
            };
            timer = new WeakTimer(timerCallback, null, Interval, Interval);

            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            timer.Dispose();
            timerCallback = null;
            base.OnDetaching();
        }
    }

    public enum PeriodicBindingUpdateMode
    {
        UpdateTarget, UpdateSource
    }

    /// <summary>
    /// Wraps up a <see cref="System.Threading.Timer"/> with only a <see cref="WeakReference"/> to the callback so that the timer does not prevent GC from collecting the object that uses this timer.
    /// Your object must hold a reference to the callback passed into this timer.
    /// </summary>
    public class WeakTimer : IDisposable
    {
        private Timer timer;
        private WeakReference<TimerCallback> weakCallback;
        public WeakTimer(TimerCallback callback)
        {
            timer = new Timer(OnTimerCallback);
            weakCallback = new WeakReference<TimerCallback>(callback);
        }

        public WeakTimer(TimerCallback callback, object state, int dueTime, int period)
        {
            timer = new Timer(OnTimerCallback, state, dueTime, period);
            weakCallback = new WeakReference<TimerCallback>(callback);
        }

        public WeakTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            timer = new Timer(OnTimerCallback, state, dueTime, period);
            weakCallback = new WeakReference<TimerCallback>(callback);
        }

        public WeakTimer(TimerCallback callback, object state, uint dueTime, uint period)
        {
            timer = new Timer(OnTimerCallback, state, dueTime, period);
            weakCallback = new WeakReference<TimerCallback>(callback);
        }

        public WeakTimer(TimerCallback callback, object state, long dueTime, long period)
        {
            timer = new Timer(OnTimerCallback, state, dueTime, period);
            weakCallback = new WeakReference<TimerCallback>(callback);
        }

        private void OnTimerCallback(object state)
        {
            if (weakCallback.TryGetTarget(out TimerCallback callback))
                callback(state);
            else
                timer.Dispose();
        }

        public bool Change(int dueTime, int period)
        {
            return timer.Change(dueTime, period);
        }
        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            return timer.Change(dueTime, period);
        }

        public bool Change(uint dueTime, uint period)
        {
            return timer.Change(dueTime, period);
        }

        public bool Change(long dueTime, long period)
        {
            return timer.Change(dueTime, period);
        }

        public bool Dispose(WaitHandle notifyObject)
        {
            return timer.Dispose(notifyObject);
        }
        public void Dispose()
        {
            timer.Dispose();
        }
    }
}