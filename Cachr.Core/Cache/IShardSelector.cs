namespace Cachr.Core.Cache;

public interface IShardSelector
{
    int ShardCount { get; }
    int SelectShard(string key);
}