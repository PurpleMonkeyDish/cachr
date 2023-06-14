namespace Cachr.Core.Protocol;

public class ProtocolViolationException : Exception
{
    public ProtocolViolationException(string message)
        : base(message)
    {

    }
}