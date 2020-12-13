using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyNoSqlGrpcServer.GrpcContracts;

namespace MyNoSqlGrpc.Reader.GrpcConnection
{
    
    
    public class MyNoSqlGrpcReaderConnection
    {
        private readonly TimeSpan _pingTimeSpan;
        private readonly TimeSpan _disconnectTimeSpan;
        public IMyNoSqlGrpcServerReader MyNoSqlGrpcServerReader { get; }
        public string ConnectionId { get; } = Guid.NewGuid().ToString();
        public string AppName { get; }
        
        public MyNoSqlGrpcReaderConnection(IMyNoSqlGrpcServerReader myNoSqlGrpcServerReader, string appName, TimeSpan pingTimeSpan)
        {
            _pingTimeSpan = pingTimeSpan;
            _disconnectTimeSpan = pingTimeSpan * 3;
            AppName = appName;

            MyNoSqlGrpcServerReader = myNoSqlGrpcServerReader;
        }


        private readonly Dictionary<string, List<Action<IGrpcConnectionUpdateCommand>>> _updateCommand = new ();

        public void SubscribeToUpdateEvent(string tableName, Action<IGrpcConnectionUpdateCommand> updateCommand)
        {
            if (!_updateCommand.ContainsKey(tableName))
                _updateCommand.Add(tableName, new List<Action<IGrpcConnectionUpdateCommand>>());
            _updateCommand[tableName].Add(updateCommand);
        }

        internal void InvokeUpdateEvent(IGrpcConnectionUpdateCommand command)
        {
            if (_updateCommand.TryGetValue(command.TableName, out var result))
                foreach (var updateAction in result)
                    updateAction(command);
        }


        private readonly object _lockObject = new ();
        private readonly Dictionary<string, List<string>> _partitions = new ();

        public CancellationTokenSource CancellationTokenSource { get; private set; }

        internal Dictionary<string, List<string>> GetPartitionsToDownload()
        {
            lock (_lockObject)
            {
                return new Dictionary<string, List<string>>(_partitions);
            }
        }
        
        private async Task ConnectionLoopAsync()
        {

            while (Started)
            {
                try
                {
                  
                    CancellationTokenSource = new CancellationTokenSource();
                    await Task.Delay(500);
                    
                    LastReceiveTime = DateTime.UtcNow;
                    
                    var result = await MyNoSqlGrpcServerReader.GreetingAsync(new GreetingGrpcRequest
                    {
                        AppName = AppName,
                        ConnectionId = ConnectionId
                    }, CancellationTokenSource.Token);
                    LastReceiveTime = DateTime.UtcNow;

                    
                    if (!result.WeContinueSession)
                        await this.DownloadPartitionsIfSessionIsNew();
             
                    await EventsLoopAsync();

                }
                catch (Exception e)
                {
                    //ToDo - Made it Loggable
                    Console.WriteLine(e);
                }
            }

        }

        private async Task EventsLoopAsync()
        {
            var grpcRequest = new GetUpdatesGrpcRequest
            {
                ConnectionId = ConnectionId
            };

            var deadConnectionTask = DeadConnectionDetectionAsync();
            
            while (CancellationTokenSource.IsCancellationRequested)
            {
                var update = await MyNoSqlGrpcServerReader
                    .GetUpdatesAsync(grpcRequest, CancellationTokenSource.Token);
                
                var command = await this.HandleNewDataAsync(update);
                    
                if (command is SkipItUpdateResult)
                    continue;
                
                InvokeUpdateEvent(command);
            }

            await deadConnectionTask;

        }
        
        public DateTime LastReceiveTime { get; internal set; } = DateTime.UtcNow;

        private async Task DeadConnectionDetectionAsync()
        {
            while (DateTime.UtcNow - LastReceiveTime > _disconnectTimeSpan)
            {
                await Task.Delay(_pingTimeSpan);
            }
            
            CancellationTokenSource.Cancel();
        }
        
        public bool Started { get; private set; }

        public void Start()
        {
            lock (_lockObject)
            {
                if (!Started)
                {
                    Started = true;
                    Task.Run(ConnectionLoopAsync);
                }
                    
                Started = true;
            }
        }

    }
}