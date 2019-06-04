using System;

namespace Lykke.Tools.ChainalysisHistoryExporter.Reporting
{
    internal class Transaction : IEquatable<Transaction>
    {
        public string CryptoCurrency { get; }
        public string Hash { get; }
        public Guid UserId { get; }
        public string OutputAddress { get; }
        public TransactionType Type { get; }

        public Transaction(string cryptoCurrency, string hash, Guid userId, string outputAddress, TransactionType type)
        {
            CryptoCurrency = cryptoCurrency;
            Hash = hash;
            UserId = userId;
            OutputAddress = outputAddress;
            Type = type;
        }

        public bool Equals(Transaction other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(CryptoCurrency, other.CryptoCurrency) && string.Equals(Hash, other.Hash) && UserId.Equals(other.UserId) && string.Equals(OutputAddress, other.OutputAddress) && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Transaction) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (CryptoCurrency != null ? CryptoCurrency.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Hash != null ? Hash.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ UserId.GetHashCode();
                hashCode = (hashCode * 397) ^ (OutputAddress != null ? OutputAddress.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) Type;
                return hashCode;
            }
        }
    }
}
