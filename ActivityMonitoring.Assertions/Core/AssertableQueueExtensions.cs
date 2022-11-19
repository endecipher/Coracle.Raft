namespace ActivityMonitoring.Assertions.Core
{
    public static class AssertableQueueExtensions
    {
        public static SearchAssertions<TData> Search<TData>(this IAssertableQueue<TData> queue)
        {
            return new SearchAssertions<TData>(queue);
        }

        /// <summary>
        /// Search while discarding entries which did not qualify
        /// </summary>
        /// <typeparam name="TData"></typeparam>
        /// <param name="queue"></param>
        /// <returns></returns>
        public static DigAssertions<TData> Dig<TData>(this IAssertableQueue<TData> queue)
        {
            return new DigAssertions<TData>(queue);
        }


        public static QueuePropertyAssertions<TData> Is<TData>(this IAssertableQueue<TData> queue)
        {
            return new QueuePropertyAssertions<TData>(queue);
        }
    }
}
