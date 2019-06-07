using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lykke.Tools.ChainalysisHistoryExporter.Common
{
    public static class PaginatedList
    {
        public static PaginatedList<TItem> From<TItem, TContinuation>(TContinuation continuationToken, IReadOnlyCollection<TItem> items)
        {
            var continuation = continuationToken != null
                ? JsonConvert.SerializeObject(continuationToken)
                : null;

            return new PaginatedList<TItem>(continuation, items);
        }

        public static PaginatedList<TItem> From<TItem>(IReadOnlyCollection<TItem> items)
        {
            return new PaginatedList<TItem>(null, items);
        }
    }

    public class PaginatedList<T>
    {
        public string Continuation { get; }
        public IReadOnlyCollection<T> Items { get; }

        public PaginatedList(string continuation, IReadOnlyCollection<T> items)
        {
            Continuation = continuation;
            Items = items;
        }
    }
}
