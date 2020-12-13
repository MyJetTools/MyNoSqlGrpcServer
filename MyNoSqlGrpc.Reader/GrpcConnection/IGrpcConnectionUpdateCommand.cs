using System.Collections.Generic;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Reader.GrpcConnection
{
    public interface IGrpcConnectionUpdateCommand
    {
        public string TableName { get; }
    }

    public struct SkipItUpdateResult: IGrpcConnectionUpdateCommand
    {
        public string TableName => null;
        public static SkipItUpdateResult Instance = new ();
    }
    
    public struct ClearTableUpdateResult: IGrpcConnectionUpdateCommand
    {
        public ClearTableUpdateResult(string tableName)
        {
            TableName = tableName;
        }
        public string TableName { get; }
    }

    public struct ResetPartitionUpdateCommand : IGrpcConnectionUpdateCommand
    {

        public ResetPartitionUpdateCommand(string tableName, string partitionKey, IReadOnlyList<DbRowGrpcModel> dbRows)
        {
            TableName = tableName;
            PartitionKey = partitionKey;
            DbRows = dbRows;
        }
        
        public string TableName { get; }
        
        public string PartitionKey { get; }
        
        public IReadOnlyList<DbRowGrpcModel> DbRows { get; }
   
    }

    public struct UpdateRowsCommand : IGrpcConnectionUpdateCommand
    {
        public UpdateRowsCommand(string tableId, IReadOnlyList<DbRowGrpcModel> rowsToUpdate)
        {
            RowsToUpdate = rowsToUpdate;
            TableName = tableId;
        }
        
        public string TableName { get; }
        public IReadOnlyList<DbRowGrpcModel> RowsToUpdate { get; }
    }

}