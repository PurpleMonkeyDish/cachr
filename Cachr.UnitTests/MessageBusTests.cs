using System;
using System.Threading;
using System.Threading.Tasks;
using Cachr.Core.Messaging;
using Moq;
using Xunit;

namespace Cachr.UnitTests;

public class MessageBusTests
{
    [Fact]
    public async Task MessageBusBroadcastDisposesMessagesWithIDisposable()
    {
        var mockDisposable = new Mock<IDisposable>();
        var bus = new MessageBus<IDisposable>(new MessageBusOptions());
        await bus.BroadcastAsync(mockDisposable.Object, CancellationToken.None).ConfigureAwait(false);
        await bus.ShutdownAsync().ConfigureAwait(false);

        mockDisposable.Verify(i => i.Dispose(), Times.Once);
        mockDisposable.Reset();
    }

    [Fact]
    public async Task MessageBusSendToRandomDisposesMessagesWithIDisposable()
    {
        var mockDisposable = new Mock<IDisposable>();
        var bus = new MessageBus<IDisposable>(new MessageBusOptions());
        await bus.SendToRandomAsync(mockDisposable.Object, CancellationToken.None).ConfigureAwait(false);
        await bus.ShutdownAsync().ConfigureAwait(false);
        mockDisposable.Verify(i => i.Dispose(), Times.Once);
    }

    [Fact]
    public async Task MessageBusBroadcastCompletesMessagesWithICompletableMessage()
    {
        var mockDisposable = new Mock<ICompletableMessage>();
        var bus = new MessageBus<ICompletableMessage>(new MessageBusOptions());
        await bus.BroadcastAsync(mockDisposable.Object, CancellationToken.None).ConfigureAwait(false);
        await bus.ShutdownAsync().ConfigureAwait(false);

        mockDisposable.Verify(i => i.Complete(), Times.Once);
        mockDisposable.Reset();
    }

    [Fact]
    public async Task MessageBusSendToRandomCompletesMessagesWithICompletableMessage()
    {
        var mockDisposable = new Mock<ICompletableMessage>();
        var bus = new MessageBus<ICompletableMessage>(new MessageBusOptions());
        await bus.SendToRandomAsync(mockDisposable.Object, CancellationToken.None).ConfigureAwait(false);
        await bus.ShutdownAsync().ConfigureAwait(false);

        mockDisposable.Verify(i => i.Complete(), Times.Once);
        mockDisposable.Reset();
    }
}
