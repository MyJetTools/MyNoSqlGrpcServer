using MyNoSqlGrpc.Server.Services.SyncQueue;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Server.Services
{
    public static class ReaderDataToSyncUtils
    {

        public static UpdatesGrpcResponse HandleDataToSync(this MyNoSqlReaderSession session, ISyncTableEvent dataToSync, int maxPayloadSize)
        {
            var result = new UpdatesGrpcResponse();

            if (dataToSync is ClearTableSyncEvent clearTableSyncEvent)
            {
                result.TableName = clearTableSyncEvent.TableName;
                result.ClearTable = true;
                return result;
            }
            
            if (dataToSync is SyncPartitionEvent syncPartitionEvent)
            {
                result.TableName = syncPartitionEvent.TableName;
                result.ResetPartitionKey = syncPartitionEvent.PartitionKey;
                return result;
            }

            if (dataToSync is SyncRowEvent syncRowEvent)
            {
                result.TableName = syncRowEvent.TableName;

                if (syncRowEvent.PayLoadSize < maxPayloadSize)
                {
                    result.DbRows = syncRowEvent.DbRows;
                    return result;
                }

                result.DownloadRows = session.AwaitPayload(syncRowEvent.DbRows);
                return result;
            }

            return result;
        }
        
    }
}