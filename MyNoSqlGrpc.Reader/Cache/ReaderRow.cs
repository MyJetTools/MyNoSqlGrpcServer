using System;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Reader.Cache
{
    public enum RowOperationResult
    {
        Insert, Update, Delete
    }
    
    public class ReaderRow<T>
    {
        public ReaderRow(DbRowGrpcModel dbRowGrpcModel, T payLoad)
        {
            RowKey = dbRowGrpcModel.RowKey;
            TimeStamp = dbRowGrpcModel.TimeStamp;
            PayLoad = payLoad;
        }

        public ReaderRow(string rowKey, DateTime timeStamp, T payLoad)
        {
            RowKey = rowKey;
            TimeStamp = timeStamp;
            PayLoad = payLoad;
        }

        public string RowKey { get; }
        
        public DateTime TimeStamp { get; }
        
        public T PayLoad { get; }
        
        
        

    }
}