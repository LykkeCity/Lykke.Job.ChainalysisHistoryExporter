using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Job.ChainalysisHistoryExporter.Reporting
{
    public interface ITransactionsIncrementRepository
    {
        Task SaveAsync(HashSet<Transaction> increment, DateTime incrementFrom, DateTime incrementTo);
    }
}
