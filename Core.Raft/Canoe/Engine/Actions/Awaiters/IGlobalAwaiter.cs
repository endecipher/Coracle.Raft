using EventGuidance.Responsibilities;
using EventGuidance.Structure;

namespace Core.Raft.Canoe.Engine.Actions.Awaiters
{
    /// <summary>
    /// Used by a method to wait until a cross-component-boundary event for continuation is raised
    /// <para/>
    /// Singleton 
    /// </summary>
    internal interface IGlobalAwaiter
    {
        IConcurrentAwaiter CommandApplication { get; }
        IConcurrentAwaiter SuccessfulAppendEntries { get; }
        IConcurrentAwaiter OngoingLeaderConfirmation { get; }
    }
}