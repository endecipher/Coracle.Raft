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

using Coracle.Raft.Engine.Snapshots;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.Command
{
    public interface IStateMachineHandler
    {
        /// <summary>
        /// Determines whether a command of id <paramref name="uniqueCommandId"/> has been executed recently
        /// </summary>
        /// <param name="uniqueCommandId">Unique Id of the command</param>
        /// <returns>A wrapper <see cref="CommandExecutionResult"/> over the command result, and whether a <see cref="ICommand"/> having <see cref="ICommand.UniqueId"/> as <paramref name="uniqueCommandId"/> was executed recently</returns>
        Task<(bool IsExecutedAndLatest, CommandExecutionResult CommandResult)> IsExecutedAndLatest(string uniqueCommandId);

        /// <summary>
        /// Extension to check if the Client Command was already executed previously and return the results of the executed command
        /// </summary>
        /// <typeparam name="TCommand">External Command Implementation Type</typeparam>
        /// <param name="command">Command object</param>
        /// <returns>Whether the <paramref name="command"/> was executed in the state machine, and a wrapper <see cref="CommandExecutionResult"/> over the command results</returns>
        Task<(bool IsExecuted, CommandExecutionResult CommandResult)> TryGetResult<TCommand>(TCommand command) where TCommand : class, ICommand;

        /// <summary>
        /// Actual Execution of Command via a persisted Log Entry during the Commit Index update
        /// </summary>
        /// <param name="logEntryCommand">Command to execute and apply towards the state machine</param>
        Task ExecuteAndApply(ICommand logEntryCommand);

        /// <summary>
        /// Reset state machine and rebuild entire state from the snapshot <paramref name="snapshotDetail"/>. 
        /// Implementation can be asynchronous for a quick response. 
        /// </summary>
        /// <param name="snapshotDetail">Snapshot Header from which the state should be built</param>
        Task ForceRebuildFromSnapshot(ISnapshotHeader snapshotDetail);
    }

    /// <remarks>
    /// If the leader
    /// crashes after committing the log entry but before responding to the client, the client will retry the command with a
    /// new leader, causing it to be executed a second time.The
    /// solution is for clients to assign unique serial numbers to
    /// every command.Then, the state machine tracks the latest
    /// serial number processed for each client, along with the associated response. If it receives a command whose serial
    /// number has already been executed, it responds immediately without re-executing the request.
    /// 
    /// <seealso cref="Section 8 Client Interaction"/>
    /// </remarks>
    /// 

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
}
