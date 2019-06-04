using System;

namespace Lykke.Tools.ChainalysisHistoryExporter.Reporting
{
    internal class Transaction
    {
        public string CryptoCurrency { get; set; }
        public string Hash { get; set; }
        public Guid UserId { get; set; }
        public string OutputAddress { get; set; }
        public TransactionType Type { get; set; }
    }
}
