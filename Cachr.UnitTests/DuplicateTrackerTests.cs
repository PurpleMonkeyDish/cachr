using Cachr.Core.Messages.Duplication;
using FsCheck.Xunit;
using Xunit;

namespace Cachr.UnitTests;

public class DuplicateTrackerTests
{
    [Property]
    public void DuplicateTrackerDetectsDuplicates(int value)
    {
        var tracker = new DuplicateTracker<int>();
        Assert.False(tracker.IsDuplicate(value));
        Assert.True(tracker.IsDuplicate(value));
    }

    [Fact]
    public void DuplicateTrackerDuplicatesExpireWhenTooManyAreAdded()
    {
        const int Count = 100000;
        var tracker = new DuplicateTracker<int>();
        for (var x = 0; x < Count; x++)
        {
            Assert.False(tracker.IsDuplicate(x));
            Assert.True(tracker.IsDuplicate(x));
        }


        for (var x = 0; x < Count; x++)
        {
            Assert.False(tracker.IsDuplicate(x));
            Assert.True(tracker.IsDuplicate(x));
        }
    }
}
