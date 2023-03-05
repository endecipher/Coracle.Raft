namespace Coracle.Raft.Engine.States
{
    internal interface ICurrentStateAccessor
    {
        IStateDevelopment Get();

        void UpdateWith(IStateDevelopment currentState);
    }

}
