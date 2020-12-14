using System;
using System.Threading.Tasks;
using MyNoSqlGrpc.Engine.ServerSyncEvents;

namespace MyNoSqlGrpc.Engine.ServerSessions
{
    public class AwaitingUpdateEvent
    {

        private TaskCompletionSource<ISyncChangeEvent> _event;
        private DateTime DateOfInitialization { get; set; }

        public bool Initialized => _event != null;

        public TimeSpan HowLong => DateTime.UtcNow - DateOfInitialization;
        
        public Task<ISyncChangeEvent> InitAsync()
        {
            DateOfInitialization = DateTime.UtcNow;
            _event = new TaskCompletionSource<ISyncChangeEvent>();
            return _event.Task;
        }

        public void SetResult(ISyncChangeEvent result)
        {
            _event.SetResult(result);
            _event = null;
        }

        public void SetExpired()
        {
            _event.SetException(new TimeoutException("Session is expired"));
        }
    }
}