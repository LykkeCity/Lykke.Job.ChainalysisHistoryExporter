# Lykke.Tools.ChainalysisHistoryExporter

Tool to export Lykke historical data to Chainalysis

# Usage

Add and configure ```appsettings.json``` to the /Lykke.Tools.ChainalysisHistoryExporter:

```json
{
  "AzureStorage": {
    "CashoutProcessorConnString": "<BilCashoutProcessorAzureStorageConnString>",
    "OperationsExecutorConnString": "<BilOperationsExecutorAzureStorageConnString>",
    "BlockchainWalletsConnString": "<BilBlockchainWalletsAzureStorageConnString>",
    "ClientPersonalInfoConnString": "<ClientPersonalInfoAzureStorageConnString>",
    "CashOperationsConnString": "<OperationsRepositoryCashOperationsAzureStorageConnString>",
    "BlockchainWalletsTable": "BlockchainWallets" 
  },
  "DepositWalletProviders": {
    "Providers": [
      "BcnCredentialsDepositWalletsProvider",
      "BilAzureDepositWalletsProvider",
      "WalletCredentialsDepositWalletsProvider"
    ]
  },
  "DepositHistoryProviders": {
    "Providers": [
      "BtcDepositsHistoryProvider",
      "EthDepositsHistoryProvider",
      "LtcDepositsHistoryProvider",
      "BchDepositsHistoryProvider"
    ]
  },
  "WithdrawalHistoryProviders": {
    "Providers": [
      "BilCashoutWithdrawalsHistoryProvider",
      "BilCashoutsBatchWithdrawalsHistoryProvider",
      "CashOperationsWithdrawalsHistoryProvider"
    ]
  },
  "Report": {
    "TransactionsFilePath": "transactions.csv",
    "DepositWalletsFilePath": "deposit-wallets.csv" 
  },
  "Services": {
    "Assets": "<AssetsServiceUrl>"
  },
  "Btc": {
    "Network": "mainnet",
    "NinjaUrl": "<BitcoinNinaUrl>"
  },
  "Eth": {
    "SamuraiUrl": "<EthereumSamuraiUrl>"
  },
  "Ltc": {
    "InsightApiUrl": "<LiteCoinInsightApiUrlWithSuffix>"
  },
  "Bch": {
    "Network": "mainnet",
    "InsightApiUrl": "https://blockdozer.com/insight-api"
  }
}
```

## Blockchain wallets azure/mongo

Blockchain wallets storage is not migrated to Mongo on prod yet, so it's usable only on dev/test env. To use it:

1. Set ```AzureStorage.BlockchainWalletsTable``` to ```BlockchainWalletsObsolete```
2. Add ```"BilMongoDepositWalletsProvider"``` to ```DepositWalletProviders.Providers```
3. Add ```MongoStorage``` section:

```json
  "MongoStorage": {
    "BlockchainWalletsConnString": "<BlockchainWalletsMongoStorageConnString>",
    "BlockchainWalletsDbName": "blockchain-wallets"
  }
```

## BitcoinCash testnet InsightApi 

Use ```https://tbch.blockdozer.com/insight-api``` as testnet InsightAPI for BitcoinCash