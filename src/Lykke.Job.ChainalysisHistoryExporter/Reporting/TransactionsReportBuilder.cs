using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Tools.ChainalysisHistoryExporter.Reporting
{
    public class TransactionsReportBuilder
    {
        private readonly TransactionsSnapshotRepository _snapshotRepository;
        private readonly TransactionsIncrementRepository _incrementRepository;
        private readonly HashSet<Transaction> _increment;
        private HashSet<Transaction> _snapshot;
        private bool _incrementSaved;

        public TransactionsReportBuilder(
            TransactionsSnapshotRepository snapshotRepository,
            TransactionsIncrementRepository incrementRepository)
        {
            _snapshotRepository = snapshotRepository;
            _incrementRepository = incrementRepository;
            _increment = new HashSet<Transaction>(262144);
        }

        public async Task LoadSnapshotAsync()
        {
            if (_snapshot != null)
            {
                throw new InvalidOperationException("Report snapshot has been loaded already");
            }

            _snapshot = await _snapshotRepository.LoadAsync();
        }

        public void AddTransaction(Transaction tx)
        {
            if (_snapshot == null)
            {
                throw new InvalidOperationException("Report snapshot has not been loaded yet");
            }
            if (_incrementSaved)
            {
                throw new InvalidOperationException("Report increment has been saved already");
            }

            if (string.IsNullOrWhiteSpace(tx.Hash) ||
                string.IsNullOrWhiteSpace(tx.CryptoCurrency) ||
                tx.UserId == Guid.Empty)
            {
                return;
            }

            lock (_snapshot)
            {
                if (_snapshot.Add(tx))
                {
                    _increment.Add(tx);
                }
            }
        }

        public async Task SaveIncrementAsync()
        {
            if (_snapshot == null)
            {
                throw new InvalidOperationException("Report snapshot has not been loaded yet");
            }
            if (_incrementSaved)
            {
                throw new InvalidOperationException("Report increment has been saved already");
            }

            _incrementSaved = true;

            await _incrementRepository.SaveAsync(_increment);
        }

        public async Task SaveSnapshotAsync()
        {
            if (_snapshot == null)
            {
                throw new InvalidOperationException("Report snapshot has not been loaded yet");
            }
            if (!_incrementSaved)
            {
                throw new InvalidOperationException("Report increment has been not saved yet");
            }

            await _snapshotRepository.SaveAsync(_snapshot);
        }
    }
}
