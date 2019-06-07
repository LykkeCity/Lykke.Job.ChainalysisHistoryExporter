using System;

namespace Lykke.Tools.ChainalysisHistoryExporter.Deposits
{
    public class DepositWallet : IEquatable<DepositWallet>
    {
        public Guid UserId { get; }
        public string Address { get; }
        public string CryptoCurrency { get; }

        public DepositWallet(Guid userId, string address, string cryptoCurrency)
        {
            UserId = userId;
            Address = address;
            CryptoCurrency = cryptoCurrency;
        }

        public bool Equals(DepositWallet other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return UserId.Equals(other.UserId) && string.Equals(Address, other.Address) && string.Equals(CryptoCurrency, other.CryptoCurrency);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DepositWallet) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = UserId.GetHashCode();
                hashCode = (hashCode * 397) ^ (Address != null ? Address.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CryptoCurrency != null ? CryptoCurrency.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
