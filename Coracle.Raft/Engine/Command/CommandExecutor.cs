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
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.States.LeaderEntities;
using TaskGuidance.BackgroundProcessing.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using Coracle.Raft.Engine.Actions.Core;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Actions;

namespace Coracle.Raft.Engine.Command
{
    internal sealed class CommandExecutor : ICommandExecutor
    {
        public CommandExecutor(
            IStateMachineHandler clientRequestHandler,
            IEngineConfiguration engineConfiguration,
            ILeaderNodePronouncer leaderNodePronouncer,
            ICurrentStateAccessor currentStateAccessor,
            IResponsibilities responsibilities,
            IGlobalAwaiter globalAwaiter,
            IPersistentStateHandler persistentState,
            IActivityLogger activityLogger,
            IAppendEntriesManager appendEntriesManager)
        {
            RequestHandler = clientRequestHandler;
            EngineConfiguration = engineConfiguration;
            LeaderNodePronouncer = leaderNodePronouncer;
            GlobalAwaiter = globalAwaiter;
            PersistentState = persistentState;
            ActivityLogger = activityLogger;
            AppendEntriesManager = appendEntriesManager;
            CurrentStateAccessor = currentStateAccessor;
            Responsibilities = responsibilities;
        }

        IStateMachineHandler RequestHandler { get; }
        IEngineConfiguration EngineConfiguration { get; }
        ILeaderNodePronouncer LeaderNodePronouncer { get; }
        IGlobalAwaiter GlobalAwaiter { get; }
        IPersistentStateHandler PersistentState { get; }
        IActivityLogger ActivityLogger { get; }
        IAppendEntriesManager AppendEntriesManager { get; }
        ICurrentStateAccessor CurrentStateAccessor { get; }
        IResponsibilities Responsibilities { get; }

        public Task<CommandExecutionResult> Execute<TCommand>(TCommand Command, CancellationToken cancellationToken) where TCommand : class, ICommand
        {
            CommandExecutionResult result;

            var action = new OnClientCommandReceive<TCommand>(Command, CurrentStateAccessor.Get(), new Actions.Contexts.OnClientCommandReceiveContextDependencies
            {
                EngineConfiguration = EngineConfiguration,
                ClientRequestHandler = RequestHandler,
                LeaderNodePronouncer = LeaderNodePronouncer,
                GlobalAwaiter = GlobalAwaiter,
                PersistentState = PersistentState,
                AppendEntriesManager = AppendEntriesManager
            }, ActivityLogger);

            action.SupportCancellation();

            action.CancellationManager.Bind(cancellationToken);

            try
            {
                result = Responsibilities.QueueBlockingAction<CommandExecutionResult>(
                            action: action,
                            executeSeparately: false
                        );
            }
            catch (Exception e)
            {
                result = new CommandExecutionResult
                {
                    IsSuccessful = false,
                    CommandResult = null,
                    OriginalCommand = EngineConfiguration.IncludeOriginalClientCommandInResults ? Command : null,
                    Exception = e,
                    LeaderNodeConfiguration = LeaderNodePronouncer.RecognizedLeaderConfiguration
                };
            }

            return Task.FromResult(result);
        }
    }
}
