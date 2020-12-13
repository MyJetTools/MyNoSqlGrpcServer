using System;
using System.Collections.Generic;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Writer
{
    public struct GetOperationBuilder<T>
    {
        private readonly IMyNoSqlGrpcServerWriter _myNoSqlGrpcServer;
        private readonly Func<ReadOnlyMemory<byte>, T> _deserializer;
        private readonly string _tableName;
        private int _limitRecords;
        private int _skipRecords;
        private string _partitionKey;

        public GetOperationBuilder(IMyNoSqlGrpcServerWriter myNoSqlGrpcServer, Func<ReadOnlyMemory<byte>, T> deserializer, 
            string tableName)
        {
            _myNoSqlGrpcServer = myNoSqlGrpcServer;
            _deserializer = deserializer;
            _tableName = tableName;
            _limitRecords = 0;
            _skipRecords = 0;
            _partitionKey = null;
        }


        public GetOperationBuilder<T> SkipRecords(int skipRecords)
        {
            _skipRecords = skipRecords;
            return this;
        }
        
        public GetOperationBuilder<T> LimitRecords(int limitRecords)
        {
            _limitRecords = limitRecords;
            return this;
        }
        
        public GetOperationBuilder<T> WithPartitionKey(string partitionKey)
        {
            _partitionKey = partitionKey;
            return this;
        }

        public async IAsyncEnumerable<T> ExecuteAsync()
        {
            var result = _myNoSqlGrpcServer.GetAsync(new GetDbRowsGrpcRequest
            {
                TableName = _tableName,
                PartitionKey = _partitionKey,
                Limit = _limitRecords,
                Skip = _skipRecords,
            });

            await foreach (var itm in result)
            {
                yield return  _deserializer(itm.Content);
            }
        }
    }
}