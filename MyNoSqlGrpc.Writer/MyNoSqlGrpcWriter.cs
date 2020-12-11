using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AsyncAwaitUtils;
using Grpc.Net.Client;
using MyNoSqlGrpcServer.GrpcContracts;
using ProtoBuf.Grpc.Client;

namespace MyNoSqlGrpc.Writer
{
    public class MyNoSqlGrpcWriter<T>
    {
        private readonly string _grpcUrl;
        private readonly string _tableName;

        private readonly IMyNoSqlGrpcServer _myNoSqlGrpcServer;

        public MyNoSqlGrpcWriter(string grpcUrl, string tableName, bool useSsl)
        {
            _grpcUrl = grpcUrl;
            _tableName = tableName;
            
            if (!useSsl)
                GrpcClientFactory.AllowUnencryptedHttp2 = true;
            
            _myNoSqlGrpcServer = GrpcChannel
                .ForAddress(grpcUrl)
                .CreateGrpcService<IMyNoSqlGrpcServer>();
        }


        private Func<T, ReadOnlyMemory<byte>> _serializer;
        private Func<ReadOnlyMemory<byte>, T> _deserializer;
        
        public MyNoSqlGrpcWriter<T> PlugSerializerDeserializer(Func<T, ReadOnlyMemory<byte>> serializer, 
            Func<ReadOnlyMemory<byte>, T> deserialized)
        {
            _serializer = serializer;
            _deserializer = deserialized;
            return this;
        }
        
        

        public async ValueTask<T> GetAsync(string partitionKey, string rowKey)
        {
            var result = await _myNoSqlGrpcServer.GetAsync(new GetDbRowsGrpcRequest
            {
                TableName = _tableName,
                PartitionKey = partitionKey,
                RowKey = rowKey,
            }).FirstOrDefaultAsync();

            return _deserializer(result.Content);
        }
        
        public async IAsyncEnumerable<T> GetAsync(string partitionKey, int take = 0, int limit = 0)
        {
            var result = _myNoSqlGrpcServer.GetAsync(new GetDbRowsGrpcRequest
            {
                TableName = _tableName,
                PartitionKey = partitionKey,
                Limit = limit,
                Take = take,
            });

            await foreach (var itm in result)
            {
                yield return  _deserializer(itm.Content);
            }

        }
        
        
    }
}