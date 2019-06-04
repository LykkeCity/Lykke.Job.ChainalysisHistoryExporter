using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lykke.Tools.ChainalysisHistoryExporter.Common
{
    internal static class PaginatedList
    {
        public static PaginatedList<TItem> From<TItem, TContinuation>(TContinuation continuationToken, IReadOnlyCollection<TItem> items)
        {
            var continuation = continuationToken != null
                ? JsonConvert.SerializeObject(continuationToken)
                : null;

            return new PaginatedList<TItem>(continuation, items);
        }
    }

    internal class PaginatedList<T>
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