using System.Collections.Generic;
using System.Threading;

namespace MyNoSqlGrpc.Server.Services
{
    public class MyNoSqlReaderSessionsList
    {
        private readonly Dictionary<string, MyNoSqlReaderSession> _sessions = new();

        private readonly ReaderWriterLockSlim _lockSlim = new ();

        
        /// <summary>
        /// True - we continue existing session
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="appName"></param>
        /// <returns></returns>
        public bool RegisterNewSession(string connectionId, string appName)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                if (_sessions.TryGetValue(connectionId, out var session))
                    return true; //ToDo - we Have to Initialize Session Since we found it

                session = new MyNoSqlReaderSession(connectionId, appName);
                _sessions.Add(connectionId, session);
                return false;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public MyNoSqlReaderSession TryGetSession(string connectionId)
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _sessions.TryGetValue(connectionId, out var session) 
                    ? session 
                    : default;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }
    }
}