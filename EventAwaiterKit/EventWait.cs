using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventAwaiterKit
{
    internal static class EventWait
    {
        internal static Task<TResult> WaitAsync<THandler, TResult>(
            Action<THandler> addHandler, Action<THandler> removeHandler,
            Func<Action<TResult>, THandler> createHandler, TResult timeoutResult,
            TimeSpan timeout, CancellationToken cancellationToken)
        {
            // Return validation failures through the task, as the original async API did.
            if (addHandler == null)
                return Task.FromException<TResult>(new ArgumentNullException(nameof(addHandler)));
            if (removeHandler == null)
                return Task.FromException<TResult>(new ArgumentNullException(nameof(removeHandler)));
            if (timeout != Timeout.InfiniteTimeSpan &&
                (timeout < TimeSpan.Zero || timeout > TimeSpan.FromMilliseconds(int.MaxValue)))
                return Task.FromException<TResult>(new ArgumentOutOfRangeException(nameof(timeout),
                    "Timeout must be infinite or between zero and Int32.MaxValue milliseconds."));
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<TResult>(cancellationToken);
            if (timeout == TimeSpan.Zero)
                return Task.FromResult(timeoutResult);

            var context = SynchronizationContext.Current;
            var signal = new TaskCompletionSource<Outcome<TResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var completion = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var handler = createHandler(value => signal.TrySetResult(new Outcome<TResult>(value, false)));
            Timer timer = null;
            CancellationTokenRegistration registration = default;
            Exception subscriptionError = null;
            var attemptedSubscription = false;

            try
            {
                // These callbacks never invoke accessors or wait for cleanup.
                registration = cancellationToken.Register(() =>
                    signal.TrySetResult(new Outcome<TResult>(default, true)), useSynchronizationContext: false);
                if (timeout != Timeout.InfiniteTimeSpan)
                    timer = new Timer(_ => signal.TrySetResult(new Outcome<TResult>(timeoutResult, false)),
                        null, timeout, Timeout.InfiniteTimeSpan);

                attemptedSubscription = true;
                addHandler(handler);
            }
            catch (Exception ex)
            {
                subscriptionError = ex;
            }

            // Start the sole cleanup owner only after addHandler has returned or thrown.
            // CompleteAsync contains all errors and publishes them through completion.
            _ = CompleteAsync();
            return completion.Task;

            async Task CompleteAsync()
            {
                try
                {
                    var outcome = default(Outcome<TResult>);
                    try
                    {
                        if (subscriptionError == null)
                            outcome = await signal.Task.ConfigureAwait(false);
                    }
                    finally
                    {
                        timer?.Dispose();
                        registration.Dispose();
                    }

                    Exception cleanupError = null;
                    if (attemptedSubscription)
                    {
                        try { await RemoveAsync(() => removeHandler(handler), context).ConfigureAwait(false); }
                        catch (Exception ex) { cleanupError = ex; }
                    }

                    if (subscriptionError != null && cleanupError != null)
                        completion.TrySetException(new AggregateException(subscriptionError, cleanupError));
                    else if (subscriptionError != null || cleanupError != null)
                        completion.TrySetException(subscriptionError ?? cleanupError);
                    else if (outcome.Canceled)
                        completion.TrySetCanceled(cancellationToken);
                    else
                        completion.TrySetResult(outcome.Value);
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
            }
        }

        private static Task RemoveAsync(Action remove, SynchronizationContext context)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var invoked = 0;

            void Invoke(object state)
            {
                if (Interlocked.Exchange(ref invoked, 1) != 0)
                    return;
                try
                {
                    remove();
                    completion.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
            }

            if (context == null || ReferenceEquals(context, SynchronizationContext.Current))
                Invoke(null);
            else
            {
                try { context.Post(Invoke, null); }
                catch (Exception ex)
                {
                    // Suppress a queued callback if Post enqueued it and then threw.
                    if (Interlocked.CompareExchange(ref invoked, 1, 0) == 0)
                        completion.TrySetException(ex);
                }
            }

            return completion.Task;
        }

        private readonly struct Outcome<T>
        {
            internal Outcome(T value, bool canceled) { Value = value; Canceled = canceled; }
            internal T Value { get; }
            internal bool Canceled { get; }
        }
    }
}
