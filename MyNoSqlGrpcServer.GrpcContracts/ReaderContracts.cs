using System.Runtime.Serialization;

namespace MyNoSqlGrpcServer.GrpcContracts
{

    [DataContract]
    public class GreetingGrpcRequest
    {
        [DataMember(Order = 1)]
        public string AppName { get; set; }
        
        [DataMember(Order = 4)]
        public string ConnectionId { get; set; }
    }

    
    [DataContract]
    public class GreetingGrpcResponse
    {
        [DataMember(Order = 1)]
        public bool WeContinueSession { get; set; }
    }
    
    
    [DataContract]
    public class SubscribeGrpcRequest
    {
        [DataMember(Order = 1)]
        public string ConnectionId { get; set; }
        
        [DataMember(Order = 2)]
        public string TableName { get; set; }
        
        [DataMember(Order = 3)]
        public string PartitionKey { get; set; }
    }

    [DataContract]
    public class GetUpdatesGrpcRequest
    {
        [DataMember(Order = 1)]
        public string ConnectionId { get; set; }
    }


    [DataContract]
    public class UpdatesGrpcResponse
    {

        [DataMember(Order = 1)]
        public string TableName { get; set; }

        [DataMember(Order = 2)]
        public bool ClearTable { get; set; }
        
        [DataMember(Order = 3)]
        public string ResetPartitionKey { get; set; }
        
        [DataMember(Order = 4)]
        public DbRowGrpcModel[] DbRows { get; set; }
        
        [DataMember(Order = 5)]
        public string DownloadRows { get; set; } 
    }
    
    
    [DataContract]
    public class DownloadPartitionGrpcRequest
    {
        
        [DataMember(Order = 1)]
        public string ConnectionId { get; set; }
        
        [DataMember(Order = 2)]
        public string TableName { get; set; }
        
        [DataMember(Order = 3)]
        public string PartitionKey { get; set; }
    }
    
    [DataContract]
    public class SyncRowsGrpcRequest
    {
        [DataMember(Order = 1)]
        public string ConnectionId { get; set; }
        
        [DataMember(Order = 2)]
        public string TableName { get; set; }
        
        [DataMember(Order = 3)]
        public string SnapshotId { get; set; }
    }
}