namespace Lykke.Job.ChainalysisHistoryExporter.AddressNormalization
{
    public interface IAddressNormalizer
    {
        bool CanNormalize(string cryptoCurrency);

        string NormalizeOrDefault(string address, bool isTransactionNormalization = false);
    }
}
