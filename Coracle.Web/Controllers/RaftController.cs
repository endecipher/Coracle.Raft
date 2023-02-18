using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Core;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Logs;
using Coracle.Raft.Engine.Operational;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Samples.ClientHandling.NoteCommand;
using Coracle.Samples.Logging;
using Coracle.Samples.PersistentData;
using Coracle.Web.Impl.Node;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Coracle.Web.Controllers
{
    public class RaftController : Controller
    {
        public const string Raft = nameof(Raft);

        public const string Entity = nameof(RaftController);

        public const string InternalError = nameof(InternalError);

        public const string ControllerError = nameof(ControllerError);

        public const string exception = nameof(exception);
        public static string AppendEntriesEndpoint => Raft + "/" + nameof(AppendEntries);
        public static string RequestVoteEndpoint => Raft + "/" + nameof(RequestVote);
        public static string InstallSnapshotEndpoint => Raft + "/" + nameof(InstallSnapshot);

        public RaftController(IExternalRpcHandler externalRpcHandler, IActivityLogger activityLogger, ICoracleNodeAccessor nodeAccessor)
        {
            ExternalRpcHandler = externalRpcHandler;
            ActivityLogger = activityLogger;
            NodeAccessor = nodeAccessor;
        }

        public IExternalRpcHandler ExternalRpcHandler { get; }
        public IActivityLogger ActivityLogger { get; }
        public ICoracleNodeAccessor NodeAccessor { get; }

        public async Task<Operation<IAppendEntriesRPCResponse>> AppendEntries()
        {
            if (!NodeAccessor.CoracleNode.IsInitialized || !NodeAccessor.CoracleNode.IsStarted)
            {
                return new Operation<IAppendEntriesRPCResponse>
                {
                    Exception = new Exception("Node not started yet")
                };
            }

            Operation<IAppendEntriesRPCResponse> operationResult = new Operation<IAppendEntriesRPCResponse>();

            try
            {
                var rpc = await HttpContext.Request.ReadFromJsonAsync<AppendEntriesRPC>();

                if (rpc.Entries != null && rpc.Entries.Any())
                {
                    FillContent(rpc.Entries);
                }

                operationResult = await ExternalRpcHandler.RespondTo(rpc, HttpContext.RequestAborted);

                if (operationResult?.Exception != null)
                {
                    ActivityLogger?.Log(new ImplActivity
                    {
                        EntitySubject = Entity,
                        Level = ActivityLogLevel.Error,
                        Event = InternalError,
                    }
                    .With(ActivityParam.New(exception, operationResult.Exception)));
                }
            }
            catch (Exception ex)
            {
                ActivityLogger?.Log(new ImplActivity
                {
                    EntitySubject = Entity,
                    Level = ActivityLogLevel.Error,
                    Event = ControllerError,
                }
                .With(ActivityParam.New(exception, ex)));
            }
            
            return operationResult;
        }

        public async Task<Operation<IInstallSnapshotRPCResponse>> InstallSnapshot()
        {
            if (!NodeAccessor.CoracleNode.IsInitialized || !NodeAccessor.CoracleNode.IsStarted)
            {
                return new Operation<IInstallSnapshotRPCResponse>
                {
                    Exception = new Exception("Node not started yet")
                };
            }

            Operation<IInstallSnapshotRPCResponse> operationResult = new Operation<IInstallSnapshotRPCResponse>();

            try
            {
                var rpc = await HttpContext.Request.ReadFromJsonAsync<InstallSnapshotRPC>();

                FillChunkData(rpc);

                operationResult = await ExternalRpcHandler.RespondTo(rpc, HttpContext.RequestAborted);

                if (operationResult?.Exception != null)
                {
                    ActivityLogger?.Log(new ImplActivity
                    {
                        EntitySubject = Entity,
                        Level = ActivityLogLevel.Error,
                        Event = InternalError,
                    }
                    .With(ActivityParam.New(exception, operationResult.Exception)));
                }
            }
            catch (Exception ex)
            {
                ActivityLogger?.Log(new ImplActivity
                {
                    EntitySubject = Entity,
                    Level = ActivityLogLevel.Error,
                    Event = ControllerError,
                }
                .With(ActivityParam.New(exception, ex)));
            }

            return operationResult;
        }

        private void FillContent(IEnumerable<LogEntry> entries)
        {
            foreach (var logEntry in entries)
            {
                if (logEntry.Type.HasFlag(LogEntry.Types.Command))
                {
                    string value = logEntry.Content.ToString();

                    NoteCommand command = JsonConvert.DeserializeObject<NoteCommand>(value);

                    logEntry.Content = command;
                }
                else if (logEntry.Type.HasFlag(LogEntry.Types.Configuration))
                {
                    string value = logEntry.Content.ToString();

                    IEnumerable<NodeConfiguration> config = JsonConvert.DeserializeObject<List<NodeConfiguration>>(value);

                    logEntry.Content = config;
                }
            }
        }

        private void FillChunkData(InstallSnapshotRPC rpc)
        {
            string value = rpc.Data.ToString();

            ISnapshotChunkData command = JsonConvert.DeserializeObject<DataChunk>(value);

            rpc.Data = command;
        }

        public async Task<Operation<IRequestVoteRPCResponse>> RequestVote()
        {
            if (!NodeAccessor.CoracleNode.IsInitialized || !NodeAccessor.CoracleNode.IsStarted)
            {
                return new Operation<IRequestVoteRPCResponse>
                {
                    Exception = new Exception("Node not started yet")
                };
            }

            Operation<IRequestVoteRPCResponse> operationResult = new Operation<IRequestVoteRPCResponse>();

            try
            {
                var rpc = await HttpContext.Request.ReadFromJsonAsync<RequestVoteRPC>();

                operationResult = await ExternalRpcHandler.RespondTo(rpc, HttpContext.RequestAborted);

                if (operationResult?.Exception != null)
                {
                    ActivityLogger?.Log(new ImplActivity
                    {
                        EntitySubject = Entity,
                        Level = ActivityLogLevel.Error,
                        Event = InternalError,
                    }
                    .With(ActivityParam.New(exception, operationResult.Exception)));
                }
            }
            catch (Exception ex)
            {
                ActivityLogger?.Log(new ImplActivity
                {
                    EntitySubject = Entity,
                    Level = ActivityLogLevel.Error,
                    Event = ControllerError,
                }
                .With(ActivityParam.New(exception, ex)));
            }

            return operationResult;
        }
    }
}
