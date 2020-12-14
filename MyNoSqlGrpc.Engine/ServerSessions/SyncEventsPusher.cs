using MyNoSqlGrpc.Engine.ServerSyncEvents;

namespace MyNoSqlGrpc.Engine.ServerSessions
{
    public class SyncEventsPusher
    {
        private readonly MyNoSqlReaderSessionsList _myNoSqlReaderSessionsList;
        private readonly SyncEventsQueue _syncEventsQueue;

        public SyncEventsPusher(MyNoSqlReaderSessionsList myNoSqlReaderSessionsList, SyncEventsQueue syncEventsQueue)
        {
            _myNoSqlReaderSessionsList = myNoSqlReaderSessionsList;
            _syncEventsQueue = syncEventsQueue;
        }

        public void PushEventsToReaders()
        {
            var nextEvent = _syncEventsQueue.TryDequeue();
            
            if (nextEvent == null)
                return;

            var sessions = _myNoSqlReaderSessionsList.GetSessions();

            foreach (var session in sessions)
            {
                session.SyncTableEvent(nextEvent);
            }
            

        }
    }
}