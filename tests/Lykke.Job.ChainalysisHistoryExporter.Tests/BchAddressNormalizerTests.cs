using Lykke.Job.ChainalysisHistoryExporter.AddressNormalization;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using Moq;
using NBitcoin.Altcoins;
using NUnit.Framework;

namespace Lykke.Job.ChainalysisHistoryExporter.Tests
{
    public class BchAddressNormalizerTests
    {
        private BchAddressNormalizer _normalizer;

        [SetUp]
        public void SetUp()
        {
            Litecoin.Instance.EnsureRegistered();

            var blockchainsProviderMock = new Mock<IBlockchainsProvider>();
            blockchainsProviderMock
                .Setup(x => x.GetBitcoinCash())
                .Returns
                (
                    new Blockchain
                    {
                        CryptoCurrency = "BTC",
                        BilId = "BitcoinCash"
                    }
                );

            _normalizer = new BchAddressNormalizer(blockchainsProviderMock.Object, new BchSettings
            {
                Network = "mainnet"
            });
        }

        [Ignore("Public Insight API doesn't work")]
        [Test]
        [TestCase("qp3wjpa3tjlj042z2wv7hahsldgwhwy0rq9sywjpyy", ExpectedResult = "qp3wjpa3tjlj042z2wv7hahsldgwhwy0rq9sywjpyy")]
        [TestCase("bitcoincash:qp3wjpa3tjlj042z2wv7hahsldgwhwy0rq9sywjpyy", ExpectedResult = "qp3wjpa3tjlj042z2wv7hahsldgwhwy0rq9sywjpyy")]
        [TestCase("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", ExpectedResult = "qp3wjpa3tjlj042z2wv7hahsldgwhwy0rq9sywjpyy")]
        [TestCase("1A1zP1eP5QGefi2DMSLmv7DivfNa", ExpectedResult = null)]
        [TestCase("LW9Tcj39N1f51DHDoue8xWE2cGEE1FKUVF", ExpectedResult = null)]
        public string TestMainNetAddresses(string address)
        {
            return _normalizer.NormalizeOrDefault(address); ;
        }
    }
}
