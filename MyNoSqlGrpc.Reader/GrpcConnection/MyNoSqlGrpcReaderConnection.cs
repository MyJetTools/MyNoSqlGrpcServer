using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using MyNoSqlGrpcServer.GrpcContracts;
using ProtoBuf.Grpc.Client;

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

        public static MyNoSqlGrpcReaderConnection Create(string grpcUrl, string appName, bool useSsl, TimeSpan pingTimeSpan)
        {
            if (!useSsl)
                GrpcClientFactory.AllowUnencryptedHttp2 = true;

            var result = GrpcChannel
                .ForAddress(grpcUrl)
                .CreateGrpcService<IMyNoSqlGrpcServerReader>();

            return new MyNoSqlGrpcReaderConnection(result, appName, pingTimeSpan);
        }


        private readonly Dictionary<string, List<Action<IGrpcConnectionUpdateCommand>>> _updateCommand = new();

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


        private readonly object _lockObject = new();
        private readonly Dictionary<string, List<string>> _partitions = new();

        public CancellationTokenSource CancellationTokenSource { get; private set; }

        internal Dictionary<string, List<string>> GetPartitionsToDownload()
        {
            lock (_lockObject)
            {
                return new Dictionary<string, List<string>>(_partitions);
            }
        }



        private TaskCompletionSource<bool> _taskCompletionConnected = new();

        public Task AwaitUntilConnected()
        {
            return _taskCompletionConnected.Task;
        }


        private async Task ConnectionLoopAsync()
        {

            while (Started)
            {
                try
                {

                    await Task.Delay(500);
                    LastReceiveTime = DateTime.UtcNow;
                    CancellationTokenSource = new CancellationTokenSource();


                    await MyNoSqlGrpcServerReader.GreetingAsync(new GreetingGrpcRequest
                    {
                        AppName = AppName,
                        ConnectionId = ConnectionId
                    }, CancellationTokenSource.Token);
                    LastReceiveTime = DateTime.UtcNow;
                    _taskCompletionConnected.SetResult(true);


                    await this.DownloadPartitionsIfSessionIsNew();

                    await EventsLoopAsync();

                }
                catch (Exception e)
                {
                    //ToDo - Made it Loggable
                    Console.WriteLine(e);
                }
                finally
                {
                    _taskCompletionConnected = new TaskCompletionSource<bool>();
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

            Console.WriteLine("Connected...");

            while (!CancellationTokenSource.IsCancellationRequested)
            {
                var update = await MyNoSqlGrpcServerReader
                    .GetUpdatesAsync(grpcRequest, CancellationTokenSource.Token);

                LastReceiveTime = DateTime.UtcNow;

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

            Console.WriteLine("Disconnect delay: " + _disconnectTimeSpan);


            while (DateTime.UtcNow - LastReceiveTime < _disconnectTimeSpan)
            {
                await Task.Delay(_pingTimeSpan);
            }

            Console.WriteLine("GRPC Connection is dead. Reconnecting...");

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