using EventGuidance.Responsibilities;
using EventGuidance.Structure;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Core.Raft.Canoe.Engine.Actions.Awaiters
{
    internal class GlobalAwaiter : IGlobalAwaiter
    {
        public GlobalAwaiter(IConcurrentAwaiter awaitACommandApplication, IConcurrentAwaiter awaitSuccessfulAppendEntries, IConcurrentAwaiter awaitStillLeader)
        {
            CommandApplication = awaitACommandApplication;
            SuccessfulAppendEntries = awaitSuccessfulAppendEntries;
            OngoingLeaderConfirmation = awaitStillLeader;
        }

        public IConcurrentAwaiter CommandApplication { get; }

        /// <summary>
        /// Currently there are no references for Waits - //TODO, post all testing, this property can be removed
        /// </summary>
        public IConcurrentAwaiter SuccessfulAppendEntries { get; }

        /// <summary>
        /// 
        /// </summary>
        public IConcurrentAwaiter OngoingLeaderConfirmation { get; }
    }

    //internal class GlobalAwaiting : IGlobalAwaiter
    //{
    //    public GlobalAwaiting(IActionLock awaitACommandApplication, IActionLock awaitSuccessfulAppendEntries, IActionLock awaitStillLeader)
    //    {
    //        AwaitACommandApplication = awaitACommandApplication;
    //        AwaitSuccessfulAppendEntries = awaitSuccessfulAppendEntries;
    //        AwaitOngoingLeaderConfirmation = awaitStillLeader;
    //    }

    //    public IActionLock AwaitACommandApplication { get; }
    //    public IActionLock AwaitSuccessfulAppendEntries { get; }
    //    public IActionLock AwaitOngoingLeaderConfirmation { get; }



    //    public void WaitForACommandToBeApplied()
    //    {
    //    }
    //}

    
}
