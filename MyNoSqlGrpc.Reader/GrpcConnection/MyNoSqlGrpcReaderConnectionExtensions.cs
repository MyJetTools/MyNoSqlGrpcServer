using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AsyncAwaitUtils;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Reader.GrpcConnection
{
    public static class MyNoSqlGrpcReaderConnectionExtensions
    {
        public static IAsyncEnumerable<DbRowGrpcModel> SubscribeAsync(this MyNoSqlGrpcReaderConnection connection, 
            string tableName, string partitionKey)
        {
            
            
            var grpcRequest = new SubscribeGrpcRequest
            {
                ConnectionId = connection.ConnectionId,
                TableName = tableName,
                PartitionKey = partitionKey
            };

            return connection.MyNoSqlGrpcServerReader.SubscribeAsync(grpcRequest);
        }
        
        public static IAsyncEnumerable<DbRowGrpcModel> DownloadPartitionAsync(this MyNoSqlGrpcReaderConnection connection,
            string tableName, string partitionKey)
        {
            var grpcRequest = new DownloadPartitionGrpcRequest
            {
                ConnectionId = connection.ConnectionId,
                PartitionKey = partitionKey,
                TableName = tableName
            };
            return connection.MyNoSqlGrpcServerReader.DownloadPartitionAsync(grpcRequest, connection.CancellationTokenSource.Token);
        }
        
        internal static async ValueTask DownloadPartitionsIfSessionIsNew(this MyNoSqlGrpcReaderConnection connection)
        {
            foreach (var (table, partitions) in connection.GetPartitionsToDownload())
            {
                foreach (var partitionKey in partitions)
                {
                    var dBrows = await connection.DownloadPartitionAsync(table, partitionKey).ToListAsync();
                    connection.InvokeUpdateEvent(new ResetPartitionUpdateCommand(table, partitionKey, dBrows));
                }
            }
        }
        
        internal static async ValueTask<IGrpcConnectionUpdateCommand> HandleNewDataAsync(
            this MyNoSqlGrpcReaderConnection connection, UpdatesGrpcResponse update)
        {
            connection.LastReceiveTime = DateTime.UtcNow;

            if (update.TableName == null)
                return SkipItUpdateResult.Instance;

            if (update.ClearTable)
                return new ClearTableUpdateResult(update.TableName);
            
            if (update.DeleteRows != null)
            {
                return new DeleteRowsCommand(update.TableName, update.DeleteRows.PartitionKey,
                    update.DeleteRows.RowKeys);
            }

            if (update.ResetPartitionKey != null)
            {
                var dbRows = await connection.DownloadPartitionAsync(update.TableName, update.ResetPartitionKey).ToListAsync();
                return new ResetPartitionUpdateCommand(update.TableName, update.ResetPartitionKey, dbRows);
            }



            if (update.DownloadRows != null)
            {
                var dbRows = await connection.MyNoSqlGrpcServerReader.SyncRowsAsync(new SyncRowsGrpcRequest
                {
                    ConnectionId = connection.ConnectionId,
                    SnapshotId = update.DownloadRows,
                    TableName = update.TableName
                }).ToListAsync();
                return new UpdateRowsCommand(update.TableName, dbRows);
            }
                

            if (update.DbRows != null)
                return new UpdateRowsCommand(update.TableName, update.DbRows);

            throw new Exception("Unknown payload data. " + update.PrintPayload());
        }
    }
}