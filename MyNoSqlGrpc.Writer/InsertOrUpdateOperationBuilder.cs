using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Writer
{
    public struct InsertOrReplaceOperationBuilder
    {
        private readonly IMyNoSqlGrpcServerWriter _myNoSqlGrpcServer;
        private readonly string _tableName;
        private readonly string _partitionKey;
        private readonly string _rowKey;
        private readonly byte[] _content;
        private DateTime? _expirationTime;
        
        public InsertOrReplaceOperationBuilder(IMyNoSqlGrpcServerWriter myNoSqlGrpcServer,
            string tableName, string partitionKey, string rowKey, byte[] content)
        {
            _myNoSqlGrpcServer = myNoSqlGrpcServer;
            _tableName = tableName;
            _partitionKey = partitionKey;
            _rowKey = rowKey;
            _content = content;
            _expirationTime = null;
        }

        public InsertOrReplaceOperationBuilder WithExpirationTime(DateTime? expirationTime)
        {
            _expirationTime = expirationTime;
            return this;
        }


        public async ValueTask<GrpcResultStatus> ExecuteAsync()
        {
            var result = await _myNoSqlGrpcServer.InsertOrReplaceAsync(new RowWithTableNameGrpcRequest
            {
                TableName = _tableName,
                DbRow = new DbRowGrpcModel
                {
                    PartitionKey = _partitionKey,
                    RowKey = _rowKey,
                    Content = _content,
                    Expires = _expirationTime,
                }
            });

            return result.Status;
        }
    }

}