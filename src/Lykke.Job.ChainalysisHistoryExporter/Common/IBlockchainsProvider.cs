namespace Lykke.Job.ChainalysisHistoryExporter.Common
{
    public interface IBlockchainsProvider
    {
        Blockchain GetByBilIdOrDefault(string bilId);
        Blockchain GetBitcoin();
        Blockchain GetEthereum();
        Blockchain GetLiteCoin();
        Blockchain GetBitcoinCash();
        Blockchain GetByAssetIdOrDefault(string assetId);
        Blockchain GuessBlockchainOrDefault(string assetReference);
    }
}