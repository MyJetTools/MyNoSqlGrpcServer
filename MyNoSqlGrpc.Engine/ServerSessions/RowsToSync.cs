using System;
using System.Collections.Generic;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Engine.ServerSessions
{
    /// <summary>
    /// If Payload is to big to deliver - we keep it here and try Deliver with GRPC Stream
    /// </summary>
    public class RowsToSync
    {
        private readonly Dictionary<string, IReadOnlyList<DbRowGrpcModel>> _rowsToSync = new ();
        
        public IReadOnlyList<DbRowGrpcModel> Get(string snapshotId)
        {
            lock (_rowsToSync)
            {
                if (_rowsToSync.Remove(snapshotId, out var result))
                    return result;

                throw new Exception("Can not find snapshot: " + snapshotId);
            }
        }
        
        public string AwaitPayload(IReadOnlyList<DbRowGrpcModel> dbRows)
        {
            var result = Guid.NewGuid().ToString();
            lock (_rowsToSync)
            {
                while (_rowsToSync.ContainsKey(result))
                    result = Guid.NewGuid().ToString();

                _rowsToSync.Add(result, dbRows);
            }

            return result;
        }
    }
}