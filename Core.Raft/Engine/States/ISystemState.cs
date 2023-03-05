namespace Coracle.Raft.Engine.States
{
    internal interface ISystemState
    {
        void Stop();
        void Resume();
        void Decommission();
    }
}
