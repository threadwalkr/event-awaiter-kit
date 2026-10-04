using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventAwaiterKit
{
    /// <summary>Waits for one future event, with optional timeout and cancellation.</summary>
    public static class EventAwaiter
    {
        /// <summary>Waits for one event, discarding any event data, or a timeout.</summary>
        /// <param name="addHandler">Attaches the supplied handler synchronously before this call returns.</param>
        /// <param name="removeHandler">Detaches the same handler once, including after failed attachment; must tolerate an absent handler.</param>
        /// <param name="timeout">Exactly Timeout.InfiniteTimeSpan, or zero through Int32.MaxValue milliseconds inclusive. Zero does not subscribe.</param>
        /// <param name="cancellationToken">Cancels waiting, not the underlying operation. Pre-cancellation prevents subscription.</param>
        /// <returns>True for an event, or false for timeout, after cleanup finishes.</returns>
        /// <exception cref="ArgumentNullException">An accessor is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The timeout is outside the supported range.</exception>
        /// <exception cref="OperationCanceledException">Cancellation wins and cleanup succeeds.</exception>
        /// <exception cref="AggregateException">Both addition and cleanup fail, with the addition exception first.</exception>
        /// <remarks>
        /// The first event/timeout/cancellation signal wins. Addition failure overrides that signal; cleanup failure overrides an ordinary outcome.
        /// Accessor and context-dispatch errors fault the task, including OperationCanceledException thrown by an accessor.
        /// Cleanup uses the captured SynchronizationContext when present; keep it alive and await asynchronously.
        /// Accessors must return promptly. A failed remover or rejected dispatch may leave the handler attached.
        /// </remarks>
        public static Task<bool> WaitForEventAsync(Action<Action> addHandler, Action<Action> removeHandler, TimeSpan timeout, CancellationToken cancellationToken = default)
            => EventWait.WaitAsync(addHandler, removeHandler, signal => () => signal(true), false, timeout, cancellationToken);

        /// <summary>Waits for one event, discarding any event data without a timeout.</summary>
        /// <param name="addHandler">Attaches the supplied handler synchronously before this call returns.</param>
        /// <param name="removeHandler">Detaches the same handler once, including after failed attachment; must tolerate an absent handler.</param>
        /// <param name="cancellationToken">Cancels waiting, not the underlying operation. Pre-cancellation prevents subscription.</param>
        /// <returns>True when the event occurs, after cleanup finishes.</returns>
        /// <exception cref="ArgumentNullException">An accessor is null.</exception>
        /// <exception cref="OperationCanceledException">Cancellation wins and cleanup succeeds.</exception>
        /// <exception cref="AggregateException">Both addition and cleanup fail, with the addition exception first.</exception>
        /// <remarks>
        /// The first event/timeout/cancellation signal wins. Addition failure overrides that signal; cleanup failure overrides an ordinary outcome.
        /// Accessor and context-dispatch errors fault the task, including OperationCanceledException thrown by an accessor.
        /// Cleanup uses the captured SynchronizationContext when present; keep it alive and await asynchronously.
        /// Accessors must return promptly. A failed remover or rejected dispatch may leave the handler attached.
        /// </remarks>
        public static Task<bool> WaitForEventAsync(Action<Action> addHandler, Action<Action> removeHandler, CancellationToken cancellationToken = default)
            => WaitForEventAsync(addHandler, removeHandler, Timeout.InfiniteTimeSpan, cancellationToken);

        /// <summary>Waits for one event, discarding any event data, or a timeout.</summary>
        /// <param name="addHandler">Attaches the supplied handler synchronously before this call returns.</param>
        /// <param name="removeHandler">Detaches the same handler once, including after failed attachment; must tolerate an absent handler.</param>
        /// <param name="timeout">Exactly Timeout.InfiniteTimeSpan, or zero through Int32.MaxValue milliseconds inclusive. Zero does not subscribe.</param>
        /// <param name="cancellationToken">Cancels waiting, not the underlying operation. Pre-cancellation prevents subscription.</param>
        /// <returns>True for an event, or false for timeout, after cleanup finishes.</returns>
        /// <exception cref="ArgumentNullException">An accessor is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The timeout is outside the supported range.</exception>
        /// <exception cref="OperationCanceledException">Cancellation wins and cleanup succeeds.</exception>
        /// <exception cref="AggregateException">Both addition and cleanup fail, with the addition exception first.</exception>
        /// <remarks>
        /// The first event/timeout/cancellation signal wins. Addition failure overrides that signal; cleanup failure overrides an ordinary outcome.
        /// Accessor and context-dispatch errors fault the task, including OperationCanceledException thrown by an accessor.
        /// Cleanup uses the captured SynchronizationContext when present; keep it alive and await asynchronously.
        /// Accessors must return promptly. A failed remover or rejected dispatch may leave the handler attached.
        /// </remarks>
        public static Task<bool> WaitForEventAsync(Action<EventHandler> addHandler, Action<EventHandler> removeHandler, TimeSpan timeout, CancellationToken cancellationToken = default)
            => EventWait.WaitAsync(addHandler, removeHandler, signal => (sender, args) => signal(true), false, timeout, cancellationToken);

        /// <summary>Waits for one event, discarding any event data without a timeout.</summary>
        /// <param name="addHandler">Attaches the supplied handler synchronously before this call returns.</param>
        /// <param name="removeHandler">Detaches the same handler once, including after failed attachment; must tolerate an absent handler.</param>
        /// <param name="cancellationToken">Cancels waiting, not the underlying operation. Pre-cancellation prevents subscription.</param>
        /// <returns>True when the event occurs, after cleanup finishes.</returns>
        /// <exception cref="ArgumentNullException">An accessor is null.</exception>
        /// <exception cref="OperationCanceledException">Cancellation wins and cleanup succeeds.</exception>
        /// <exception cref="AggregateException">Both addition and cleanup fail, with the addition exception first.</exception>
        /// <remarks>
        /// The first event/timeout/cancellation signal wins. Addition failure overrides that signal; cleanup failure overrides an ordinary outcome.
        /// Accessor and context-dispatch errors fault the task, including OperationCanceledException thrown by an accessor.
        /// Cleanup uses the captured SynchronizationContext when present; keep it alive and await asynchronously.
        /// Accessors must return promptly. A failed remover or rejected dispatch may leave the handler attached.
        /// </remarks>
        public static Task<bool> WaitForEventAsync(Action<EventHandler> addHandler, Action<EventHandler> removeHandler, CancellationToken cancellationToken = default)
            => WaitForEventAsync(addHandler, removeHandler, Timeout.InfiniteTimeSpan, cancellationToken);

        /// <summary>Waits for one event payload, or a timeout.</summary>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <param name="addHandler">Attaches the supplied handler synchronously before this call returns.</param>
        /// <param name="removeHandler">Detaches the same handler once, including after failed attachment; must tolerate an absent handler.</param>
        /// <param name="timeout">Exactly Timeout.InfiniteTimeSpan, or zero through Int32.MaxValue milliseconds inclusive. Zero does not subscribe.</param>
        /// <param name="cancellationToken">Cancels waiting, not the underlying operation. Pre-cancellation prevents subscription.</param>
        /// <returns>The event payload after cleanup. Occurred distinguishes an event, including a null/default payload, from timeout.</returns>
        /// <exception cref="ArgumentNullException">An accessor is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The timeout is outside the supported range.</exception>
        /// <exception cref="OperationCanceledException">Cancellation wins and cleanup succeeds.</exception>
        /// <exception cref="AggregateException">Both addition and cleanup fail, with the addition exception first.</exception>
        /// <remarks>
        /// The first event/timeout/cancellation signal wins. Addition failure overrides that signal; cleanup failure overrides an ordinary outcome.
        /// Accessor and context-dispatch errors fault the task, including OperationCanceledException thrown by an accessor.
        /// Cleanup uses the captured SynchronizationContext when present; keep it alive and await asynchronously.
        /// Accessors must return promptly. A failed remover or rejected dispatch may leave the handler attached.
        /// </remarks>
        public static Task<EventWaitResult<T>> WaitForEventAsync<T>(Action<Action<T>> addHandler, Action<Action<T>> removeHandler, TimeSpan timeout, CancellationToken cancellationToken = default)
            => EventWait.WaitAsync(addHandler, removeHandler,
                signal => value => signal(new EventWaitResult<T>(value)), default(EventWaitResult<T>), timeout, cancellationToken);

        /// <summary>Waits for one event payload without a timeout.</summary>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <param name="addHandler">Attaches the supplied handler synchronously before this call returns.</param>
        /// <param name="removeHandler">Detaches the same handler once, including after failed attachment; must tolerate an absent handler.</param>
        /// <param name="cancellationToken">Cancels waiting, not the underlying operation. Pre-cancellation prevents subscription.</param>
        /// <returns>The event payload after cleanup, with Occurred set to true.</returns>
        /// <exception cref="ArgumentNullException">An accessor is null.</exception>
        /// <exception cref="OperationCanceledException">Cancellation wins and cleanup succeeds.</exception>
        /// <exception cref="AggregateException">Both addition and cleanup fail, with the addition exception first.</exception>
        /// <remarks>
        /// The first event/timeout/cancellation signal wins. Addition failure overrides that signal; cleanup failure overrides an ordinary outcome.
        /// Accessor and context-dispatch errors fault the task, including OperationCanceledException thrown by an accessor.
        /// Cleanup uses the captured SynchronizationContext when present; keep it alive and await asynchronously.
        /// Accessors must return promptly. A failed remover or rejected dispatch may leave the handler attached.
        /// </remarks>
        public static Task<EventWaitResult<T>> WaitForEventAsync<T>(Action<Action<T>> addHandler, Action<Action<T>> removeHandler, CancellationToken cancellationToken = default)
            => WaitForEventAsync(addHandler, removeHandler, Timeout.InfiniteTimeSpan, cancellationToken);

        /// <summary>Waits for one event payload, or a timeout.</summary>
        /// <typeparam name="TEventArgs">The event payload type.</typeparam>
        /// <param name="addHandler">Attaches the supplied handler synchronously before this call returns.</param>
        /// <param name="removeHandler">Detaches the same handler once, including after failed attachment; must tolerate an absent handler.</param>
        /// <param name="timeout">Exactly Timeout.InfiniteTimeSpan, or zero through Int32.MaxValue milliseconds inclusive. Zero does not subscribe.</param>
        /// <param name="cancellationToken">Cancels waiting, not the underlying operation. Pre-cancellation prevents subscription.</param>
        /// <returns>The event payload after cleanup. Occurred distinguishes an event, including a null/default payload, from timeout.</returns>
        /// <exception cref="ArgumentNullException">An accessor is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The timeout is outside the supported range.</exception>
        /// <exception cref="OperationCanceledException">Cancellation wins and cleanup succeeds.</exception>
        /// <exception cref="AggregateException">Both addition and cleanup fail, with the addition exception first.</exception>
        /// <remarks>
        /// The first event/timeout/cancellation signal wins. Addition failure overrides that signal; cleanup failure overrides an ordinary outcome.
        /// Accessor and context-dispatch errors fault the task, including OperationCanceledException thrown by an accessor.
        /// Cleanup uses the captured SynchronizationContext when present; keep it alive and await asynchronously.
        /// Accessors must return promptly. A failed remover or rejected dispatch may leave the handler attached.
        /// </remarks>
        public static Task<EventWaitResult<TEventArgs>> WaitForEventAsync<TEventArgs>(Action<EventHandler<TEventArgs>> addHandler, Action<EventHandler<TEventArgs>> removeHandler, TimeSpan timeout, CancellationToken cancellationToken = default)
            => EventWait.WaitAsync(addHandler, removeHandler,
                signal => (sender, args) => signal(new EventWaitResult<TEventArgs>(args)), default(EventWaitResult<TEventArgs>), timeout, cancellationToken);

        /// <summary>Waits for one event payload without a timeout.</summary>
        /// <typeparam name="TEventArgs">The event payload type.</typeparam>
        /// <param name="addHandler">Attaches the supplied handler synchronously before this call returns.</param>
        /// <param name="removeHandler">Detaches the same handler once, including after failed attachment; must tolerate an absent handler.</param>
        /// <param name="cancellationToken">Cancels waiting, not the underlying operation. Pre-cancellation prevents subscription.</param>
        /// <returns>The event payload after cleanup, with Occurred set to true.</returns>
        /// <exception cref="ArgumentNullException">An accessor is null.</exception>
        /// <exception cref="OperationCanceledException">Cancellation wins and cleanup succeeds.</exception>
        /// <exception cref="AggregateException">Both addition and cleanup fail, with the addition exception first.</exception>
        /// <remarks>
        /// The first event/timeout/cancellation signal wins. Addition failure overrides that signal; cleanup failure overrides an ordinary outcome.
        /// Accessor and context-dispatch errors fault the task, including OperationCanceledException thrown by an accessor.
        /// Cleanup uses the captured SynchronizationContext when present; keep it alive and await asynchronously.
        /// Accessors must return promptly. A failed remover or rejected dispatch may leave the handler attached.
        /// </remarks>
        public static Task<EventWaitResult<TEventArgs>> WaitForEventAsync<TEventArgs>(Action<EventHandler<TEventArgs>> addHandler, Action<EventHandler<TEventArgs>> removeHandler, CancellationToken cancellationToken = default)
            => WaitForEventAsync(addHandler, removeHandler, Timeout.InfiniteTimeSpan, cancellationToken);
    }
}
