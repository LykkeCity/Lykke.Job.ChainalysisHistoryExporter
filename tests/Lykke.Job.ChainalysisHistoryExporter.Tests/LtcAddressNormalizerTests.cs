using Lykke.Job.ChainalysisHistoryExporter.AddressNormalization;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using Moq;
using NBitcoin.Altcoins;
using NUnit.Framework;

namespace Lykke.Job.ChainalysisHistoryExporter.Tests
{
    public class LtcAddressNormalizerTests
    {
        private LtcAddressNormalizer _normalizer;

        [SetUp]
        public void SetUp()
        {
            Litecoin.Instance.EnsureRegistered();

            var blockchainsProviderMock = new Mock<IBlockchainsProvider>();
            blockchainsProviderMock
                .Setup(x => x.GetLiteCoin())
                .Returns
                (
                    new Blockchain
                    {
                        CryptoCurrency = "LTC",
                        BilId = "LiteCoin"
                    }
                );

            _normalizer = new LtcAddressNormalizer(blockchainsProviderMock.Object, new LtcSettings
            {
                Network = "ltc-main"
            });
        }

        [Test]
        [TestCase("LW9Tcj39N1f51DHDoue8xWE2cGEE1FKUVF")]
        public void TestValidMainNetAddresses(string address)
        {
            // Act

            var result = _normalizer.NormalizeOrDefault(address);

            // Assert

            Assert.AreEqual(address, result);
        }

        [Test]
        [TestCase("QNCn9mHykUebdDRncKsuJeGwoBhMusS6p8")]
        public void TestInvalidMainNetAddresses(string address)
        {
            // Act

            var result = _normalizer.NormalizeOrDefault(address);

            // Assert

            Assert.IsNull(result);
        }
    }
}
