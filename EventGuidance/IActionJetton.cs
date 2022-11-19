using EventGuidance.Structure;
using System;

namespace EventGuidance.Responsibilities
{
    public interface IActionJetton
    {
        bool IsBlocking { get; set; }
        IEventAction EventAction { get; }
        string EventKey { get; }
        Exception Exception { get; set; }
        bool HasCanceled { get; }
        bool HasCompleted { get; }
        bool HasFaulted { get; }
        bool HasTimedOut { get; }
        bool IsProcessing { get; }
        object Result { set; }
        T GetResult<T>() where T : class;
        void FreeBlockingResources();
        void MoveToCancelled();
        void MoveToCompleted();
        void MoveToFaulted();
        void MoveToProcessing();
        void MoveToReady();
        void MoveToStopped();
        void MoveToTimeOut();
        void SetResultIfAny<T>(T result, Exception exception = null) where T : class;
        void Dispose();
    }
}
