using System.Collections.Generic;

namespace MyNoSqlGrpc.Engine.ServerSessions
{
    public class SessionSubscribers
    {
        public readonly Dictionary<string, Dictionary<string, string>> Subscribers = new ();


 
        public bool IsSubscribedTo(string dbTable, string partitionKey)
        {
            if (Subscribers.TryGetValue(dbTable, out var partitions))
                return partitions.ContainsKey(partitionKey);

            return false;
        }
        
        public bool IsSubscribedTo(string dbTable)
        {
            return Subscribers.ContainsKey(dbTable);
        }

        public void Subscribe(string dbTable, string partitionKey)
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
}