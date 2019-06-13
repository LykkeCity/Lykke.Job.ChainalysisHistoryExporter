namespace Lykke.Tools.ChainalysisHistoryExporter.AddressNormalization
{
    public interface IAddressNormalizer
    {
        bool CanNormalize(string cryptoCurrency);

        string NormalizeOrDefault(string address);
    }
}
