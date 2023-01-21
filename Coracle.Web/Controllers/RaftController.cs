using ActivityLogger.Logging;
using Coracle.Raft.Engine.Operational;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Samples.Logging;
using Microsoft.AspNetCore.Mvc;

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

        public RaftController(IExternalRpcHandler externalRpcHandler, IActivityLogger activityLogger)
        {
            ExternalRpcHandler = externalRpcHandler;
            ActivityLogger = activityLogger;
        }

        public IExternalRpcHandler ExternalRpcHandler { get; }
        public IActivityLogger ActivityLogger { get; }

        public async Task<Operation<IAppendEntriesRPCResponse>> AppendEntries()
        {
            Operation<IAppendEntriesRPCResponse> operationResult = new Operation<IAppendEntriesRPCResponse>();

            try
            {
                var rpc = await GetObject<AppendEntriesRPC>(HttpContext.Request);

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

        public async Task<Operation<IRequestVoteRPCResponse>> RequestVote()
        {
            Operation<IRequestVoteRPCResponse> operationResult = new Operation<IRequestVoteRPCResponse>();

            try
            {
                var rpc = await GetObject<RequestVoteRPC>(HttpContext.Request);

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

        public static async Task<TContent> GetObject<TContent>(HttpRequest httpRequest) where TContent : class
        {
            return await httpRequest.ReadFromJsonAsync<TContent>();
        }
    }
}
