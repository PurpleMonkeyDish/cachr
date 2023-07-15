using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Cachr.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cachr.Core.Cache;

public class CacheStorage : ICacheStorage
{
    private readonly ObjectStorageContext _context;
    private readonly IDataMapper _dataMapper;
    private readonly ICacheFileManager _fileManager;
    private readonly ILogger<CacheStorage> _logger;

    public CacheStorage(ObjectStorageContext context,
        IDataMapper dataMapper,
        ICacheFileManager fileManager,
        ILogger<CacheStorage> logger)
    {
        _context = context;
        _dataMapper = dataMapper;
        _fileManager = fileManager;
        _logger = logger;
    }

    public async Task<CacheEntry?> GetMetadataAsync(string key, int shard, CancellationToken cancellationToken)
    {
        var storedObject = await _context.StoredObjects
            .Include(i => i.Metadata)
            .Where(i => i.Key == key)
            .FirstOrDefaultAsync(cancellationToken);

        return _dataMapper.MapCacheEntryData(storedObject);
    }

    public async Task<CacheEntry?> UpdateExpiration(string key,
        int shard,
        DateTimeOffset? absoluteExpiration,
        TimeSpan? slidingExpiration,
        CancellationToken cancellationToken
    )
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var metadata = await GetMetadataAsync(key, shard, cancellationToken);
        if (metadata is null)
        {
            return null;
        }

        var metadataEntry = await _context.ObjectMetadata.Where(i => i.Id == metadata.MetadataId)
            .SingleAsync(cancellationToken);
        metadataEntry.SlidingExpiration = (int?)slidingExpiration?.TotalMilliseconds;
        metadataEntry.AbsoluteExpiration = absoluteExpiration?.ToUnixTimeMilliseconds();
        metadataEntry.LastAccess = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        await _context.SaveChangesAsync(cancellationToken);
        metadata = await GetMetadataAsync(key, shard, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return metadata;
    }

    public async Task Touch(string key, int shard, CancellationToken cancellationToken)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var metadata = await _context.StoredObjects.Where(i => i.Shard == shard)
            .Where(i => i.Key == key)
            .Select(i => i.Metadata)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (metadata is null)
        {
            return;
        }

        metadata.LastAccess = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SyncRecordAsync(
        CacheEntry remoteRecord,
        Func<CacheEntry, UpdateType, Task> needsUpdateCallback,
        CancellationToken cancellationToken)
    {
        var record = await GetMetadataAsync(remoteRecord.Key, remoteRecord.Shard, cancellationToken)
            .ConfigureAwait(false);
        if (record is not null)
        {
            if (record.Modified == remoteRecord.Modified && remoteRecord.MetadataId == record.MetadataId)
            {
                return;
            }

            if (record.Modified > remoteRecord.Modified)
            {
                await needsUpdateCallback(remoteRecord, UpdateType.Remote).ConfigureAwait(false);
                return;
            }
        }

        await needsUpdateCallback(remoteRecord, UpdateType.Local).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<CacheEntry> SampleShardAsync(
        int shard,
        double percentage = 0.01,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        percentage = Math.Clamp(percentage, 0, 1);
        var count = await _context.StoredObjects.CountAsync(i => i.Shard == shard, cancellationToken)
            .ConfigureAwait(false);
        var takeCount = (int)(count * percentage);
        if (takeCount == 0)
        {
            takeCount = 1;
        }

        await foreach (var item in _context.StoredObjects.Where(i => i.Shard == shard)
                           .Include(i => i.Metadata)
                           .OrderBy(i => EF.Functions.Random())
                           .Take(takeCount)
                           .AsAsyncEnumerable()
                           .WithCancellation(cancellationToken)
                           .ConfigureAwait(false)
                      )
        {
            yield return _dataMapper.MapCacheEntryData(item)!;
        }
    }

    public async IAsyncEnumerable<CacheEntry> StreamShardAsync(int shard,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in _context.StoredObjects
                           .Include(i => i.Metadata)
                           .Where(i => i.Shard == shard)
                           .AsAsyncEnumerable()
                           .WithCancellation(cancellationToken))
        {
            yield return _dataMapper.MapCacheEntryData(item)!;
        }
    }

    public async Task<int> ReapShardAsync(int shard, CancellationToken cancellationToken)
    {
        return await ReapAsync(() => EnumerateStoredObjects(shard), cancellationToken);
    }

    public async Task<int> ReapStaleMetadataAsync(CancellationToken cancellationToken)
    {
        var reapedCount = 0;
        while (true)
        {
            var currentCount = 0;
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await foreach (var item in _context.ObjectMetadata
                               .Where(i => !_context.StoredObjects.Any(x => x.MetadataId == i.Id)).Take(250)
                               .AsAsyncEnumerable()
                               .WithCancellation(cancellationToken)
                               .ConfigureAwait(false))
            {
                currentCount++;
                reapedCount++;
                _context.Entry(item).State = EntityState.Deleted;
            }

            if (currentCount == 0) break;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return reapedCount;
    }

    public async Task ReapExpiredRecordsAsync(CancellationToken cancellationToken)
    {
        var startTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        while (true)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);
            await foreach (var item in _context.StoredObjects
                               .Where(i => i.Metadata.AbsoluteExpiration != null ||
                                           i.Metadata.SlidingExpiration != null)
                               .Where(i => i.Modified < (startTimestamp - 120000))
                               .Where(i => i.Metadata.LastAccess < (startTimestamp - 120000))
                               .Where(i => i.Metadata.Modified < (startTimestamp - 120000))
                               .Where(i =>
                                   (i.Metadata.AbsoluteExpiration != null &&
                                    i.Metadata.AbsoluteExpiration < startTimestamp) ||
                                   (i.Metadata.SlidingExpiration != null &&
                                    (i.Metadata.LastAccess + i.Metadata.SlidingExpiration.Value) < startTimestamp))
                               .OrderBy(i => i.Metadata.LastAccess)
                               .Take(250)
                               .AsAsyncEnumerable()
                               .WithCancellation(cancellationToken)
                               .ConfigureAwait(false)
                          )
            {
                // Reaper will cleanup the metadata.
                _context.Entry(item).State = EntityState.Deleted;
            }

            var count = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            if (count == 0) break;
        }
    }

    private async Task<int> ReapAsync(Func<IEnumerable<FileInfo>> files, CancellationToken cancellationToken)
    {
        var currentCount = 0;
        var earliestFileTime = DateTime.Now.AddMinutes(-10);
        foreach (var fileChunk in files()
                     .Chunk(100)
                )
        {
            var chunk = fileChunk.Where(i => i.LastAccessTime < earliestFileTime)
                .Where(i => i.LastWriteTime < earliestFileTime).ToArray();
            foreach (var fileNeedingTimestampUpdate in fileChunk.Except(chunk))
            {
                fileNeedingTimestampUpdate.LastAccessTime = DateTime.Now;
            }

            var dataToProcess = new List<(Guid id, int shard, FileInfo fileInfo)>();
            foreach (var file in chunk)
            {
                (Guid id, int shard, FileInfo fileInfo) info;
                try
                {
                    info = (GetId(file), GetShard(file), file);
                    dataToProcess.Add(info);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse ID from path {fileName}", file.FullName);
                }
            }

            var toCheck = dataToProcess.Select(i => i.id).ToHashSet();

            if (toCheck.Count == 0) continue;

            var existingRecords =
                await _context.ObjectMetadata.Where(i => toCheck.Contains(i.Id)).Select(i => i.Id)
                    .ToArrayAsync(cancellationToken);

            var recordsToRemove = toCheck.Where(i => existingRecords.All(x => x != i)).ToHashSet();
            if (recordsToRemove.Count == 0) break;


            foreach (var info in dataToProcess)
            {
                if (!recordsToRemove.Contains(info.id))
                {
                    info.fileInfo.LastAccessTime = DateTime.Now;
                    continue;
                }

                currentCount++;
                // If we fail to delete the file, it's ok. We'll catch it with the next reap.
                try
                {
                    _fileManager.Delete(info.id, info.shard);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete object {id} on shard {shard}", info.id, info.shard);
                }
            }
        }

        return currentCount;
    }

    public async Task<int> ReapAsync(CancellationToken cancellationToken)
    {
        return await ReapAsync(EnumerateStoredObjects, cancellationToken).ConfigureAwait(false);
    }

    public async Task CreateOrReplaceEntryAsync(string key,
        int shard,
        DateTimeOffset? absoluteExpiration,
        TimeSpan? slidingExpiration,
        Func<Stream, Task> callbackAsync,
        CancellationToken cancellationToken)
    {
        var targetId = Guid.NewGuid();
        await using var dbTransaction =
            await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var storedObject = await _context.StoredObjects.FindAsync(new object?[] { key }, cancellationToken);
        await using (var fileStream = _fileManager.Open(targetId, shard, false))
        {
            await callbackAsync(fileStream).ConfigureAwait(false);
            await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        var metadataEntry = new StoredObjectMetadata
        {
            Id = targetId,
            AbsoluteExpiration = absoluteExpiration?.ToUnixTimeMilliseconds(),
            SlidingExpiration = slidingExpiration?.TotalMilliseconds
        };

        _context.ObjectMetadata.Add(metadataEntry);
        var originalId = storedObject?.MetadataId;

        if (storedObject is not null)
        {
            storedObject.MetadataId = metadataEntry.Id;
            storedObject.Modified = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
        else
        {
            storedObject = new StoredObject { Key = key, MetadataId = metadataEntry.Id, Shard = shard };
            _context.Add(storedObject);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await dbTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        if (originalId != null)
        {
            await TryReapRecord(originalId.Value, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task PurgeShard(int shard, CancellationToken cancellationToken)
    {
        while (true)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);
            var count = await _context.StoredObjects.Where(i => i.Shard == shard).Take(200)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            if (count == 0) break;
        }

        try
        {
            var shardDirectory = GetShardDirectoryInfo(shard);
            if (!shardDirectory.Exists) return;
            shardDirectory.Delete(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to purge files for shard {shard}, these will be collected during background reaping",
                shard);
        }
    }

    private int GetShard(FileInfo fileInfo)
    {
        return int.Parse(fileInfo.Directory!.Parent!.Parent!.Parent!.Name);
    }

    private Guid GetId(FileInfo fileInfo)
    {
        return Guid.Parse(fileInfo.Directory!.Name);
    }

    private DirectoryInfo GetShardDirectoryInfo(int shard)
    {
        return new DirectoryInfo(Path.GetFullPath(Path.Combine(_fileManager.BasePath, shard.ToString())));
    }

    private IEnumerable<FileInfo> EnumerateStoredObjects(int shard)
    {
        var directoryInfo = GetShardDirectoryInfo(shard);
        if (!directoryInfo.Exists) return Enumerable.Empty<FileInfo>();

        return directoryInfo.EnumerateFiles(_fileManager.FileName,
            new EnumerationOptions
            {
                MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = true, MaxRecursionDepth = 5
            });
    }

    private IEnumerable<FileInfo> EnumerateStoredObjects()
    {
        var directoryInfo = new DirectoryInfo(Path.GetFullPath(_fileManager.BasePath));
        return directoryInfo.EnumerateFiles(_fileManager.FileName,
            new EnumerationOptions
            {
                MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = true, MaxRecursionDepth = 6
            });
    }

    private async Task TryReapRecord(Guid id, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        if (await _context.StoredObjects.AnyAsync(i => i.MetadataId == id, cancellationToken)
                .ConfigureAwait(false))
        {
            return;
        }

        await _context.ObjectMetadata
            .Where(i => i.Id == id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
}
