namespace Core.Raft.Canoe.Engine.States
{
    internal interface ISystemState
    {
        void Pause();
        void Resume();
        void Decomission();
    }
}
