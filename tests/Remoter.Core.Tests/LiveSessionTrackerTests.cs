using Remoter.Core.Sessions;
using Xunit;

namespace Remoter.Core.Tests;

public class LiveSessionTrackerTests
{
    private static readonly Guid Home = Guid.NewGuid();
    private static readonly Guid Lab = Guid.NewGuid();

    [Fact]
    public void NothingIsLiveToStartWith()
    {
        Assert.False(new LiveSessionTracker().IsLive(Home));
    }

    [Fact]
    public void ALiveSessionMarksItsConnection()
    {
        var tracker = new LiveSessionTracker();
        tracker.Update(Home, new object(), live: true);

        Assert.True(tracker.IsLive(Home));
        Assert.False(tracker.IsLive(Lab));
    }

    [Fact]
    public void StaysLiveUntilTheLastOfSeveralSessionsGoes()
    {
        var tracker = new LiveSessionTracker();
        var first = new object();
        var second = new object();
        tracker.Update(Home, first, true);
        tracker.Update(Home, second, true);

        tracker.Update(Home, first, false);
        Assert.True(tracker.IsLive(Home));

        tracker.Update(Home, second, false);
        Assert.False(tracker.IsLive(Home));
    }

    [Fact]
    public void RemovingASessionForgetsIt()
    {
        var tracker = new LiveSessionTracker();
        var session = new object();
        tracker.Update(Home, session, true);

        tracker.Remove(session);

        Assert.False(tracker.IsLive(Home));
    }

    [Fact]
    public void VersionMovesOnlyWhenAConnectionFlips()
    {
        var tracker = new LiveSessionTracker();
        var first = new object();
        var second = new object();
        var raised = 0;
        tracker.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(LiveSessionTracker.Version)) raised++; };

        tracker.Update(Home, first, true);   // Home goes live
        tracker.Update(Home, second, true);  // still live, no change
        tracker.Update(Home, first, true);   // already counted, no change
        tracker.Update(Home, first, false);  // second is still open, no change
        tracker.Update(Home, second, false); // Home stops being live

        Assert.Equal(2, raised);
        Assert.Equal(2, tracker.Version);
    }

    [Fact]
    public void ForgettingAnUnknownSessionIsHarmless()
    {
        var tracker = new LiveSessionTracker();
        tracker.Update(Home, new object(), false);
        tracker.Remove(new object());

        Assert.Equal(0, tracker.Version);
        Assert.False(tracker.IsLive(Home));
    }

    [Fact]
    public void SessionsAreTrackedByIdentityNotEquality()
    {
        // Two sessions that compare equal are still two sessions.
        var tracker = new LiveSessionTracker();
        var a = "same";
        var b = new string("same".ToCharArray());
        tracker.Update(Home, a, true);
        tracker.Update(Home, b, true);

        tracker.Update(Home, a, false);

        Assert.True(tracker.IsLive(Home));
    }
}
