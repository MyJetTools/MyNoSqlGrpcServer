using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyNoSqlGrpc.Engine.Db;
using MyNoSqlGrpc.Engine.ServerSessions;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Engine.Api
{
    public class MyNoSqlGrpcServerReader : IMyNoSqlGrpcServerReader
    {
        private readonly MyNoSqlReaderSessionsList _sessionsList;
        private readonly DbTablesList _dbTablesList;
        private readonly IMyNoSqlGrpcEngineSettings _settings;

        public MyNoSqlGrpcServerReader(MyNoSqlReaderSessionsList sessionsList, DbTablesList dbTablesList, IMyNoSqlGrpcEngineSettings settings)
        {
            _sessionsList = sessionsList;
            _dbTablesList = dbTablesList;
            _settings = settings;
        }
        
        public ValueTask<GreetingGrpcResponse> GreetingAsync(GreetingGrpcRequest request, CancellationToken token = default)
        {
            var pingTimeout = request.PingTimeout == null ? TimeSpan.FromSeconds(3) : TimeSpan.Parse(request.PingTimeout);
            var result = new GreetingGrpcResponse
            {
                WeContinueSession =
                    _sessionsList.RegisterNewSession(request.ConnectionId, request.AppName, pingTimeout)
            };

            return new ValueTask<GreetingGrpcResponse>(result);
        }

        public IAsyncEnumerable<DbRowGrpcModel> SubscribeAsync(SubscribeGrpcRequest request, CancellationToken token = default)
        {
            var session = _sessionsList.GetSession(request.ConnectionId);

            var table = _dbTablesList.GetTable(request.TableName);
            
            var result = table.LockWithReadAccess(readAccess =>
            {
                session.SubscribeToPartition(request.TableName, request.PartitionKey);

                var partition = readAccess.TryGetPartition(request.PartitionKey);

                if (partition == null)
                    return (IReadOnlyList<DbRowGrpcModel>) Array.Empty<DbRowGrpcModel>();

                return partition.Get().ToList();
            });
            

            return new AsyncEnumerableResult<DbRowGrpcModel>(result);
        }
        
        private async Task<UpdatesGrpcResponse> AwaitRequest(MyNoSqlReaderSession session)
        {
            var result = await session.IssueAwaitingUpdateEvent();
            return session.HandleDataToSync(result, _settings.MaxPayloadSize);
        }

        public ValueTask<UpdatesGrpcResponse> GetUpdatesAsync(GetUpdatesGrpcRequest request, CancellationToken token = default)
        {
            var session = _sessionsList.GetSession(request.ConnectionId);

            var dataToSync = session.GetDataToSync();

            return dataToSync == null 
                ? new ValueTask<UpdatesGrpcResponse>(AwaitRequest(session)) 
                : new ValueTask<UpdatesGrpcResponse>(session.HandleDataToSync(dataToSync, _settings.MaxPayloadSize));
        }

        public IAsyncEnumerable<DbRowGrpcModel> DownloadPartitionAsync(DownloadPartitionGrpcRequest request, CancellationToken token = default)
        {
            var session = _sessionsList.GetSession(request.ConnectionId);

            var table = _dbTablesList.GetTable(request.TableName);
            
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
            var session = _sessionsList.GetSession(request.ConnectionId);
            
            var dbRows = session.RowsToSync.Get(request.SnapshotId);

            return new AsyncEnumerableResult<DbRowGrpcModel>(dbRows);
        }
    }
}