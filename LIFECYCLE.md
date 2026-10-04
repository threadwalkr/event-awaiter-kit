# Event wait contract

All event shapes use the same lifecycle. Accessors must return promptly and must not synchronously wait for the returned task.

## Subscription and completion

1. Null accessors and invalid timeouts fault the returned task before subscription. A timeout must be exactly `Timeout.InfiniteTimeSpan` (-1 ms), or between zero and `Int32.MaxValue` milliseconds inclusive. Other negative values, including fractions of a millisecond, are invalid. Timer scheduling has millisecond granularity and is not a real-time deadline.
2. An already-canceled token produces a canceled task without subscription, even with zero timeout. Otherwise zero timeout returns the timeout result without subscription.
3. For an ordinary wait, cancellation and timeout monitoring begin immediately before subscription. The add accessor runs synchronously before the call returns its task. Create the wait, start the operation, then await. Accessors cannot be interrupted by timeout or cancellation.
4. The first event, timeout, or cancellation signal to win task completion selects the outcome. Concurrent signals have no priority or guaranteed wall-clock order. Later signals do nothing.
5. After the subscription attempt exits, one owner disposes monitoring resources and performs cleanup. Removal receives the exact handler instance supplied to addition, and is attempted once even when addition throws: an accessor might attach and then throw. A remover must tolerate that handler being absent.
6. The public task completes only after cleanup finishes or fails. The helper cancels waiting, not the operation that produces events.

## Exceptions

| Condition | Returned task |
| --- | --- |
| Event | Successful event result after cleanup |
| Timeout | Successful timeout result after cleanup |
| Cancellation | Canceled with the supplied token after cleanup |
| Addition throws, including after signaling | Faulted with the original addition exception after cleanup |
| Removal throws | Faulted with the original removal exception, overriding event/timeout/cancellation |
| Addition and removal both throw | Faulted with `AggregateException`, addition first and removal second |
| Captured context rejects cleanup dispatch | Faulted with the dispatch exception; no fallback to another thread |

Accessor exceptions, including `OperationCanceledException`, are faults rather than token cancellation. They do not escape through event, timer, or cancellation callbacks. A remover that throws can leave a subscription attached; successful cleanup is required for the no-retained-subscription guarantee.

## Threading

Addition runs on the caller. Cleanup uses the caller's captured `SynchronizationContext`, when present, or runs without thread affinity otherwise. A synchronization context is not universally a particular physical thread. Call from the source's owning context or supply accessors that dispatch to its owner.

Keep the captured context alive and processing work until the wait finishes. Use `await`; blocking the UI with `.Wait()` or `.Result` can deadlock cleanup. A context that accepts a post but never executes it leaves the task pending. A rejecting context faults the wait, and the caller may need to arrange detachment once the context is usable again.

Completion callbacks only signal the wait. Consumer continuations are not deliberately run inline inside those callbacks. Synchronous events raised during addition are supported: removal waits until addition exits. Cleanup can itself raise events or request cancellation without selecting a second outcome.
