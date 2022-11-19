namespace Core.Raft.Canoe.Engine.States
{
    internal interface ICurrentStateAccessor
    {
        IChangingState Get();

        void UpdateWith(IChangingState currentState);
    }

}
