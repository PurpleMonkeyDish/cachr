using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace Cachr.Core.Messaging;

public sealed class MessageBusOptions : IOptions<MessageBusOptions>
{
    public const int DefaultCapacity = 1000;
    public const BoundedChannelFullMode DefaultFullMode = BoundedChannelFullMode.DropOldest;
    public int Capacity { get; init; } = DefaultCapacity;
    public BoundedChannelFullMode FullMode { get; init; } = DefaultFullMode;

    public MessageBusOptions Value => this;

    public BoundedChannelOptions CreateChannelOptions()
    {
        return new(Capacity)
        {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.DropOldest
        };
    }
}
