using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EventAwaiterKit.Tests;

[TestClass]
public sealed class LifecycleRegressionTests
{
    [TestMethod]
    public async Task Event_RemovesExactlyOnce()
    {
        Action? handler = null;
        var removes = 0;
        var wait = EventAwaiter.WaitForEventAsync(h => handler = h, _ => removes++);
        handler!();
        Assert.IsTrue(await wait.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.AreEqual(1, removes);
    }

    [TestMethod]
    public async Task SynchronousEvent_DoesNotRemoveUntilAddReturns()
    {
        var insideAdd = false;
        var removedInsideAdd = false;
        var wait = EventAwaiter.WaitForEventAsync((Action h) =>
        {
            insideAdd = true;
            h();
            insideAdd = false;
        }, _ => removedInsideAdd |= insideAdd);
        Assert.IsTrue(await wait.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.IsFalse(removedInsideAdd);
    }

    [TestMethod]
    public async Task ThrowingRemove_FaultsWaitWithoutThrowingFromEvent()
    {
        Action? handler = null;
        var failure = new InvalidOperationException("remove failed");
        var wait = EventAwaiter.WaitForEventAsync(h => handler = h, _ => throw failure);
        Exception? callbackFailure = null;
        try { handler!(); }
        catch (Exception ex) { callbackFailure = ex; }
        // Checking completion first also bounds this regression on the old implementation.
        var completed = await Task.WhenAny(wait, Task.Delay(1000));
        Assert.AreSame(wait, completed, "Removal failure left the waiter pending.");
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => wait);
        Assert.AreSame(failure, actual);
        Assert.IsNull(callbackFailure);
    }
}
