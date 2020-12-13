using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using MyNoSqlGrpc.Server.Services.SyncQueue;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Server.Services
{
    public class MyNoSqlReaderSession
    {

        
        public string SessionId { get; }
        
        public string AppName { get; }

        public readonly Dictionary<string, Dictionary<string, string>> Subscribers = new ();

        private readonly Queue<ISyncTableEvent> _syncQueue = new ();
        
        public DateTime LastUseTime { get; set; } = DateTime.UtcNow;

        private readonly object _lockObject = new();

        public void SubscribeToPartition(string dbTable, string partitionKey)
        {
            lock (_lockObject)
            {
                if (Subscribers.TryGetValue(dbTable, out var partitions))
                {
                    partitions.TryAdd(partitionKey, partitionKey);
                    return;
                }

                partitions = new Dictionary<string, string> {{partitionKey, partitionKey}};
                Subscribers.Add(dbTable, partitions);
            }
        }

        public MyNoSqlReaderSession(string sessionId, string appName)
        {
            SessionId = sessionId;
            AppName = appName;
        }

        public void UpdateLastUseTime()
        {
            LastUseTime = DateTime.UtcNow;
        }

        public ISyncTableEvent GetDataToSync()
        {
            lock (_lockObject)
            {
                return _syncQueue.Count == 0 ? null : _syncQueue.Dequeue();
            }
        }

        public void SyncTableEvent(ISyncTableEvent syncTableEvent)
        {
            lock (_syncQueue)
                _syncQueue.Enqueue(syncTableEvent);

            Task.Run(PingConnection);
        }


        private readonly Dictionary<string, DbRowGrpcModel[]> _rowsToSync = new ();

        public DbRowGrpcModel[] GetRowsToSync(string snapshotId)
        {
            lock (_lockObject)
            {
                if (_rowsToSync.Remove(snapshotId, out var result))
                    return result;

                throw new Exception("Can not find snapshot: " + snapshotId);
            }
        }

        public string AwaitPayload(DbRowGrpcModel[] dbRows)
        {
            var result = Guid.NewGuid().ToString();
            lock (_lockObject)
            {
                while (_rowsToSync.ContainsKey(result))
                    result = Guid.NewGuid().ToString();

                _rowsToSync.Add(result, dbRows);
            }

            return result;
        }


        private AwaitingUpdateEvent _awaitingUpdateEvent = new ();


        // ToDo - сделать возможность настраивать PingTimeout
        private readonly TimeSpan _pingTimeout = TimeSpan.FromSeconds(5);

        
        
        //ToDo - подключить серверные пинги
        public void PingConnection()
        {
            if (!_awaitingUpdateEvent.Initialized)
                return;

            lock (_rowsToSync)
            {
                if (!_awaitingUpdateEvent.Initialized)
                    return;

                if (_syncQueue.Count > 0)
                {
                    var @event = _syncQueue.Dequeue();
                    _awaitingUpdateEvent.SetResult(@event);
                }

                if (_awaitingUpdateEvent.HowLong > _pingTimeout)
                    _awaitingUpdateEvent.SetResult(new PingSyncEvent());
            }

        }

        public Task<ISyncTableEvent> IssueAwaitingUpdateEvent()
        {
            lock (_lockObject)
            {
                if (_awaitingUpdateEvent.Initialized)
                    throw new Exception("Awaiting event is already initialized");
                
                return _awaitingUpdateEvent.InitAsync();
            }
        }
    }
}