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
    }
    
    public class DbTable : IDbTableReadAccess, IDbTableWriteAccess
    {
        
        public string Id { get; }

        public DbTable(string id)
        {
            Id = id;
        }

        private readonly ReaderWriterLockSlim _lockSlim = new ();

        private readonly Dictionary<string, DbPartition> _partitions = new ();


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
        
        public IEnumerable<DbRowGrpcModel> Get(string partitionKey)
        {
            _lockSlim.EnterReadLock();
            try
            {
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

        public void Gc(string partitionKey, int maxAmount)
        {
            _lockSlim.EnterWriteLock();

            try
            {
                if (_partitions.TryGetValue(partitionKey, out var dbPartition))
                    dbPartition.Gc(maxAmount);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }


        private bool NeedGcPartitions(int maxPartitions)
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
        
        public void Gc(int maxAmount)
        {
            
            if (!NeedGcPartitions(maxAmount))
                return;
            
            _lockSlim.EnterWriteLock();
            try
            {
                
                if (_partitions.Count > maxAmount)
                    return;
                
                var itemsByLastAccess = _partitions.Values.OrderBy(itm => itm.LastAccessTime).ToList();

                var i = 0;
                while (_partitions.Count> maxAmount)
                {
                    _partitions.Remove(itemsByLastAccess[i].PartitionKey);
                    i++;
                }
                
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }
    }
}