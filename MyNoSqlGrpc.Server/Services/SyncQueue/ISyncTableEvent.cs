using System.Linq;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Server.Services.SyncQueue
{
    public interface ISyncTableEvent
    {
        
    }

    public struct PingSyncEvent : ISyncTableEvent
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
    
    public struct SyncPartitionEvent : ISyncTableEvent
    {
        public SyncPartitionEvent(string tableName, string partitionKey)
        {
            TableName = tableName;
            PartitionKey = partitionKey;
        }
        public string TableName { get; }
        public string PartitionKey { get; }
    }

    public struct SyncRowEvent : ISyncTableEvent
    {
        public SyncRowEvent(string tableName, DbRowGrpcModel[] dbRows)
        {
            TableName = tableName;
            DbRows = dbRows;
            PayLoadSize = DbRows.Sum(itm => itm.Content.Length);
        } 
        
        public string TableName { get; }
        
        public DbRowGrpcModel[] DbRows { get; }
        
        public int PayLoadSize { get; }
    }
}