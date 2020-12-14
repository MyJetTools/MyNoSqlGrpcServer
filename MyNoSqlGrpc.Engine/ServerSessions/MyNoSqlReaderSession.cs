using System;
using System.Threading.Tasks;
using MyNoSqlGrpc.Engine.ServerSyncEvents;

namespace MyNoSqlGrpc.Engine.ServerSessions
{
    public class MyNoSqlReaderSession : IDisposable
    {
        public string SessionId { get; }
        public string AppName { get; }

        public DateTime LastUseTime { get; internal set; } = DateTime.UtcNow;

        private readonly object _lockObject = new();
                
        private readonly TimeSpan _pingTimeout;
        public MyNoSqlReaderSession(string sessionId, string appName, TimeSpan pingTimeout)
        {
            SessionId = sessionId;
            AppName = appName;
            _pingTimeout = pingTimeout;
        }

        private readonly SessionSubscribers _sessionSubscribers = new();
        
        public void SubscribeToPartition(string dbTable, string partitionKey)
        {
            lock (_lockObject)
            {
                _sessionSubscribers.Subscribe(dbTable, partitionKey);
                TryDeliverMessageToAwaitingEvent();
            }
        }


        private readonly SessionEventsQueue _sessionEventsQueue = new();

        public ISyncChangeEvent GetDataToSync()
        {
            lock (_lockObject)
                return _sessionEventsQueue.Dequeue();
        }

        public void SyncTableEvent(ISyncChangeEvent syncEvent)
        {

            lock (_lockObject)
            {

                if (syncEvent is ISyncTableEvent syncTableEvent)
                    if (!_sessionSubscribers.IsSubscribedTo(syncTableEvent.TableName))
                        return;

                if (syncEvent is ISyncTablePartitionEvent tablePartition)
                    if (!_sessionSubscribers.IsSubscribedTo(tablePartition.TableName, tablePartition.PartitionKey))
                        return;


                _sessionEventsQueue.Enqueue(syncEvent);


                TryDeliverMessageToAwaitingEvent();
            }
        }

        public readonly RowsToSync RowsToSync = new();

        private readonly AwaitingUpdateEvent _awaitingUpdateEvent = new ();

        private void TryDeliverMessageToAwaitingEvent()
        {
            if (!_awaitingUpdateEvent.Initialized)
                return;

            var nextEvent = _sessionEventsQueue.Dequeue();

            if (nextEvent != null)
                _awaitingUpdateEvent.SetResult(nextEvent); 
        }

        public void PushOrPing()
        {
            lock (_lockObject)
            {
                if (!_awaitingUpdateEvent.Initialized)
                    return;

                TryDeliverMessageToAwaitingEvent();

                if (_awaitingUpdateEvent.HowLong > _pingTimeout)
                    _awaitingUpdateEvent.SetResult(new PingSyncEvent());
            }
        }

        public Task<ISyncChangeEvent> IssueAwaitingUpdateEvent()
        {
            lock (_lockObject)
            {
                if (_awaitingUpdateEvent.Initialized)
                    throw new Exception("Awaiting event is already initialized");
                
                var result = _awaitingUpdateEvent.InitAsync();
                TryDeliverMessageToAwaitingEvent();
                return result;
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                if (_awaitingUpdateEvent.Initialized)
                    _awaitingUpdateEvent.SetExpired();
            }
        }
    }
}