
namespace ActivityMonitoring.Assertions.Core
{
    public interface INotifier<TData>
    {
        Func<TData, bool> Condition { get; init; }
        bool IsConditionMatched { get; }
        void Wait();
        void Wait(TimeSpan timeSpan, CancellationToken cancellationToken);
        INotifier<TData> RemoveOnceMatched();
    }
}