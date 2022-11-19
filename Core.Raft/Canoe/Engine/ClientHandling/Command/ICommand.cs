using System;

namespace Core.Raft.Canoe.Engine.Command
{
    public interface ICommand : IDisposable
    {
        /// <summary>
        /// Our goal for Raft is to implement linearizable semantics (each operation appears to execute instantaneously,
        /// exactly once, at some point between its invocation and
        /// its response). However, as described so far Raft can execute a command multiple times: for example, if the leader
        /// crashes after committing the log entry but before responding to the client, the client will retry the command with a
        /// new leader, causing it to be executed a second time.
        /// 
        /// The solution is for clients to assign unique serial numbers to
        /// every command. Then, the state machine tracks the latest
        /// serial number processed for each client, along with the associated response. If it receives a command whose serial
        /// number has already been executed, it responds immediately without re-executing the request.
        /// <see cref="Section 8 Client Interaction"/>
        /// </summary>
        string UniqueId { get; }

        /// <summary>
        /// Read-only operations can be handled without writing
        /// anything into the log.However, with no additional measures, this would run the risk of returning stale data, since
        /// the leader responding to the request might have been superseded by a newer leader of which it is unaware.Linearizable reads must not return stale data, and Raft needs
        /// two extra precautions to guarantee this without using the
        /// log.First, a leader must have the latest information on
        /// which entries are committed.The Leader Completeness
        /// Property guarantees that a leader has all committed entries, but at the start of its term, it may not know which
        /// those are.To find out, it needs to commit an entry from
        /// its term.Raft handles this by having each leader commit a blank no-op entry into the log at the start of its
        /// term.Second, a leader must check whether it has been deposed before processing a read-only request (its information may be stale if a more recent leader has been elected).
        /// Raft handles this by having the leader exchange heartbeat messages with a majority of the cluster before responding to read-only requests.
        /// 
        /// <see cref="Section 8 Client Interaction"/>
        /// </summary>
        bool IsReadOnly { get; }

        string Type { get; }

        object Data { get; }
    }
}
