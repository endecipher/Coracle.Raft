using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.Actions.Contexts;
using Core.Raft.Canoe.Engine.ActivityLogger;
using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Command;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Exceptions;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Structure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Actions
{
    /// <summary>
    /// This is invoked from the node to handle a Client Command
    /// </summary>
    internal sealed class OnClientCommandReceive<TCommand> : EventAction<OnClientCommandReceiveContext<TCommand>, ClientHandlingResult> where TCommand : class, ICommand //where TCommandResult : ClientHandlingResult, new() 
    {
        #region Constants

        public const string ActionName = nameof(OnClientCommandReceive<TCommand>);
        public const string CommandId = nameof(CommandId);
        public const string CurrentState = nameof(CurrentState);
        public const string RetryingAsLeaderNodeNotFound = nameof(RetryingAsLeaderNodeNotFound); 
        public const string ForwardingCommandToLeader = nameof(ForwardingCommandToLeader); 
        public const string AppendingNewEntryForNonReadOnlyCommand = nameof(AppendingNewEntryForNonReadOnlyCommand); 
        public const string NotifyOtherNodes = nameof(NotifyOtherNodes); 
        public const string CommandApplied = nameof(CommandApplied); 
        public const string NodeId = nameof(NodeId); 
        public const string NodeActionName = nameof(NodeActionName); 

        #endregion

        /// <summary>
        /// The TimeConfiguration maybe changed dynamically on observation of the system
        /// </summary>
        private IEngineConfiguration Configuration => Input.EngineConfiguration;
        private bool IncludeCommand => Input.EngineConfiguration.IncludeOriginalClientCommandInResults;

        public override string UniqueName => ActionName;

        public OnClientCommandReceive(TCommand command, IChangingState state, OnClientCommandReceiveContextDependencies actionDependencies, IActivityLogger activityLogger = null) : base(new OnClientCommandReceiveContext<TCommand>(state, actionDependencies)
        {
            Command = command,
            InvocationTime = DateTimeOffset.UtcNow,
            UniqueCommandId = command.UniqueId
        }, activityLogger)
        { }

        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Configuration.ClientCommandTimeout_InMilliseconds);

        #region Workflow

        protected override ClientHandlingResult DefaultOutput()
        {
            return new ClientHandlingResult
            {
                IsOperationSuccessful = false,
                LeaderNodeConfiguration = null,
                Exception = LeaderNotFoundException.New(),
                OriginalCommand = IncludeCommand ? Input.Command : null,
            };
        }

        protected override Task<bool> ShouldProceed()
        {
            return Task.FromResult(Input.IsContextValid);
        }

        protected override async Task<ClientHandlingResult> Action(CancellationToken cancellationToken)
        {
            /// <remarks>
            /// Clients of Raft send all of their requests to the leader.
            /// When a client first starts up, it connects to a randomly chosen server. If the client’s first choice is not the leader,
            /// that server will reject the client’s request and supply information about the most recent leader it has heard from
            /// (AppendEntries requests include the network address of the leader). If the leader crashes, client requests will time
            /// out; clients then try again with randomly-chosen servers.
            /// 
            /// <seealso cref="Section 8 Client Interaction"/>
            /// </remarks>
            while (!Input.LeaderNodePronouncer.IsLeaderRecognized)
            {
                CancellationManager.ThrowIfCancellationRequested();

                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"Leader Node Configuration not found. Retrying..",
                    EntitySubject = ActionName,
                    Event = RetryingAsLeaderNodeNotFound,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(CommandId, Input.UniqueCommandId))
                .WithCallerInfo());

                await Task.Delay(Configuration.NoLeaderElectedWaitInterval_InMilliseconds, CancellationManager.CoreToken);
            }

            ClientHandlingResult result;

            /// <remarks>
            /// The leader handles all client requests (if a client contacts a follower, the follower redirects it to the leader).
            /// <seealso cref="Section 5.1 Raft Basics"/>
            /// </remarks>
            //TODO: Remove ForwardToLeader Logic. And just return back that, hey, this is not the leader.
            if (Input.State is not Leader || !Input.IsContextValid)
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"Current State Value is {Input.State.StateValue}. Forwarding to Leader..",
                    EntitySubject = ActionName,
                    Event = ForwardingCommandToLeader,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(CommandId, Input.UniqueCommandId))
                .With(ActivityParam.New(CurrentState, Input.State.StateValue.ToString()))
                .WithCallerInfo());

                result = new ClientHandlingResult
                {
                    IsOperationSuccessful = false,
                    LeaderNodeConfiguration = Input.LeaderNodePronouncer.RecognizedLeaderConfiguration,
                    Exception = ClientCommandDeniedException.New(),
                    OriginalCommand = IncludeCommand ? Input.Command : null,
                };

                return result;
            }

            /// <remarks>
            /// ..so far Raft can execute a command multiple times:
            /// 
            /// If the leader crashes after committing the log entry but before responding to the client, the client will retry the command with a
            /// new leader, causing it to be executed a second time. The solution is for clients to assign unique serial numbers to
            /// every command. Then, the state machine tracks the latest serial number processed for each client, along with the associated response.
            /// If it receives a command whose serial number has already been executed, it responds immediately without re - executing the request.
            /// 
            /// <seealso cref="Section 8 Client Interaction"/>
            /// </remarks>
            if (await Input.ClientRequestHandler.IsCommandLatest(Input.Command.UniqueId, out var executedCommandResult))
            {
                return executedCommandResult;
            }

            /// <remarks>
            /// A leader must check whether it has been deposed before processing a read-only request (its information may be stale if a more recent leader has been elected).
            /// Raft handles this by having the leader exchange heartbeat messages with a majority of the cluster before responding to read - only requests.
            /// <seealso cref="Section 8 Client Interaction"/>
            /// </remarks>
            if (Input.Command.IsReadOnly && Input.State is Leader leaderState)
            {
                leaderState.SendHeartbeat(null);

                Input.GlobalAwaiter.AwaitNoDeposition(cancellationToken);

                if (Input.State.StateValue.IsLeader())
                {
                    await Input.ClientRequestHandler.TryGetCommandResult<TCommand>(Input.Command, out var clientHandlingResult);
                    return clientHandlingResult;
                }
                else
                {
                    result = new ClientHandlingResult
                    {
                        IsOperationSuccessful = false,
                        LeaderNodeConfiguration = Input.LeaderNodePronouncer.RecognizedLeaderConfiguration,
                        Exception = ClientCommandDeniedException.New(),
                        OriginalCommand = IncludeCommand ? Input.Command : null,
                    };

                    return result;
                }
            }
            else if (!Input.Command.IsReadOnly && Input.State.StateValue.IsLeader())
            {
                /// <remarks>
                /// We just append the Log Entry, thus increasing the Last Log Index. 
                /// Log Entry Application will be triggered when CommitIndex is updated.
                /// </remarks>
                /// 

                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"Current State Value is {Input.State.StateValue}. Appending New Entry..",
                    EntitySubject = ActionName,
                    Event = AppendingNewEntryForNonReadOnlyCommand,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(CommandId, Input.UniqueCommandId))
                .With(ActivityParam.New(CurrentState, Input.State.StateValue.ToString()))
                .WithCallerInfo());


                /// <remarks>
                /// Each client request contains a command to be executed by the replicated state machines.
                /// The leader appends the command to its log as a new entry, then issues AppendEntries RPCs in parallel to each of the other 
                /// servers to replicate the entry.
                /// <seealso cref="Section 5.3"/>
                /// 
                /// 
                /// A command can complete as soon as a majority of the cluster has responded to a single round of remote procedure calls; a minority of
                /// slow servers need not impact overall system performance.
                /// <seealso cref="Section 2"/>
                /// </remarks>
                /// 
                /// <remarks>
                /// Each log entry stores a state machine command along with the term number when the entry was received by the leader.
                /// The term numbers in log entries are used to detect inconsistencies between logs and to ensure some of the properties
                /// in Figure 3. Each log entry also has an integer index identifying its position in the log.
                /// <seealso cref="Section 5.3 Log Replication"/>
                /// </remarks>
                var logEntry = await Input.PersistentState.LogEntries.AppendNewCommandEntry<TCommand>(Input.Command);

                /// <remarks>
                /// Each client request contains a command to be executed by the replicated state machines. 
                /// The leader appends the command to its log as a new entry, then issues AppendEntries RPCs in parallel to each of the other
                /// servers to replicate the entry. When the entry has been safely replicated, the leader applies the entry to its state machine 
                /// and returns the result of that execution to the client. 
                /// 
                /// If followers crash or run slowly, or if network packets are lost, the leader retries AppendEntries RPCs indefinitely 
                /// (even after it has responded to the client) until all followers eventually store all log entries.
                /// <seealso cref="Section 5.3 Log Replication"/>
                /// </remarks>

                /// <summary>
                /// Servers retry RPCs if they do not receive a response in a timely manner, and they issue RPCs in parallel for best performance.
                /// <see cref="Section 5.1 End Para"/>
                /// </summary>
                /// 

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = ActionName,
                    Event = NotifyOtherNodes,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(CommandId, Input.UniqueCommandId))
                .With(ActivityParam.New(CurrentState, Input.State.StateValue.ToString()))
                .WithCallerInfo());

                Input.AppendEntriesManager.InitiateAppendEntries();

                ClientHandlingResult clientHandlingResult;

                /// <remarks>
                /// When the entry has been
                /// safely replicated(as described below), the leader applies
                /// the entry to its state machine and returns the result of that
                /// execution to the client.
                /// 
                /// If followers crash or run slowly,
                /// or if network packets are lost, the leader retries AppendEntries RPCs indefinitely (even after it has responded to
                /// the client) until all followers eventually store all log entries.
                /// </remarks>
                /// 
                //TODO: Make sure that Global Awaiter is a dependency which makes us wait until a specific log entry index is comitted. This thing is used like everywhere

                Input.GlobalAwaiter.AwaitEntryCommit(logEntry.CurrentIndex, cancellationToken);

                /// <remarks>
                /// Wait for UpdateCommitIndex to apply some Commands. 
                /// 
                /// NOTE:
                /// This may seem risky, but if somehow some error occurs during those parallel tasks above (Like networking/RPC issues),
                /// then they will again be retried, as the documentation states.
                /// 
                /// If all are kept on retrying, then there is a major issue, since Majority nodes could not be reached, and this task should timeout.
                /// 
                /// If something happens in another thread which makes the State change from the current Leader, then ideally this task will be cancelled, as
                /// this OnClientCommand was initiated with executeSeparately false. (Thus binded with Responsibilities.CoreToken)
                /// </remarks>
                await Input.ClientRequestHandler.TryGetCommandResult<TCommand>(Input.Command, out clientHandlingResult);

                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"Client Command applied post-commit",
                    EntitySubject = ActionName,
                    Event = CommandApplied,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(CommandId, Input.UniqueCommandId))
                .WithCallerInfo());

                return clientHandlingResult;
            }
            else
            {
                return new ClientHandlingResult
                {
                    IsOperationSuccessful = false,
                    LeaderNodeConfiguration = Input.LeaderNodePronouncer.RecognizedLeaderConfiguration,
                    Exception = ClientCommandDeniedException.New(),
                    OriginalCommand = IncludeCommand ? Input.Command : null,
                };
            }
        }

        #endregion
    }
}
