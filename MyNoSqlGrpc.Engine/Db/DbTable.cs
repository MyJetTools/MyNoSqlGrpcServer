using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Engine.Db
{

    public interface IDbTableReadAccess
    {
        DbPartition TryGetPartition(string partitionKey);
    }
    
    public interface IDbTableWriteAccess
    {
        DbPartition GetOrCreatePartition(string partitionKey);

        DbPartition TryGetPartition(string partitionKey);

        int PartitionsCount();

        IEnumerable<DbPartition> GetPartitions();

        void RemovePartition(string partitionKey);
    }
    
    public class DbTable : IDbTableReadAccess, IDbTableWriteAccess
    {
        
        public string Id { get; }

        public DbTable(string id)
        {
            Id = id;
        }

        private readonly ReaderWriterLockSlim _lockSlim = new ();

        private readonly SortedDictionary<string, DbPartition> _partitions = new ();


        public void LockWithReadAccess(Action<IDbTableReadAccess> readAccess)
        {
            _lockSlim.EnterReadLock();
            try
            {
                readAccess(this);
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }
        
        public T LockWithReadAccess<T>(Func<IDbTableReadAccess, T> readAccess)
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
        
        public IEnumerable<DbRowGrpcModel> Get(string partitionKey)
        {
            _lockSlim.EnterReadLock();
            try
            {
                if (partitionKey == null)
                {
                    foreach (var dbRow in _partitions.Values.SelectMany(partition => partition.Get()))
                    {
                        yield return dbRow;
                    }
                }
                else
                if (_partitions.TryGetValue(partitionKey, out var result))
                {
                    foreach (var dbRow in result.Get())
                    {
                        yield return dbRow;
                    }
                }
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }
        
        public DbRowGrpcModel Get(string partitionKey, string rowKey)
        {
            _lockSlim.EnterReadLock();
            try
            {
                if (_partitions.TryGetValue(partitionKey, out var result))
                    return result.TryGet(rowKey);
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }

            return null;
        }


        public void LockWithWriteAccess(Action<IDbTableWriteAccess> writeAccess)
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
        
        public T LockWithWriteAccess<T>(Func<IDbTableWriteAccess, T> writeAccess)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                return writeAccess(this);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }


        public DbPartition GetOrCreatePartition(string partitionKey)
        {
            if (_partitions.TryGetValue(partitionKey, out var result))
                return result;

            var resultPartition = new DbPartition(partitionKey);
            
            _partitions.Add(partitionKey, resultPartition);
            return resultPartition;
        }

        DbPartition IDbTableReadAccess.TryGetPartition(string partitionKey)
        {
            return _partitions.TryGetValue(partitionKey, out var result) 
                ? result 
                : null;
        }

        int IDbTableWriteAccess.PartitionsCount()
        {
            return _partitions.Count;
        }

        public IEnumerable<DbPartition> GetPartitions()
        {
            return _partitions.Values;
        }

        public void RemovePartition(string partitionKey)
        {
            if (_partitions.ContainsKey(partitionKey))
                _partitions.Remove(partitionKey);
        }

        DbPartition IDbTableWriteAccess.TryGetPartition(string partitionKey)
        {
            return _partitions.TryGetValue(partitionKey, out var result) 
                ? result 
                : null;
        }

        public int GetAmount(string partitionKey)
        {
            _lockSlim.EnterReadLock();
            try
            {

                if (_partitions.TryGetValue(partitionKey, out var dbPartition))
                {
                    return dbPartition.Count;
                }

                return 0;

            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public int GetPartitionsCount()
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _partitions.Count;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public bool WeHavePartitionsToGc(int maxPartitions)
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _partitions.Count > maxPartitions;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }
        
    }
}