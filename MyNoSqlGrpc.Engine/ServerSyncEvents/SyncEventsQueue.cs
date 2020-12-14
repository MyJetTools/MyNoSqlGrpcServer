using System.Collections.Generic;
using MyNoSqlGrpc.Engine.Db;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Engine.ServerSyncEvents
{
    public class SyncEventsQueue
    {
        private readonly object _lockObject = new ();

        private readonly Queue<ISyncChangeEvent> _syncQueue = new ();
        
        public void EnqueueDbRowChange(DbTable dbTable, DbRowGrpcModel dbRowGrpcModel)
        {
            var newEvent = new SyncRowEvent(dbTable.Id, dbRowGrpcModel);
            lock (_lockObject)
            {
                _syncQueue.Enqueue(newEvent);    
            }
        }
        
        public void EnqueueDbRowDelete(DbTable dbTable, DbRowGrpcModel dbRowGrpcModel)
        {
            var newEvent = new DeleteDbRowEvent(dbTable.Id, dbRowGrpcModel.PartitionKey, dbRowGrpcModel.RowKey);
            lock (_lockObject)
                _syncQueue.Enqueue(newEvent);    
        }

        public void EnqueueSyncPartition(DbTable dbTable, DbPartition partition)
        {
            var newEvent = new SyncPartitionEvent(dbTable.Id, partition.PartitionKey);
            lock (_lockObject)
            {
                _syncQueue.Enqueue(newEvent);    
            }
        }
        
        public void EnqueueDbRowsChange(DbTable dbTable, string partitionKey, IReadOnlyList<DbRowGrpcModel> dbRowGrpcModel)
        {
            var newEvent = new SyncRowEvent(dbTable.Id, partitionKey, dbRowGrpcModel);
            lock (_lockObject)
            {
                _syncQueue.Enqueue(newEvent);    
            }
        }


        public ISyncChangeEvent TryDequeue()
        {
            lock (_lockObject)
            {
                return _syncQueue.Count == 0 
                    ? default 
                    : _syncQueue.Dequeue();
            }
        }

        
    }
}