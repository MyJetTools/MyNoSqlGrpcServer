using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MyNoSqlGrpc.Engine.ServerSessions
{
    

    public class MyNoSqlReaderSessionsList 
    {
        private readonly TimeSpan _expirationSessionTimeout;
        private readonly Dictionary<string, MyNoSqlReaderSession> _sessions = new();

        private IReadOnlyList<MyNoSqlReaderSession> _sessionsAsList = new List<MyNoSqlReaderSession>();

        private readonly ReaderWriterLockSlim _lockSlim = new ();

        public MyNoSqlReaderSessionsList(IMyNoSqlGrpcEngineSettings settings)
        {
            _expirationSessionTimeout = settings.SessionExpiration;
        }


        /// <summary>
        /// True - we continue existing session
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="appName"></param>
        /// <param name="pingTimeout">Timeout to ping the session</param>
        /// <returns></returns>
        public bool RegisterNewSession(string connectionId, string appName, TimeSpan pingTimeout)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                if (_sessions.TryGetValue(connectionId, out var session))
                    return true;

                session = new MyNoSqlReaderSession(connectionId, appName, pingTimeout);
                _sessions.Add(connectionId, session);
                _sessionsAsList = _sessions.Values.ToList();
                return false;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public MyNoSqlReaderSession TryGetSession(string connectionId)
        {
            MyNoSqlReaderSession result;
            
            _lockSlim.EnterReadLock();
            try
            {
                result = _sessions.TryGetValue(connectionId, out var session)
                    ? session
                    : default;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }

            if (result != null)
                result.LastUseTime = DateTime.UtcNow;

            return result;
        }


        private void RemoveSessions(IEnumerable<MyNoSqlReaderSession> sessions)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                foreach (var session in sessions)
                {
                    if (_sessions.ContainsKey(session.SessionId))
                        _sessions.Remove(session.SessionId);
                }
                
                _sessionsAsList = _sessions.Values.ToList();
            }
            finally
            {

                _lockSlim.ExitWriteLock();
            }
        }

        public void PingSessions()
        {
            List<MyNoSqlReaderSession> expiredSessions = null;
            
            _lockSlim.EnterReadLock();
            try
            {
                var dateTime = DateTime.UtcNow;
                foreach (var session in _sessions.Values)
                {
                    if (dateTime - session.LastUseTime > _expirationSessionTimeout)
                    {
                        expiredSessions ??= new List<MyNoSqlReaderSession>();
                        expiredSessions.Add(session);
                        session.Dispose();
                    }
                    else
                    {
                        session.PushOrPing();    
                    }
                    
                }
                    
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
            
            if (expiredSessions != null)
                RemoveSessions(expiredSessions);
        }


        public IReadOnlyList<MyNoSqlReaderSession> GetSessions()
        {
            return _sessionsAsList;
        }
    }
}