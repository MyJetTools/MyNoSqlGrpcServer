using MyNoSqlGrpc.Engine.ServerSyncEvents;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Engine.ServerSessions
{
    public static class ReaderDataToSyncUtils
    {

        public static UpdatesGrpcResponse HandleDataToSync(this MyNoSqlReaderSession session, ISyncChangeEvent dataToSync, int maxPayloadSize)
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

                result.DownloadRows = session.RowsToSync.AwaitPayload(syncRowEvent.DbRows);
                return result;
            }

            if (dataToSync is DeleteDbRowEvent deleteDbRowEvent)
            {
                result.TableName = deleteDbRowEvent.TableName;
                result.DeleteRows = 
                    new DeleteRowGrpcContract
                    {
                        PartitionKey = deleteDbRowEvent.PartitionKey,
                        RowKeys = deleteDbRowEvent.RowKeys
                    };
                return result;
            }
            
            return result;
        }
        
    }
}