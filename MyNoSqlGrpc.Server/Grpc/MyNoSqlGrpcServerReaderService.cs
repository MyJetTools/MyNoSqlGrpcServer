using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyNoSqlGrpc.Server.Services;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Server.Grpc
{
    public class MyNoSqlGrpcServerReaderService : IMyNoSqlGrpcServerReader
    {
        private static Exception GetSessionExpiredException(string connection)
        {
            return new ($"Session {connection} is expired");
        }
        
        public static int MaxPayloadSize { get; set; }
        
        public ValueTask<GreetingGrpcResponse> GreetingAsync(GreetingGrpcRequest request, CancellationToken token = default)
        {
            var result = new GreetingGrpcResponse
            {
                WeContinueSession =
                    ServiceLocator.MyNoSqlReaderSessionsList.RegisterNewSession(request.ConnectionId, request.AppName)
            };

            return new ValueTask<GreetingGrpcResponse>(result);
        }

        public ValueTask SubscribeAsync(SubscribeGrpcRequest request, CancellationToken token = default)
        {
            var session = ServiceLocator.MyNoSqlReaderSessionsList.TryGetSession(request.ConnectionId);
            if (session == null)
                throw GetSessionExpiredException(request.ConnectionId);

            var table = ServiceLocator.DbTablesList.TryGetTable(request.TableName);
            if (table == null)
                throw new Exception($"Table {request.TableName} is not found");
            
            session.SubscribeToPartition(request.TableName, request.PartitionKey);

            return new ValueTask();
        }
        
        private static async Task<UpdatesGrpcResponse> AwaitRequest(MyNoSqlReaderSession session)
        {
            var result = await session.IssueAwaitingUpdateEvent();
            return session.HandleDataToSync(result, MaxPayloadSize);
        }

        public ValueTask<UpdatesGrpcResponse> GetUpdatesAsync(GetUpdatesGrpcRequest request, CancellationToken token = default)
        {
            var session = ServiceLocator.MyNoSqlReaderSessionsList.TryGetSession(request.ConnectionId);

            if (session == null)
                throw GetSessionExpiredException(request.ConnectionId);

            var dataToSync = session.GetDataToSync();

            return dataToSync == null 
                ? new ValueTask<UpdatesGrpcResponse>(AwaitRequest(session)) 
                : new ValueTask<UpdatesGrpcResponse>(session.HandleDataToSync(dataToSync, MaxPayloadSize));
        }

        public IAsyncEnumerable<DbRowGrpcModel> DownloadPartitionAsync(DownloadPartitionGrpcRequest request, CancellationToken token = default)
        {
            var session = ServiceLocator.MyNoSqlReaderSessionsList.TryGetSession(request.ConnectionId);
            if (session == null)
                throw GetSessionExpiredException(request.ConnectionId);
            
            var table = ServiceLocator.DbTablesList.TryGetTable(request.TableName);

            if (table == null)
                throw new Exception($"Table {request.TableName} is not found");
            
            var rows = table.LockWithWriteAccess<IReadOnlyList<DbRowGrpcModel>>(writeAccess =>
            {
                session.SubscribeToPartition(table.Id, request.PartitionKey);

                var partition = writeAccess.TryGetPartition(request.PartitionKey);

                if (partition == null)
                    return Array.Empty<DbRowGrpcModel>();

                return partition.Get().ToList();
            });

            return new AsyncEnumerableResult<DbRowGrpcModel>(rows);
        }

        public IAsyncEnumerable<DbRowGrpcModel> SyncRowsAsync(SyncRowsGrpcRequest request, CancellationToken token = default)
        {
            var session = ServiceLocator.MyNoSqlReaderSessionsList.TryGetSession(request.ConnectionId);
            if (session == null)
                throw GetSessionExpiredException(request.ConnectionId);

            var dbRows = session.GetRowsToSync(request.SnapshotId);

            return new AsyncEnumerableResult<DbRowGrpcModel>(dbRows);
        }
    }
}