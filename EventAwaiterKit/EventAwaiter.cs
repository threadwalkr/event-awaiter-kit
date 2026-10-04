using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventAwaiterKit
{
    /// <summary>Waits for one future event, with optional timeout and cancellation.</summary>
    public static class EventAwaiter
    {
        public static Task<bool> WaitForEventAsync(Action<Action> addHandler, Action<Action> removeHandler, TimeSpan timeout, CancellationToken cancellationToken = default)
            => EventWait.WaitAsync(addHandler, removeHandler, signal => () => signal(true), false, timeout, cancellationToken);

        public static Task<bool> WaitForEventAsync(Action<Action> addHandler, Action<Action> removeHandler, CancellationToken cancellationToken = default)
            => WaitForEventAsync(addHandler, removeHandler, Timeout.InfiniteTimeSpan, cancellationToken);

        public static Task<bool> WaitForEventAsync(Action<EventHandler> addHandler, Action<EventHandler> removeHandler, TimeSpan timeout, CancellationToken cancellationToken = default)
            => EventWait.WaitAsync(addHandler, removeHandler, signal => (sender, args) => signal(true), false, timeout, cancellationToken);

        public static Task<bool> WaitForEventAsync(Action<EventHandler> addHandler, Action<EventHandler> removeHandler, CancellationToken cancellationToken = default)
            => WaitForEventAsync(addHandler, removeHandler, Timeout.InfiniteTimeSpan, cancellationToken);

        public static Task<EventWaitResult<T>> WaitForEventAsync<T>(Action<Action<T>> addHandler, Action<Action<T>> removeHandler, TimeSpan timeout, CancellationToken cancellationToken = default)
            => EventWait.WaitAsync(addHandler, removeHandler,
                signal => value => signal(new EventWaitResult<T>(value)), default(EventWaitResult<T>), timeout, cancellationToken);

        public static Task<EventWaitResult<T>> WaitForEventAsync<T>(Action<Action<T>> addHandler, Action<Action<T>> removeHandler, CancellationToken cancellationToken = default)
            => WaitForEventAsync(addHandler, removeHandler, Timeout.InfiniteTimeSpan, cancellationToken);

        public static Task<EventWaitResult<TEventArgs>> WaitForEventAsync<TEventArgs>(Action<EventHandler<TEventArgs>> addHandler, Action<EventHandler<TEventArgs>> removeHandler, TimeSpan timeout, CancellationToken cancellationToken = default)
            => EventWait.WaitAsync(addHandler, removeHandler,
                signal => (sender, args) => signal(new EventWaitResult<TEventArgs>(args)), default(EventWaitResult<TEventArgs>), timeout, cancellationToken);

        public static Task<EventWaitResult<TEventArgs>> WaitForEventAsync<TEventArgs>(Action<EventHandler<TEventArgs>> addHandler, Action<EventHandler<TEventArgs>> removeHandler, CancellationToken cancellationToken = default)
            => WaitForEventAsync(addHandler, removeHandler, Timeout.InfiniteTimeSpan, cancellationToken);
    }
}
