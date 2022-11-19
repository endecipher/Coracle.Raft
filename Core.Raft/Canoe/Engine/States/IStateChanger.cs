namespace Core.Raft.Canoe.Engine.States
{
    internal interface IStateChanger
    {
        void Initialize();
        void AbandonStateAndConvertTo<T>(string typename) where T : IChangingState, new();
    }
}
