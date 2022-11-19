using System.Threading;

namespace EventGuidance.Cancellation
{
    public interface ICancellationManager
    {
        void Refresh();
        void Bind(CancellationToken token);
        void TriggerCancellation();
        void ThrowIfCancellationRequested();
        CancellationToken CoreToken { get; }
    }
}
