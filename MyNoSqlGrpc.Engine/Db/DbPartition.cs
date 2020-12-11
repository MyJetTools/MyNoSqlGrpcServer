using System;
using System.Collections.Generic;
using System.Linq;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Engine.Db
{
    public class DbPartition
    {
        private readonly Dictionary<string, DbRowGrpcModel> _rows = new ();
        public string PartitionKey { get; }
        public int Count => _rows.Count;
        
        public DateTime LastExpirationAttempt { get; internal set; }
        
        public DateTime LastAccessTime { get; private set; }

        public DbPartition(string partitionKey)
        {
            LastExpirationAttempt = DateTime.UtcNow;
            PartitionKey = partitionKey;
        }


        public IEnumerable<DbRowGrpcModel> Get()
        {
            LastAccessTime= DateTime.UtcNow;
            return _rows.Values;
        }

        public DbRowGrpcModel TryGet(string rowKey)
        {
            LastAccessTime= DateTime.UtcNow;
            if (_rows.TryGetValue(rowKey, out var dbRow))
            {
                dbRow.LastAccessTime = DateTime.UtcNow;
                return dbRow;
            }

            return null;
        }

        public DbRowGrpcModel TryDelete(string rowKey)
        {
            if (!_rows.TryGetValue(rowKey, out var result)) 
                return null;
            
            _rows.Remove(rowKey);
            return result;

        }

        public bool Insert(DbRowGrpcModel dbRow)
        {
            LastAccessTime= DateTime.UtcNow;
            dbRow.TimeStamp = LastAccessTime;
            dbRow.LastAccessTime = LastAccessTime;
            
            if (_rows.ContainsKey(dbRow.RowKey))
                return false;
            
            _rows.Add(dbRow.RowKey, dbRow);
            return true;
        }

        public void InsertOrReplace(DbRowGrpcModel dbRow)
        {
            LastAccessTime = DateTime.UtcNow;
            dbRow.TimeStamp = LastAccessTime;
            dbRow.LastAccessTime = LastAccessTime;
            if (_rows.ContainsKey(dbRow.RowKey))
                _rows[dbRow.RowKey] = dbRow;
            else
                _rows.Add(dbRow.RowKey, dbRow);
        }

        public void Gc(int maxAmount)
        {
            if (Count <= maxAmount)
                return;

            var itemsByLastAccess = _rows.Values.OrderBy(itm => itm.LastAccessTime).ToList();

            var i = 0;
            while (Count> maxAmount)
            {
                _rows.Remove(itemsByLastAccess[i].RowKey);
                i++;
            }
        }
    }
}