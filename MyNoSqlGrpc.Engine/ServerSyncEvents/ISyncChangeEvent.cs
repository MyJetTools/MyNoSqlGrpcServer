using System.Collections.Generic;
using System.Linq;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Engine.ServerSyncEvents
{
    
    
    public interface ISyncChangeEvent{
    
    }

    public interface ISyncTableEvent : ISyncChangeEvent
    {
        string TableName { get; }
    }

    public interface ISyncTablePartitionEvent : ISyncChangeEvent
    {
        string TableName { get; }
        string PartitionKey { get; }
    }


    public static class SyncTablePartitionEventExtensions
    {
        public static bool ForTheSameCategory(this ISyncTablePartitionEvent src, ISyncTablePartitionEvent dest)
        {
            return src.TableName == dest.TableName && src.PartitionKey == dest.PartitionKey;
        }
    }

    public struct PingSyncEvent : ISyncChangeEvent
    {
    }
    
    public struct ClearTableSyncEvent : ISyncTableEvent
    {
        public ClearTableSyncEvent(string tableName)
        {
            TableName = tableName;
        }
        public string TableName { get; }
    }
    
    public struct SyncPartitionEvent : ISyncTablePartitionEvent
    {
        public SyncPartitionEvent(string tableName, string partitionKey)
        {
            TableName = tableName;
            PartitionKey = partitionKey;
        }
        public string TableName { get; }
        public string PartitionKey { get; }
    }

    public struct SyncRowEvent : ISyncTablePartitionEvent
    {
        public SyncRowEvent(string tableName, DbRowGrpcModel dbRow)
        {
            TableName = tableName;
            PartitionKey = dbRow.PartitionKey;
            DbRows = new List<DbRowGrpcModel> {dbRow};
            PayLoadSize = dbRow.Content.Length;
        } 
        
        public SyncRowEvent(string tableName, string partitionKey, IReadOnlyList<DbRowGrpcModel> dbRows)
        {
            TableName = tableName;
            PartitionKey = partitionKey;
            DbRows = new List<DbRowGrpcModel>();
            DbRows.AddRange(dbRows);
            PayLoadSize = dbRows.Sum(dbRow => dbRow.Content.Length);
        } 
        
        public string TableName { get; }
        
        public string PartitionKey { get; }
        
        public List<DbRowGrpcModel> DbRows { get; }
        
        public int PayLoadSize { get; private set; }

        public bool TryMerge(SyncRowEvent otherEvent)
        {
            if (!this.ForTheSameCategory(otherEvent))
                return false;

            DbRows.AddRange(otherEvent.DbRows);
            PayLoadSize += otherEvent.PayLoadSize;
            return true;
        }
    }

    //ToDo - Сделать групировку евентов
    public struct DeleteDbRowEvent : ISyncTablePartitionEvent
    {
        public DeleteDbRowEvent(string tableName, string partitionKey, string rowKey)
        {
            TableName = tableName;
            PartitionKey = partitionKey;
            RowKeys = new List<string> { rowKey};
        }
        public string TableName { get; }
        
        public string PartitionKey { get; }

        public List<string> RowKeys { get; }

        public bool TryMerge(DeleteDbRowEvent otherEvent)
        {
            if (!this.ForTheSameCategory(otherEvent))
                return false;
            
            RowKeys.AddRange(otherEvent.RowKeys);
            return true;
        }
    }
}