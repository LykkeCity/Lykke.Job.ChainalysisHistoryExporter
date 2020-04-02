using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.AddressNormalization;
using Lykke.Job.ChainalysisHistoryExporter.Assets;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Deposits;
using Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositsHistoryProviders.Bitcoin;
using Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositsHistoryProviders.BitcoinCash;
using Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositsHistoryProviders.Ethereum;
using Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositsHistoryProviders.LiteCoin;
using Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositsHistoryProviders.Ripple;
using Lykke.Job.ChainalysisHistoryExporter.Reporting;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Microsoft.Extensions.Caching.Memory;
using NBitcoin.Altcoins;
using NUnit.Framework;

namespace Lykke.Job.ChainalysisHistoryExporter.Tests
{
    public class DepositsHistoryProvidersTests
    {
        private AddressNormalizer _addressNormalizer;
        private IBlockchainsProvider _blockchainProvider;
        private ILogFactory _logFactory;

        [SetUp]
        public void Setup()
        {
            BCash.Instance.EnsureRegistered();
            Litecoin.Instance.EnsureRegistered();

            _logFactory = LogFactory.Create().AddUnbufferedConsole();
            var assetsClient = new AssetsClient(_logFactory, new AssetsClientSettings());
            _blockchainProvider = new BlockchainsProvider(assetsClient);
            _addressNormalizer = new AddressNormalizer
            (
                _logFactory,
                new IAddressNormalizer[]
                {
                    new GeneralAddressNormalizer(),
                    new BtcAddressNormalizer(_blockchainProvider, new BtcSettings {Network = "mainnet"}),
                    new BchAddressNormalizer(_blockchainProvider, new BchSettings {Network = "mainnet"}),
                    new LtcAddressNormalizer(_blockchainProvider, new LtcSettings {Network = "ltc-main"}),
                    new EthAddressNormalizer(_blockchainProvider),
                    new XrpAddressNormalizer(_blockchainProvider)
                }
            );
        }

        [TearDown]
        public void Teardown()
        {
            _logFactory?.Dispose();
        }

        [Test]
        public async Task TestBtcDepositsHistory()
        {
            // Arrange

            var historyProvider = new BtcDepositsHistoryProvider
            (
                _blockchainProvider,
                new BtcSettings
                {
                    Network = "main",
                    NinjaUrl = "http://api.qbit.ninja"
                }
            );
            var wallet = new DepositWallet
            (
                Guid.NewGuid(),
                _addressNormalizer.NormalizeOrDefault("3N7cHrmKeEsjuFTx39WyGoZwAikAVSFoWX", "BTC"),
                "BTC"
            );

            // Act

            var transactions = await historyProvider.GetHistoryAsync(wallet, null);

            Assert.IsNull(transactions.Continuation, "Test should be modified to support continuation");

            var inputTransaction = transactions.Items.SingleOrDefault(x => x.Hash == "d8f1de74975ff7966c72944b75ac7ec307f58b42019e2206845c78b2f16326ba");
            var coloredInputTransaction1 = transactions.Items.SingleOrDefault(x => x.Hash == "15f04a503a203a7a043d1126750028f3c805624fec38353280df23c85283df04");
            var coloredInputTransaction2 = transactions.Items.SingleOrDefault(x => x.Hash == "c09d8d502d15ce54e61c14a97ef5a59a8ada20c269ac6ef2a065dd615f04247a");
            var outputTransaction1 = transactions.Items.SingleOrDefault(x => x.Hash == "8c3f45a126a9558d393250aa58ca2b8b4b428c948b1ec837578ef1e13f81f6bf");
            var outputTransaction2 = transactions.Items.SingleOrDefault(x => x.Hash == "cde67afceff4f7a4437b520c74f9200d4079f078754be617eafdcece835e9d9c");
            var outputTransaction3 = transactions.Items.SingleOrDefault(x => x.Hash == "a4e793c395fc958cd87769a75411584da1517b6b6e48d7e160a6f19057ede853");
            var outputTransaction4 = transactions.Items.SingleOrDefault(x => x.Hash == "1363846a07e44774ebd9c29f5033894492273b96dca873aab63065064fe00c06");
            var outputTransaction5 = transactions.Items.SingleOrDefault(x => x.Hash == "12971723f3ebc31744693e7db4ba5fc9131c9b446c55a910ac7431b74bb91387");
            var outputTransaction6 = transactions.Items.SingleOrDefault(x => x.Hash == "cdf73195df0b8ec119153c80b29d05944117711ce7570d70e5bcd681522c9521");
            
            // Assert

            Assert.IsNotNull(inputTransaction);
            Assert.IsNotNull(coloredInputTransaction1);
            Assert.IsNotNull(coloredInputTransaction2);
            Assert.IsNull(outputTransaction1);
            Assert.IsNull(outputTransaction2);
            Assert.IsNull(outputTransaction3);
            Assert.IsNull(outputTransaction4);
            Assert.IsNull(outputTransaction5);
            Assert.IsNull(outputTransaction6);
            
            Assert.AreEqual("BTC", inputTransaction.CryptoCurrency);
            Assert.AreEqual(wallet.Address, inputTransaction.OutputAddress);
            Assert.AreEqual(wallet.UserId, inputTransaction.UserId);
            Assert.AreEqual(TransactionType.Deposit, inputTransaction.Type);

            Assert.AreEqual("BTC", coloredInputTransaction1.CryptoCurrency);
            Assert.AreEqual(wallet.Address, coloredInputTransaction1.OutputAddress);
            Assert.AreEqual(wallet.UserId, coloredInputTransaction1.UserId);
            Assert.AreEqual(TransactionType.Deposit, coloredInputTransaction1.Type);

            Assert.AreEqual("BTC", coloredInputTransaction2.CryptoCurrency);
            Assert.AreEqual(wallet.Address, coloredInputTransaction2.OutputAddress);
            Assert.AreEqual(wallet.UserId, coloredInputTransaction2.UserId);
            Assert.AreEqual(TransactionType.Deposit, coloredInputTransaction2.Type);
        }

        [Test]
        public async Task TestLtcDepositsHistory()
        {
            // Arrange

            var historyProvider = new LtcDepositsHistoryProvider
            (
                _logFactory,
                new LtcSettings
                {
                    Network = "ltc-main",
                    InsightApiUrl = "https://insight.litecore.io/api"
                },
                _blockchainProvider,
                _addressNormalizer
            );
            var wallet = new DepositWallet
            (
                Guid.NewGuid(),
                _addressNormalizer.NormalizeOrDefault("MJ1yiB1YLQFra7teEnsYhHbCXtg7Z5cXER", "LTC"),
                "LTC"
            );

            // Act

            var transactions = await historyProvider.GetHistoryAsync(wallet, null);

            Assert.IsNull(transactions.Continuation, "Test should be modified to support continuation");

            var inputTransaction1 = transactions.Items.SingleOrDefault(x => x.Hash == "0d7b0f981c5c2eec3f3894684ac376d09d35316a0e2b92613aa8447e43267782");
            var inputTransaction2 = transactions.Items.SingleOrDefault(x => x.Hash == "185dd76bdddbedd755fa3f1f8964900cc58091fc59b55eadc9182964e8bca4d0");
            var inputTransaction3 = transactions.Items.SingleOrDefault(x => x.Hash == "03e525835881d46dcd57afab4cf1a0bbf4393dbe5ffb9d5b71ad22585cfeb1d5");
            var outputTransaction1 = transactions.Items.SingleOrDefault(x => x.Hash == "ea56e88b637fc3a3eb51c74d7009c5c2d123ec45a646bf4bc18da4000c968739");
            var outputTransaction2 = transactions.Items.SingleOrDefault(x => x.Hash == "736182ece2301dbf6201a8990a8c22f2984bc1e8e83cb8fddb58b0f9ff278397");
            var outputTransaction3 = transactions.Items.SingleOrDefault(x => x.Hash == "23414816382e805390263fabe3eeafd708d4321d8b2084419f26b81020ec8367");

            // Assert

            Assert.IsNotNull(inputTransaction1);
            Assert.IsNotNull(inputTransaction2);
            Assert.IsNotNull(inputTransaction3);
            Assert.IsNull(outputTransaction1);
            Assert.IsNull(outputTransaction2);
            Assert.IsNull(outputTransaction3);

            Assert.AreEqual("LTC", inputTransaction1.CryptoCurrency);
            Assert.AreEqual(wallet.Address, inputTransaction1.OutputAddress);
            Assert.AreEqual(wallet.UserId, inputTransaction1.UserId);
            Assert.AreEqual(TransactionType.Deposit, inputTransaction1.Type);

            Assert.AreEqual("LTC", inputTransaction2.CryptoCurrency);
            Assert.AreEqual(wallet.Address, inputTransaction2.OutputAddress);
            Assert.AreEqual(wallet.UserId, inputTransaction2.UserId);
            Assert.AreEqual(TransactionType.Deposit, inputTransaction2.Type);

            Assert.AreEqual("LTC", inputTransaction3.CryptoCurrency);
            Assert.AreEqual(wallet.Address, inputTransaction3.OutputAddress);
            Assert.AreEqual(wallet.UserId, inputTransaction3.UserId);
            Assert.AreEqual(TransactionType.Deposit, inputTransaction3.Type);
        }

        [Test]
        [Ignore("Public Insight API doesn't work")]
        public async Task TestBchDepositsHistory()
        {
            // Arrange

            var historyProvider = new BchDepositsHistoryProvider
            (
                _logFactory,
                new BchSettings
                {
                    Network = "main",
                    InsightApiUrl = "https://blockdozer.com/insight-api"
                },
                _blockchainProvider,
                _addressNormalizer
            );
            var wallet = new DepositWallet
            (
                Guid.NewGuid(),
                _addressNormalizer.NormalizeOrDefault("qrvvjf9an22vv4wumm2enzdee7xna659kggarhwzyl", "BCH"),
                "BCH"
            );

            // Act

            var transactions = await historyProvider.GetHistoryAsync(wallet, null);

            Assert.IsNull(transactions.Continuation, "Test should be modified to support continuation");

            var inputTransaction = transactions.Items.SingleOrDefault(x => x.Hash == "ca5119c2a07ef74e66fc25f5eb2501fede8eb04b1174c0e27b4e0093b705acfc");
            var outputTransaction = transactions.Items.SingleOrDefault(x => x.Hash == "e9510d71e44749c9456fe2040c8ae5e276698dfd834c1c0c1329a85506cc87ac");

            // Assert

            Assert.IsNotNull(inputTransaction);
            Assert.IsNull(outputTransaction);

            Assert.AreEqual("BCH", inputTransaction.CryptoCurrency);
            Assert.AreEqual(wallet.Address, inputTransaction.OutputAddress);
            Assert.AreEqual(wallet.UserId, inputTransaction.UserId);
            Assert.AreEqual(TransactionType.Deposit, inputTransaction.Type);
        }

        [Test]
        [Ignore("Mainnet Samurai is not accessible")]
        public async Task TestEthDepositsHistoryForErc20()
        {
            // Arrange

            var historyProvider = new EthDepositsHistoryProvider
            (
                _blockchainProvider,
                new EthSettings
                {
                    SamuraiUrl = "http://144.76.25.187:8004"
                },
                _addressNormalizer
            );
            var wallet = new DepositWallet
            (
                Guid.NewGuid(),
                _addressNormalizer.NormalizeOrDefault("0x27e017534031cf15f6b207c635362d720eef8908", "ETH"),
                "ETH"
            );

            // Act

            var transactions = await historyProvider.GetHistoryAsync(wallet, null);

            Assert.IsNull(transactions.Continuation, "Test should be modified to support continuation");

            var inputTransaction = transactions.Items.SingleOrDefault(x => x.Hash == "0x7b6d2c134d12d23c4bdd89cea50fd8693a119a42d0afcc3975725d830eeb01c8");
            var outputTransaction = transactions.Items.SingleOrDefault(x => x.Hash == "0x7dbedaf6d657dffd3bfe6423d74e8e8b5821eaf519fee68fecc96884d0398d09");

            // Assert

            Assert.IsNotNull(inputTransaction);
            Assert.IsNull(outputTransaction);

            Assert.AreEqual("ETH", inputTransaction.CryptoCurrency);
            Assert.AreEqual(wallet.Address, inputTransaction.OutputAddress);
            Assert.AreEqual(wallet.UserId, inputTransaction.UserId);
            Assert.AreEqual(TransactionType.Deposit, inputTransaction.Type);
        }

        [Test]
        [Ignore("Mainnet Samurai is not accessible")]
        public async Task TestEthDepositsHistoryForEther()
        {
            // Arrange

            var historyProvider = new EthDepositsHistoryProvider
            (
                _blockchainProvider,
                new EthSettings
                {
                    SamuraiUrl = "http://144.76.25.187:8004"
                },
                _addressNormalizer
            );
            var wallet = new DepositWallet
            (
                Guid.NewGuid(),
                _addressNormalizer.NormalizeOrDefault("0xff9d0186e9bd8234cfefbae286c2f0321b0d760d", "ETH"),
                "ETH"
            );

            // Act

            var transactions = await historyProvider.GetHistoryAsync(wallet, null);

            Assert.IsNull(transactions.Continuation, "Test should be modified to support continuation");

            var inputTransaction1 = transactions.Items.SingleOrDefault(x => x.Hash == "0x605a3d3338fad48e8ca7597acad718afe56479776baf95568586c5c65674e5ae");
            var inputTransaction2 = transactions.Items.SingleOrDefault(x => x.Hash == "0x7c36ca2cf156982000f4fb3e9df2cf20539367e92b16b909f7a98333f4f52843");
            var inputTransaction3 = transactions.Items.SingleOrDefault(x => x.Hash == "0x20a52fbd3eefe39beb4576b2591430b28e43a3b906d324d99a501aff1cade8d8");
            var inputTransaction4 = transactions.Items.SingleOrDefault(x => x.Hash == "0xad0ffe618b4ddb58f552361f32dc671f48c9a11a1095b66520ecb33f3ec7d7fe");
            var inputTransaction5 = transactions.Items.SingleOrDefault(x => x.Hash == "0x302c023c77d7800cda7424a98adcd08e9c419c92d46ec00ed767bab02d212cc3");
            var outputTransaction1 = transactions.Items.SingleOrDefault(x => x.Hash == "0x6ba8e3f37c412914bee3b6eb1fdad7097ebf167499941d6b36100ec84cd714ea");
            var outputTransaction2 = transactions.Items.SingleOrDefault(x => x.Hash == "0x401a611b4324454814fd33fe3a2808068821eca67b275810c9d916fbc3237fb9");
            var outputTransaction3 = transactions.Items.SingleOrDefault(x => x.Hash == "0xa312466e6ad6266f1c44c135684e8dba64380186966f14b838aa2c755262cb4b");

            // Assert

            Assert.IsNotNull(inputTransaction1);
            Assert.IsNotNull(inputTransaction2);
            Assert.IsNotNull(inputTransaction3);
            Assert.IsNotNull(inputTransaction4);
            Assert.IsNotNull(inputTransaction5);
            Assert.IsNull(outputTransaction1);
            Assert.IsNull(outputTransaction2);
            Assert.IsNull(outputTransaction3);

            Assert.AreEqual("ETH", inputTransaction1.CryptoCurrency);
            Assert.AreEqual(wallet.Address, inputTransaction1.OutputAddress);
            Assert.AreEqual(wallet.UserId, inputTransaction1.UserId);
            Assert.AreEqual(TransactionType.Deposit, inputTransaction1.Type);

            Assert.AreEqual("ETH", inputTransaction2.CryptoCurrency);
            Assert.AreEqual(wallet.Address, inputTransaction2.OutputAddress);
            Assert.AreEqual(wallet.UserId, inputTransaction2.UserId);
            Assert.AreEqual(TransactionType.Deposit, inputTransaction2.Type);

            Assert.AreEqual("ETH", inputTransaction3.CryptoCurrency);
            Assert.AreEqual(wallet.Address, inputTransaction3.OutputAddress);
            Assert.AreEqual(wallet.UserId, inputTransaction3.UserId);
            Assert.AreEqual(TransactionType.Deposit, inputTransaction3.Type);

            Assert.AreEqual("ETH", inputTransaction4.CryptoCurrency);
            Assert.AreEqual(wallet.Address, inputTransaction4.OutputAddress);
            Assert.AreEqual(wallet.UserId, inputTransaction4.UserId);
            Assert.AreEqual(TransactionType.Deposit, inputTransaction4.Type);

            Assert.AreEqual("ETH", inputTransaction5.CryptoCurrency);
            Assert.AreEqual(wallet.Address, inputTransaction5.OutputAddress);
            Assert.AreEqual(wallet.UserId, inputTransaction5.UserId);
            Assert.AreEqual(TransactionType.Deposit, inputTransaction5.Type);
        }

        [Test]
        public async Task TestXrpDepositsHistoryWithTag()
        {
            // Arrange

            var historyProvider = new XrpDepositsHistoryProvider
            (
                _blockchainProvider,
                new XrpSettings
                {
                    RpcUrl = "http://s.altnet.rippletest.net:51234"
                }
            );
            
            var wallet = new DepositWallet
            (
                Guid.NewGuid(),
                "rBdTcycW4Q29RCojiQiQ3tAHNJiTsiqNu2+1188615693",
                "XRP"
            );

            // Act

            var transactions = await historyProvider.GetHistoryAsync(wallet, null);

            var inputTransaction1 = transactions.Items.SingleOrDefault(x => x.Hash == "2ECB3C49152C3E95CB11BCCE59FDEA96200696B9B529E13891DDC3F115EF55AB");
            var inputTransaction2 = transactions.Items.SingleOrDefault(x => x.Hash == "8812C6CAFBE7C37C449D0C64673371BEAE26500276648FE6634BE87FCA9AFDF9");
            var outputTransaction1 = transactions.Items.SingleOrDefault(x => x.Hash == "B5E4D68E8F0C9C440BF1C4040CB303546A0534EE173AB372B3F149ADCA59AB0D");


            // Assert

            Assert.IsNotNull(inputTransaction1);
            Assert.IsNull(inputTransaction2);
            Assert.IsNull(outputTransaction1);

            Assert.AreEqual("XRP", inputTransaction1.CryptoCurrency);
            Assert.IsTrue(wallet.Address.StartsWith(inputTransaction1.OutputAddress));
            Assert.AreEqual(wallet.UserId, inputTransaction1.UserId);
            Assert.AreEqual(TransactionType.Deposit, inputTransaction1.Type);
        }

        [Test]
        public async Task TestXrpDepositsHistoryWithoutTag()
        {
            // Arrange

            var historyProvider = new XrpDepositsHistoryProvider
            (
                _blockchainProvider,
                new XrpSettings
                {
                    RpcUrl = "http://s.altnet.rippletest.net:51234"
                }
            );

            var wallet = new DepositWallet
            (
                Guid.NewGuid(),
                "rBdTcycW4Q29RCojiQiQ3tAHNJiTsiqNu2",
                "XRP"
            );

            // Act

            var transactions = await historyProvider.GetHistoryAsync(wallet, null);

            var inputTransaction1 = transactions.Items.SingleOrDefault(x => x.Hash == "2ECB3C49152C3E95CB11BCCE59FDEA96200696B9B529E13891DDC3F115EF55AB");
            var inputTransaction2 = transactions.Items.SingleOrDefault(x => x.Hash == "8812C6CAFBE7C37C449D0C64673371BEAE26500276648FE6634BE87FCA9AFDF9");
            var outputTransaction1 = transactions.Items.SingleOrDefault(x => x.Hash == "B5E4D68E8F0C9C440BF1C4040CB303546A0534EE173AB372B3F149ADCA59AB0D");


            // Assert

            Assert.IsNotNull(inputTransaction1);
            Assert.IsNotNull(inputTransaction2);
            Assert.IsNull(outputTransaction1);

            Assert.AreEqual("XRP", inputTransaction1.CryptoCurrency);
            Assert.AreEqual(wallet.Address, inputTransaction1.OutputAddress);
            Assert.AreEqual(wallet.UserId, inputTransaction1.UserId);
            Assert.AreEqual(TransactionType.Deposit, inputTransaction1.Type);

            Assert.AreEqual("XRP", inputTransaction2.CryptoCurrency);
            Assert.AreEqual(wallet.Address, inputTransaction2.OutputAddress);
            Assert.AreEqual(wallet.UserId, inputTransaction2.UserId);
            Assert.AreEqual(TransactionType.Deposit, inputTransaction2.Type);
        }
    }
}
