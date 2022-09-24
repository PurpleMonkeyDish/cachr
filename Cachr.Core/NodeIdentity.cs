namespace Cachr.Core;

public static class NodeIdentity
{
    private static string? s_name;
    public static Guid Id { get; } = Guid.NewGuid();

    public static string Name
    {
        get => s_name ??= Environment.MachineName;
        set
        {
            if (s_name != null)
                throw new InvalidOperationException(
                    "The name of this machine has already been set, and cannot be changed.");
            s_name = value;
        }
    }
}
