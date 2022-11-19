namespace Core.Raft.Canoe.Engine.States.LeaderState
{
    /// <summary>
    /// OKAY HEAR ME OUT.
    /// 
    /// Do we really need this class? Because, once the AppendEntries RPC Responses have returned, after updating the Match and Next Index,
    /// The commit Index dependency changes, and thus we trigger the Leaders Commit Index update Method. That should update the Commit Index.
    /// 
    /// Also, once updated, the rules for all Servers would be subscribed to the commit Index. If that changes, then that means the log application 
    /// to the state machine will occur. 
    /// 
    /// And now, if log application does occur, that means, that any threads which have been waiting for the client command state machine result to be returned, 
    /// they can now stop waiting and return with the result.
    /// 
    /// We can plug a Command Timeout, in case the log Entry has not been replicated to a majority within some time (or) some other issues.
    /// 
    /// Now, think of what happens if we keep on requesting Responsibilities to be added with AppendEntries RPC to the same node again and again.
    /// In that case, for every AppendEntries RPC launched, we can issue a retry counter (Not infinite, but a high number)
    /// But even then, we may be queueing up a lot of AppendEntries RPC Requests for that same node.
    /// 
    /// And really if you think about it, SESION GUID DOES NOT MATTER. Since everything is idempotent, they should naturally fail/update the indices.
    /// No matter if that request was 20 minutes back, or a new one issued. Instead of just retrying indefinetly, we can just put a stop of the stale AppendEntries RPC being requeued.
    /// For that we can keep a Node Level track of every session like a Least Recently Added Cache. If the retrying RPC Event Action doesn't find it's session GUID in the cache, then no requeueing. 
    /// 
    /// For now, we need a good way for client commands to be subscribed to the changes of state machine applications through the command guid.
    /// </summary>
    /// 

    internal struct AppendEntriesSession
    {
        public string UniqueSessionId { get; internal set; }
        public bool FromCommand { get; internal set; }
    }
}
