#region License
// Copyright (c) 2023 Ayan Choudhury
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using ActivityLogger.Logging;
using Coracle.Raft.Engine.Actions.Contexts;
using Coracle.Raft.Engine.Exceptions;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.ActivityLogger;
using TaskGuidance.BackgroundProcessing.Actions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Command;

namespace Coracle.Raft.Engine.Actions.Core
{
    internal sealed class OnClientCommandReceive<TCommand> : BaseAction<OnClientCommandReceiveContext<TCommand>, CommandExecutionResult> where TCommand : class, ICommand 
    {
        #region Constants

        public const string ActionName = nameof(OnClientCommandReceive<TCommand>);
        public const string commandId = nameof(commandId);
        public const string currentState = nameof(currentState);
        public const string RetryingAsLeaderNodeNotFound = nameof(RetryingAsLeaderNodeNotFound);
        public const string ForwardingCommandToLeader = nameof(ForwardingCommandToLeader);
        public const string AppendingNewEntryForNonReadOnlyCommand = nameof(AppendingNewEntryForNonReadOnlyCommand);
        public const string NotifyOtherNodes = nameof(NotifyOtherNodes);
        public const string CommandApplied = nameof(CommandApplied);

        #endregion

        private IEngineConfiguration Configuration => Input.EngineConfiguration;
        private bool IncludeCommand => Input.EngineConfiguration.IncludeOriginalClientCommandInResults;
        public override string UniqueName => ActionName;
        public override TimeSpan TimeOut => TimeSpan.FromMilliseconds(Configuration.ClientCommandTimeout_InMilliseconds);

        public OnClientCommandReceive(TCommand command, IStateDevelopment state, OnClientCommandReceiveContextDependencies actionDependencies, IActivityLogger activityLogger = null) : base(new OnClientCommandReceiveContext<TCommand>(state, actionDependencies)
        {
            Command = command,
            UniqueCommandId = command.UniqueId
        }, activityLogger)
        { }


        #region Workflow

        protected override CommandExecutionResult DefaultOutput()
        {
            return new CommandExecutionResult
            {
                IsSuccessful = false,
                LeaderNodeConfiguration = null,
                Exception = ClientCommandDeniedException.New(),
                OriginalCommand = IncludeCommand ? Input.Command : null,
            };
        }

        protected override Task<bool> ShouldProceed()
        {
            return Task.FromResult(Input.IsContextValid);
        }

        protected override async Task<CommandExecutionResult> Action(CancellationToken cancellationToken)
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
                .With(ActivityParam.New(commandId, Input.UniqueCommandId))
                .WithCallerInfo());

                await Task.Delay(Configuration.NoLeaderElectedWaitInterval_InMilliseconds, CancellationManager.CoreToken);
            }

            CommandExecutionResult result;

            /// <remarks>
            /// The leader handles all client requests (if a client contacts a follower, the follower redirects it to the leader).
            /// <seealso cref="Section 5.1 Raft Basics"/>
            /// </remarks>
            if (Input.State is not Leader || !Input.IsContextValid)
            {
                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = ActionName,
                    Event = ForwardingCommandToLeader,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(commandId, Input.UniqueCommandId))
                .With(ActivityParam.New(currentState, Input.State.StateValue.ToString()))
                .WithCallerInfo());

                result = new CommandExecutionResult
                {
                    IsSuccessful = false,
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
            /// 
            var (IsExecutedAndLatest, CommandResult) = await Input.ClientRequestHandler.IsExecutedAndLatest(Input.Command.UniqueId);

            if (IsExecutedAndLatest)
            {
                return CommandResult;
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
                    var (IsExecuted, Result) = await Input.ClientRequestHandler.TryGetResult(Input.Command);
                    return Result;
                }
                else
                {
                    result = new CommandExecutionResult
                    {
                        IsSuccessful = false,
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
                    EntitySubject = ActionName,
                    Event = AppendingNewEntryForNonReadOnlyCommand,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(commandId, Input.UniqueCommandId))
                .With(ActivityParam.New(currentState, Input.State.StateValue.ToString()))
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
                var logEntry = await Input.PersistentState.AppendNewCommandEntry(Input.Command);

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
                .With(ActivityParam.New(commandId, Input.UniqueCommandId))
                .With(ActivityParam.New(currentState, Input.State.StateValue.ToString()))
                .WithCallerInfo());

                Input.AppendEntriesManager.InitiateAppendEntries();

                /// <remarks>
                /// When the entry has been safely replicated (as described below), the leader applies
                /// the entry to its state machine and returns the result of that execution to the client.
                /// 
                /// If followers crash or run slowly, or if network packets are lost, the leader retries AppendEntries RPCs 
                /// indefinitely (even after it has responded to the client) until all followers eventually store all log entries.
                /// </remarks>

                Input.GlobalAwaiter.AwaitEntryCommit(logEntry.CurrentIndex, cancellationToken);

                /// <remarks>
                /// Wait for UpdateCommitIndex to apply some Commands. 
                /// </remarks>

                var (IsExecuted, Result) = await Input.ClientRequestHandler.TryGetResult(Input.Command);

                ActivityLogger?.Log(new CoracleActivity
                {
                    EntitySubject = ActionName,
                    Event = CommandApplied,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(commandId, Input.UniqueCommandId))
                .WithCallerInfo());

                return Result;
            }
            else
            {
                return new CommandExecutionResult
                {
                    IsSuccessful = false,
                    LeaderNodeConfiguration = Input.LeaderNodePronouncer.RecognizedLeaderConfiguration,
                    Exception = ClientCommandDeniedException.New(),
                    OriginalCommand = IncludeCommand ? Input.Command : null,
                };
            }
        }

        #endregion
    }
}
