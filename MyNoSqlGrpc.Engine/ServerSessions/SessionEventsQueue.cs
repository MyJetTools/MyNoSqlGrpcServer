using System.Collections.Generic;
using MyNoSqlGrpc.Engine.ServerSyncEvents;

namespace MyNoSqlGrpc.Engine.ServerSessions
{
    //ToDo - when we deliver the message - we have to Deliver it with the MessageId
    public class SessionEventsQueue
    {
        private readonly Queue<ISyncChangeEvent> _syncQueue = new();

        public void Enqueue(ISyncChangeEvent @event)
        {
            _syncQueue.Enqueue(@event);
        }


        private void TryToMixTheSameUpdateRows(ISyncChangeEvent changeEvent)
        {
            if (!(changeEvent is SyncRowEvent syncRowEvent))
                return;

            if (_syncQueue.Count == 0)
                return;
            var nextEvent = _syncQueue.Peek();
            while (nextEvent is SyncRowEvent syncRowNextEvent)
            {
                if (!syncRowEvent.TryMerge(syncRowNextEvent))
                    return;

                _syncQueue.Dequeue();

                if (_syncQueue.Count == 0)
                    return;
                nextEvent = _syncQueue.Peek();
            }
        }

        private void TryToMixTheSameDeleteRows(ISyncChangeEvent changeEvent)
        {
            if (!(changeEvent is DeleteDbRowEvent deleteRowEvent))
                return;

            if (_syncQueue.Count == 0)
                return;
            var nextEvent = _syncQueue.Peek();
            while (nextEvent is DeleteDbRowEvent deleteRowNextEvent)
            {
                if (!deleteRowEvent.TryMerge(deleteRowNextEvent))
                    return;

                _syncQueue.Dequeue();

                if (_syncQueue.Count == 0)
                    return;
                nextEvent = _syncQueue.Peek();
            }
        }

        public ISyncChangeEvent Dequeue()
        {
            if (_syncQueue.Count == 0)
                return null;

            var result = _syncQueue.Dequeue();
            TryToMixTheSameUpdateRows(result);
            TryToMixTheSameDeleteRows(result);

            return result;
        }
    }
}