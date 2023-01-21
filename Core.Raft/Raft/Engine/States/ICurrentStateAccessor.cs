namespace Coracle.Raft.Engine.States
{
    internal interface ICurrentStateAccessor
    {
        IChangingState Get();

        void UpdateWith(IChangingState currentState);
    }

}
