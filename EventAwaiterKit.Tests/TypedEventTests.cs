namespace EventAwaiterKit.Tests;

[TestClass]
public sealed class TypedEventTests
{
    [TestMethod]
    public async Task ActionPayloadPreservesNullDefaultAndReferenceIdentity()
    {
        var reference = new object();
        foreach (var value in new object?[] { null, reference })
        {
            var result = await EventAwaiter.WaitForEventAsync<object?>((Action<object?> h) => h(value), _ => { });
            Assert.IsTrue(result.Occurred);
            Assert.AreSame(value, result.Value);
        }
        var zero = await EventAwaiter.WaitForEventAsync<int>((Action<int> h) => h(0), _ => { });
        Assert.IsTrue(zero.Occurred);
        Assert.AreEqual(0, zero.Value);
    }

    [TestMethod]
    public async Task EventArgsPreserveDerivedReferenceAndNull()
    {
        var payload = new Payload { Number = 42 };
        foreach (var value in new Payload?[] { null, payload })
        {
            var result = await EventAwaiter.WaitForEventAsync<Payload?>((EventHandler<Payload?> h) => h(this, value), _ => { });
            Assert.IsTrue(result.Occurred);
            Assert.AreSame(value, result.Value);
        }
    }

    [TestMethod]
    public async Task TimeoutAndDefaultResultHaveNoValue()
    {
        var action = await EventAwaiter.WaitForEventAsync<int>((Action<int> _) => { }, _ => { }, TimeSpan.Zero);
        var eventHandler = await EventAwaiter.WaitForEventAsync<Payload>((EventHandler<Payload> _) => { }, _ => { }, TimeSpan.Zero);
        Assert.IsFalse(action.Occurred);
        Assert.IsFalse(eventHandler.Occurred);
        Assert.Throws<InvalidOperationException>(() => action.Value);
        Assert.Throws<InvalidOperationException>(() => eventHandler.Value);
        Assert.Throws<InvalidOperationException>(() => default(EventWaitResult<int>).Value);
    }

    [TestMethod]
    public async Task FirstPayloadWinsAndLateValuesDoNotReplaceIt()
    {
        Action<string>? callback = null;
        var wait = EventAwaiter.WaitForEventAsync<string>(h => callback = h, _ => { });
        callback!("first");
        callback("second");
        var result = await wait.WaitAsync(TimeSpan.FromSeconds(5));
        callback("late");
        Assert.AreEqual("first", result.Value);
    }

    [TestMethod]
    public async Task ConcurrentPayloadResultIsOneOfThePublishedObjects()
    {
        var first = new object();
        var second = new object();
        Action<object>? callback = null;
        var wait = EventAwaiter.WaitForEventAsync<object>(h => callback = h, _ => { });
        await Task.WhenAll(Task.Run(() => callback!(first)), Task.Run(() => callback!(second)));
        var result = await wait.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(ReferenceEquals(first, result.Value) || ReferenceEquals(second, result.Value));
    }

    [TestMethod]
    public async Task TypedNullAccessorsFaultBeforeSubscription()
    {
        var calls = 0;
        await Assert.ThrowsAsync<ArgumentNullException>(() => EventAwaiter.WaitForEventAsync<int>(
            (Action<Action<int>>)null!, _ => calls++));
        await Assert.ThrowsAsync<ArgumentNullException>(() => EventAwaiter.WaitForEventAsync<int>(
            (Action<int> _) => calls++, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => EventAwaiter.WaitForEventAsync<EventArgs>(
            (Action<EventHandler<EventArgs>>)null!, _ => calls++));
        await Assert.ThrowsAsync<ArgumentNullException>(() => EventAwaiter.WaitForEventAsync<EventArgs>(
            (EventHandler<EventArgs> _) => calls++, null!));
        Assert.AreEqual(0, calls);
    }

    [TestMethod]
    public async Task ExistingAndTypedEventSubscriptionsCompileAndRun()
    {
        var source = new Source();
        var action = EventAwaiter.WaitForEventAsync(h => source.Action += h, h => source.Action -= h);
        var eventHandler = EventAwaiter.WaitForEventAsync(h => source.Event += h, h => source.Event -= h);
        var typedAction = EventAwaiter.WaitForEventAsync<int>(h => source.TypedAction += h, h => source.TypedAction -= h);
        var typedEvent = EventAwaiter.WaitForEventAsync<Payload>(h => source.TypedEvent += h, h => source.TypedEvent -= h);
        source.Raise();
        Assert.IsTrue(await action);
        Assert.IsTrue(await eventHandler);
        Assert.AreEqual(42, (await typedAction).Value);
        Assert.AreEqual(42, (await typedEvent).Value.Number);
    }

    private sealed class Payload : EventArgs
    {
        internal int Number { get; init; }
    }

    private sealed class Source
    {
        internal event Action? Action;
        internal event EventHandler? Event;
        internal event Action<int>? TypedAction;
        internal event EventHandler<Payload>? TypedEvent;
        internal void Raise()
        {
            Action?.Invoke();
            Event?.Invoke(this, EventArgs.Empty);
            TypedAction?.Invoke(42);
            TypedEvent?.Invoke(this, new Payload { Number = 42 });
        }
    }
}
