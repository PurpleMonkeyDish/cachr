namespace Cachr.Core.Storage;

public interface IKeyPathProvider
{
    string ComputePath(string key);
}