using System.Threading;

namespace EventGuidance.Responsibilities
{
    public interface IEventProcessor
    {
        void StartProcessing(CancellationToken token);
        void Stop();
        void Enqueue(IActionJetton info);
    }
}
