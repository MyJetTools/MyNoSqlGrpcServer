using System;
using System.Threading.Tasks;
using MyNoSqlGrpc.Server.Services.SyncQueue;

namespace MyNoSqlGrpc.Server.Services
{
    public class AwaitingUpdateEvent
    {

        private TaskCompletionSource<ISyncTableEvent> _event;
        private DateTime DateOfInitialization { get; set; }


        public bool Initialized => _event != null;

        public TimeSpan HowLong => DateTime.UtcNow - DateOfInitialization;
        
        public Task<ISyncTableEvent> InitAsync()
        {
            DateOfInitialization = DateTime.UtcNow;
            _event = new TaskCompletionSource<ISyncTableEvent>();
            return _event.Task;
        }

        public void SetResult(ISyncTableEvent result)
        {
            _event.SetResult(result);
            _event = null;
        }
    }
}