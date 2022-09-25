namespace Cachr.Core.Peering;

public interface IPeerConnection
{
    Guid Id { get; }
    bool Enabled { get; set; }
    ValueTask CloseAsync();
}