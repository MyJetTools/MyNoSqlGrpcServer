using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Reader.GrpcConnection
{
    public static class GrpcConnectionUtils
    {

        public static string PrintPayload(this UpdatesGrpcResponse update)
        {
            var result =
                $"ClearTable={update.ClearTable}; TableName={update.TableName}; SyncPartitionId={update.ResetPartitionKey}; DownloadRows={update.DownloadRows} ";

            if (update.DbRows == null)
                return result + "DbRows=null";

            return result + "DbRows.Length="+update.DbRows.Count;
        }
        
    }
}