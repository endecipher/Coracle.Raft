using System;

namespace Core.Raft
{
    public class Brainstorming
    {
        /*
         *  Server Initialized => TimeCalculator
         * 
    When a leader state gets initialized:
        - NextIndex[] for each server, index of the next log entry to send to that server (initialized to leader last log index + 1)
        - MatchIndex[] for each server, index of highest log entry known to be replicated on server (initialized to 0, increases monotonically
        - It appends a no-op entry in the Log (thus increasing last Log Index)
        - DateTimeOffset? LastAppendEntriesRPCInvokedFromLeader = DateTime.MinValue;
        - Create HeartBeatTimer set to dueTime as the interval

        3 scenarios 
            - A) Leader just got elected
            - A) Leader is idle (No client request received)
            - B) Client Request Received

        A) Leader Just Got Elected:
            - If leader just got elected, then we would have had appended a no-op entry in the Log, the condition lastLogIndex >= nextIndex[] for a server satisfies
            - Since this condition satisfied, it will trigger the below
                - LastAppendEntriesRPCInvokedFromLeader = DateTime.Now; (object locked)
                - We send AppendEntries RPC parallely with log entries such as List<LogEntry>[nextIndex, lastLogIndex] to each peer
                ** - Whatever entries we are sending, the prevlogIndex and prevLogTerm should also be included in the request
                    - If response is successful for a given server's RPC:
                        - This means that server is up-to-date with leader's logs.
                        - Update nextIndex = the lastLogIndex + 1 and matchIndex = lastLogIndex for that server.
                    - If AppendEntriesResponse for a server fails because of log inconsistency and we see Success as false,
                        - Decrement nextIndex for that server
                        - Queue another AppendEntriesRPC only to that server with the inclusion of the previous logs also based on [nextIndex, lastLogIndex]
                        - This queuing can happen as we decerement nextIndex[] everytime, so this can go on for a bit, while other servers can parallely respond 
                    - If AppendEntriesRPC Times out
                        - We form a new AppendEntriesRPC
                        - We queue that RPC call to be performed
                        - Retry indefinitely
                - If we receive parallelly, successful responses from a majority of servers,
                    - We find that the log has been replicated to a majority successfully
                    - We finally update our own (thisNode leader) CommitIndex
                        - This triggers an internal action to be performed which does the following:
                            - If commitIndex > lastApplied: increment lastApplied, apply log[lastApplied] to state machine
                - Parallely, on all servers, the above would have fired independently
        
        B) Leader Idle:
            - We need a way to cut down network calls for heartbeats if it just executed a previous RPC session
            - A Leader can have a periodic Heartbeat Timer which invokes a check to see when was the last Parallel AppendEntries RPC registered
                - If the LastAppendEntriesRPCInvokedFromLeader (object locked) was invoked a short time back we can change the Heartbeat Timer to the difference of the interval times so that it can do the check again.
                - If the LastAppendEntriesRPCInvokedFromLeader (object locked) was a while back, then it's time to send a heartbeat to all
                    - Proceeding with Heartbeat, (THERE IS NO MAJORITY DEPENDENCY)
                    - LastAppendEntriesRPCInvokedFromLeader = DateTime.Now
                    - We send AppendEntries RPC parallely with no log entries parallely. 
                    - If we receive responses which are successful
                        - This means that server is up-to-date with leader's logs.
                        - Update nextIndex = the lastLogIndex + 1 and matchIndex = lastLogIndex for that server.
                    - If we receive rejected responses (fails because of log inconsistency and we see Success as false)
                        - Decrement nextIndex for that server
                        - Queue another AppendEntriesRPC only to that server with the inclusion of the previous logs also based on [nextIndex, lastLogIndex]. 
                            - Whatever entries we are sending, the prevlogIndex and prevLogTerm should also be included in the request
                        - This queuing can happen as we decrement nextIndex[] everytime, so this can go on for a bit but we do not need to keep track of other responses 
                        - If AppendEntriesRPC Times out
                            - We form a new AppendEntriesRPC
                            - We queue that RPC call to be performed
                            - Retry indefinitely
                    - We do not have any motive to keep track of successful majority responses. 
                    - The followers who eventually get updated with the leader's logs would trigger an internal action to be performed which does the following:
                                - If commitIndex > lastApplied: increment lastApplied, apply log[lastApplied] to state machine
                                    - Parallely, on all servers, the above would have fired independently
        
        B) Client Request Received:
            - If leader receives a client request, we first check the client Command unique Guid
                - We ask the Replicated Log Holder to check if there exists a Command with the GUID
                    - If it doesn't then it's a new Write request and thus we can go ahead and proceed
                    - If it matches we ask the ClientExecutor to fetch the response for that entry and immediately return it to the client.
            - Proceeding, we append the new Write entry in the Log (last Log Index gets incremented), the condition lastLogIndex >= nextIndex[] for a server satisfies
            - LastAppendEntriesRPCInvokedFromLeader = DateTime.Now
            - We send AppendEntries RPC parallely with log entries such as List<LogEntry>[nextIndex, lastLogIndex] to each peer
                - If response is successful for a given server's RPC:
                    - This means that server is up-to-date with leader's logs.
                    - Update nextIndex = the lastLogIndex + 1 and matchIndex = lastLogIndex for that server.
                - If AppendEntriesResponse for a server fails because of log inconsistency and we see Success as false,
                    - Decrement nextIndex for that server
                    - Queue another AppendEntriesRPC only to that server with the inclusion of the previous logs also based on [nextIndex, lastLogIndex]
                    - This queuing can happen as we decrement nextIndex[] everytime, so this can go on for a bit, while other servers can parallely respond 
                - If AppendEntriesRPC Times out
                    - We form a new AppendEntriesRPC
                    - We queue that RPC call to be performed
                    - Retry indefinitely
            - If we receive parallelly, successful responses from a majority of servers,
                - We find that the log has been replicated to a majority successfully
            - But how do we know the above? 
                - If there exists an N such that N > commitIndex, a majority of matchIndex[i] ≥ N, and log[N].term == currentTerm:
                    - Set commitIndex = N (§5.3, §5.4).
            - From the above we can see that whenever the MatchIndex get's updated we should fire an internal action to check if the above condition satisfies,
                - Thus, 
                    if there exists a LogIndex > commitIndex (The entry has not been committed yet by the leader) &&
                    a majority of matchIndex[i] >= N (The logIndex has been replicated to a majority of the followers) &&
                    logEntries.GetValueAtIndex(N).Term == currentTerm (And the Entry is from the CurrentTerm)
                        - We finally update our own (thisNode leader) CommitIndex
                            - Set commitIndex = N
                    - This triggers an internal action to be performed which does the following:
                        - If commitIndex > lastApplied: increment lastApplied, apply log[lastApplied] to state machine
            - Parallely, on all servers, the above would have fired independently

        Additional Notes:
        - There might be a case where Heartbeats and AppendEntries RPC get queued up for a server for a long time (server might have network failure/fault)
            - The global task pipeline executor might get filled with multiple outgoing requests which are being again retried and queued after timing out
            - In that case, the old requests which are being queued up again will be stale.
            - So in this case, if we have a max retry counter and if the the parallel requests had a InvocationTime, we can co-ordinate:
                - If current_retries > max retry counter && LastAppendEntriesRPCInvokedFromLeader is more recent than EventAction.InvocationTime,
                    - Don't queue this request anymore
                - Else
                    - Queue it up again
        - We need a way to track Broadcast Time, since broadCastTime (Time between sending request and receiving response) << Election Timeout << MTBF
            - Whenever a RPC gets sent out from a server, we should track how much time the entire networking call took.
            - So, for every RPC response, let the server handle volatile information to capture this metric
                - Each response can contain a TimeSpan that it took from request-till-response. 
                - OnReceive can implement a quick functionality to calculate/log averages by sending necessary info to a TimeCalculator object
        - Retries should only be queued when a TimeoutException is thrown.
            - Whenever a retry is scheduled, necessary info should be sent to the TimeCalculator so that it can keep track of MTBF
            - On any cancellation exception/faults it should be thrown

        - Understand the significance of this rule:
            - If there exists an N such that N > commitIndex, a majority of matchIndex[i] ≥ N, and log[N].term == currentTerm:
                - Set commitIndex = N (§5.3, §5.4).


         * 
         * 
         * 
         * 
         * 
         *   
         *   //When a fellow Server sends the RPC call to this Server
         *   OnReceiveAppendEntriesRPC: //This event requires an Output of AppendEntriesReponse
1. Reply false if term < currentTerm (§5.1)  - Action   OnRejectEntriesRPC_LowerTerm
2. Reply false if log doesn’t contain an entry at prevLogIndex 
whose term matches prevLogTerm (§5.3) - Action  OnRejectEntriesRPC_LogMismatch
3. If an existing entry conflicts with a new one (same index
but different terms), delete the existing entry and all that
follow it (§5.3) - Action OnEntriesRPC_Overwrite
4. Append any new entries not already in the log - Action OnEntriesRPC_AppendMerge
5. If leaderCommit > commitIndex, set commitIndex =
min(leaderCommit, index of last new entry) - Action - Action OnEntriesRPC_UpdateCommitIndex
         *   
         *  
         *  
         *  OnReceiveRequestVoteRPC 
1. Reply false if term < currentTerm (§5.1) - Action OnVoteRPC_DenyLowerTerm
2. If votedFor is null or candidateId, and candidate’s log is at
least as up-to-date as receiver’s log, grant vote (§5.2, §5.4) - OnVoteRPC_Grant
         *  
         *  
         *  
         *  
         *  
         *  
________________
All Servers:
 
        OnDefaultBehavior
• If commitIndex > lastApplied: increment lastApplied, apply
log[lastApplied] to state machine (§5.3) - OnDefaultBehavior_ApplyLog
• If RPC request or response contains term T > currentTerm:
set currentTerm = T, convert to follower (§5.1)  - If any RPC (AppendEntries/RequestVote) Request incoming, or outgoing request's response,
        Then - OnDefaultBehavior_DowngradeToFollowerDueToLowerTerm

________________
Followers (§5.2):
• Respond to RPCs from candidates and leaders -- ??
• If election timeout elapses without receiving AppendEntries
RPC from current leader or granting vote to candidate:
convert to candidate --  OnElectionTimeout  -> Fire CancellationToken to Cancel OnGoing Actions.  ElectionTimer.RegisterTriggerOnTimeout(() => Responsilities[].Perform()) 

Candidates (§5.2):

        OnHostingElection
• On conversion to candidate, start election:
    • Increment currentTerm - OnElection_IncrementTerm
    • Vote for self - OnElection_VoteForSelf
    • Reset election timer - OnElection_ResetTimer
    • Send RequestVote RPCs to all other servers - OnElection_RequestVotes
• If votes received from majority of servers: become leader  OnVoteCollectionUpgradeToLeader
• If AppendEntries RPC received from new leader: convert to follower - OnRecognizedLeaderDowngradeToFollower - followed by calling OnReceiveAppendEntriesRPC 
• If election timeout elapses: start new election -> Fire new thingy.


Leaders:  -Tinput = LeaderState

       Just on Leader State gets intialized -> Register Background Thread to send heartbeats

        OnLeaderAppointmentFireHeartbeats

• Upon election: send initial empty AppendEntries RPCs
(heartbeat) to each server; -- repeat during idle periods to
prevent election timeouts (§5.2) 
            
        SendHeartbeats()
        {
            Configuration.Servers.ParallelForEach(x=> {
               new HeartbeatEventAction<State, AppendEntriesRPCResponse>    
            })
        }


• If command received from client: append entry to local log,
respond after entry applied to state machine (§5.3)  --        
        OnClientCommandReceived -> This should Trigger OnDefaultBehavior_ApplyLog and Apply the State Machine Command and update a boolean Flag UpToDate
        -> If the flag is true, then finally respond


            

• If last log index ≥ nextIndex for a follower: send
AppendEntries RPC with log entries starting at nextIndex
    • If successful: update nextIndex and matchIndex for
    follower (§5.3)
    • If AppendEntries fails because of log inconsistency:
    decrement nextIndex and retry (§5.3)
• If there exists an N such that N > commitIndex, a majority
of matchIndex[i] ≥ N, and log[N].term == currentTerm:
set commitIndex = N (§5.3, §5.4).





        *   public static FetchTypeKey<TInput, TOutput>() => return nameof(TInput).Concat(nameof(TOutput));
        *
        *   EventKeyMap = new ConcurrentDictionary<string, (TInput, TOutput, IEventAction)>
        *   
        *   await Responsibilities.Instance.QueueWorkItems<TInput, TOutput>();
        *   
        *   
        *   async Task<TOutput> QueueWorkItem<TInput, TOutput>(EventKeyString key, TInput Input, bool isSeparate = false){
        *   
        *       
        *   
        *       EventAction<TInput,TOutput> actionToBeScheduled = EventKeyMap[key] as EventAction<TInput,TOutput>;
        *       
        *       if(!isSeparate)
        *           actionToBeScheduled.BindCancellationSource(this.CancellationManager.Source);
        *       
        *       EventScheduler.Add(someString, () => {
        *           
        *           var EventResult = new v
        *           actionToBeScheduled.Perform()
        *       }.ContinueWith(() => {
        *           EventResultMap.Add(key, new EventResult {
        *               
        *               Type = typeof(TOutput)
        *               
        *           }
        *       }))
        *       
        *       return await EventScheduler.TryFetch(key);
        *   }
        *   
        *
        *   EventScheduler {
        *   
        *       async Task<TOutput> TryFetch(string key){
        *           
        *           new SpinWaitLock(TimeSpan(Timeout));
        *           EventResultMap.TryGetValue(key, out EventResult value)
        *           return EventResult.OfType<TOutput>;
        *       }
        *   
        *   
        *   }
        *
        *
        */
    }
}
