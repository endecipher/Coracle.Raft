using Core.Raft.Canoe.Engine.States;

namespace Core.Raft.Canoe.Engine.Node
{
    /// <summary>
    /// Governs the associated Canoe Node's internal state 
    /// </summary>
    internal interface ICanoeNodeState
    {
        /// <summary>
        /// Always holds the current/ongoing <see cref="AbstractState"/>.
        /// Is <see cref="null"/> before first state initialization.
        /// </summary>
        internal IChangingState State { get; set; }
    }
}
