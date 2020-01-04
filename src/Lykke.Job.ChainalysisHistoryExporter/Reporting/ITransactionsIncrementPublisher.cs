using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Job.ChainalysisHistoryExporter.Reporting
{
    public interface ITransactionsIncrementPublisher
    {
        Task Publish(HashSet<Transaction> increment, DateTime incrementFrom, DateTime incrementTo);
    }
}
