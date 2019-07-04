using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Newtonsoft.Json;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositHistoryProviders.Ethereum
{
    public class SamuraiClient
    {
        private readonly string _url;

        public SamuraiClient(string url)
        {
            _url = url;
        }

        public async Task<PaginatedList<SamuraiOperation>> GetOperationsHistoryAsync(string address, string continuation)
        {
            var continuationToken = continuation != null
                ? JsonConvert.DeserializeObject<SamuraiContinuationToken>(continuation)
                : new SamuraiContinuationToken {Start = 0};

            var response = await _url
                .AppendPathSegments("api", "AddressHistory", address)
                .SetQueryParams(new
                {
                    Count = 1000, 
                    Start = continuationToken.Start
                })
                .GetJsonAsync<SamuraiOperationsHistoryResponse>();

            var nextStart = response.History.Length + continuationToken.Start;
            var resultContinuationToken = response.History.Length < 1000 
                ? null 
                : new SamuraiContinuationToken{Start = nextStart};

            return PaginatedList.From(resultContinuationToken, response.History);
        }

        public async Task<PaginatedList<SamuraiErc20Operation>> GetErc20OperationsHistory(string address, string continuation)
        {
            var continuationToken = continuation != null
                ? JsonConvert.DeserializeObject<SamuraiContinuationToken>(continuation)
                : new SamuraiContinuationToken {Start = 0};

            var response = await _url
                .AppendPathSegments("api", "Erc20TransferHistory", "getErc20Transfers", "v2")
                .SetQueryParams(new {count = 1000, start = continuationToken.Start})
                .PostJsonAsync(new {assetHolder = address});

            var responseContent = await response.Content.ReadAsStringAsync();
            var operations = JsonConvert.DeserializeObject<SamuraiErc20Operation[]>(responseContent);

            var nextStart = operations.Length + continuationToken.Start;
            var resultContinuationToken = operations.Length < 1000 
                ? null 
                : new SamuraiContinuationToken{Start = nextStart};

            return PaginatedList.From(resultContinuationToken, operations);
        }
    }
}
