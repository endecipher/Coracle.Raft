namespace Coracle.Raft.Engine.States
{
    internal interface ISystemState
    {
        void Pause();
        void Resume();
        void Decomission();
    }
}
