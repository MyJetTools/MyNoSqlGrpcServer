using System;
using System.Collections.Generic;
using System.Threading;

namespace MyNoSqlGrpc.Reader.Cache
{

    public interface ITableReadAccess<T>
    {
        public ReaderPartition<T> TryGetPartition(string partitionKey);
    }
    
    public interface ITableWriteAccess<T>
    {
        public ReaderPartition<T> GetOrCreatePartition(string partitionKey);
        
        public ReaderPartition<T> ClearPartition(string partitionKey);

        public Dictionary<string, ReaderPartition<T>> Clear();
    }


    public class ReaderTable<T> : ITableReadAccess<T>, ITableWriteAccess<T>
    {
        private Dictionary<string, ReaderPartition<T>> _partitions =
            new();

        private readonly ReaderWriterLockSlim _lockSlim = new();

        ReaderPartition<T> ITableReadAccess<T>.TryGetPartition(string partitionKey)
        {
            return _partitions.TryGetValue(partitionKey, out var partition) 
                ? partition 
                : null;
        }

        ReaderPartition<T> ITableWriteAccess<T>.GetOrCreatePartition(string partitionKey)
        {
            if (_partitions.TryGetValue(partitionKey, out var partition))
                return partition;

            partition = new ReaderPartition<T>();
            _partitions.Add(partitionKey, partition);
            return partition;
        }

        ReaderPartition<T> ITableWriteAccess<T>.ClearPartition(string partitionKey)
        {
            if (!_partitions.TryGetValue(partitionKey, out var partition))
                return null;

            _partitions.Remove(partitionKey);
            return partition;
        }

        Dictionary<string, ReaderPartition<T>> ITableWriteAccess<T>.Clear()
        {
            var result = _partitions;
            _partitions = new Dictionary<string, ReaderPartition<T>>();
            return result;
        }

        public TOut LockWithReadAccess<TOut>(Func<ITableReadAccess<T>, TOut> readAccess)
        {
            _lockSlim.EnterReadLock();
            try
            {
                return readAccess(this);
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public void LockWithWriteAccess(Action<ITableWriteAccess<T>> writeAccess)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                writeAccess(this);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

    }

}