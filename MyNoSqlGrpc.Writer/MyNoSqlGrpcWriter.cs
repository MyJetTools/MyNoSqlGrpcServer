using System;
using System.Threading.Tasks;
using AsyncAwaitUtils;
using Grpc.Net.Client;
using MyNoSqlGrpcServer.GrpcContracts;
using ProtoBuf.Grpc.Client;

namespace MyNoSqlGrpc.Writer
{
    
    public class MyNoSqlGrpcWriter<T> 
    {
        public struct UpdateResult
        {
            public T Result { get; set; }
            public DateTime? UpdateExpirationTime { get; set; }
            public bool RemoveExpirationTime { get; set; }
        }

        private readonly string _tableName;

        private readonly IMyNoSqlGrpcServerWriter _myNoSqlGrpcServer;

        public MyNoSqlGrpcWriter(IMyNoSqlGrpcServerWriter myNoSqlGrpcServerWriter, string tableName)
        {
            _tableName = tableName.ToLower();
            _myNoSqlGrpcServer = myNoSqlGrpcServerWriter;
        }

        public static MyNoSqlGrpcWriter<T> Create(string grpcUrl, string tableName, bool useSsl)
        {
            if (!useSsl)
                GrpcClientFactory.AllowUnencryptedHttp2 = true;

            var result = GrpcChannel
                .ForAddress(grpcUrl)
                .CreateGrpcService<IMyNoSqlGrpcServerWriter>();

            return new MyNoSqlGrpcWriter<T>(result, tableName);
        }

        private Func<T, byte[]> _serializer;
        private Func<ReadOnlyMemory<byte>, T> _deserializer;
        
        public MyNoSqlGrpcWriter<T> PlugSerializerDeserializer(Func<T, byte[]> serializer, 
            Func<ReadOnlyMemory<byte>, T> deserializer)
        {
            _serializer = serializer;
            _deserializer = deserializer;
            return this;
        }


        private async ValueTask<DbRowGrpcModel> GetDbRowModel(string partitionKey, string rowKey)
        {
            return await _myNoSqlGrpcServer.GetAsync(new GetDbRowsGrpcRequest
            {
                TableName = _tableName,
                PartitionKey = partitionKey,
                RowKey = rowKey,
            }).FirstOrDefaultAsync();
        }

        public ValueTask CreateTableIfNotExistsAsync()
        {
            return _myNoSqlGrpcServer.CreateTableIfNotExistsAsync(new CreateTableGrpcRequest
            {
                TableName = _tableName
            });
        }
        
        
        public async ValueTask<T> GetAsync(string partitionKey, string rowKey)
        {
            var result = await GetDbRowModel(partitionKey, rowKey);
            return _deserializer(result.Content);
        }
        
        public GetOperationBuilder<T> Get()
        {
            return new GetOperationBuilder<T>(_myNoSqlGrpcServer, _deserializer, _tableName);
        }

        public InsertOperationBuilder Insert(string partitionKey, string rowKey, T data)
        {
            var content = _serializer(data);
            return new InsertOperationBuilder(_myNoSqlGrpcServer, _tableName, partitionKey, rowKey, content);
        }
        
        
        public InsertOrReplaceOperationBuilder InsertOrReplace(string partitionKey, string rowKey, T data)
        {
            var content = _serializer(data);
            return new InsertOrReplaceOperationBuilder(_myNoSqlGrpcServer, _tableName, partitionKey, rowKey, content);
        }
        
        public async ValueTask<GrpcResultStatus> UpdateAsync(string partitionKey, string rowKey, Func<T, UpdateResult> updateAction)
        {
            

            while (true)
            {
                var dbRowModel = await GetDbRowModel(partitionKey, rowKey);

                if (dbRowModel == null)
                    return GrpcResultStatus.NotFound;

                var entity = _deserializer(dbRowModel.Content);
                
                var updateResult = updateAction(entity);

                if (updateResult.Result == null)
                    return GrpcResultStatus.Ok;

                var updateGrpcEntity = new RowWithTableNameGrpcRequest
                {
                    TableName = _tableName,
                    DbRow = new DbRowGrpcModel
                    {
                        PartitionKey = partitionKey,
                        RowKey = rowKey,
                        Content = _serializer(updateResult.Result),
                    }
                };

                if (!updateResult.RemoveExpirationTime)
                    updateGrpcEntity.DbRow.Expires = updateResult.UpdateExpirationTime ?? dbRowModel.Expires;
                
                var result = await _myNoSqlGrpcServer.UpdateAsync(updateGrpcEntity);

                if (result.Status != GrpcResultStatus.RecordChanged)
                    return result.Status;

            }
            
            
            
            
        }
        
        
    }
}