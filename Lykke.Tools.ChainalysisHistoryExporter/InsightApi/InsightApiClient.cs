using System.Threading.Tasks;
using Flurl;
using Flurl.Http;

namespace Lykke.Tools.ChainalysisHistoryExporter.InsightApi
{
    public class InsightApiClient
    {
        private readonly string _url;

        public InsightApiClient(string url)
        {
            _url = url;
        }

        public Task<InsightApiTransactionsResponse> GetAddressTransactions(string address, int page)
        {
            return _url
                .AppendPathSegments("txs")
                .SetQueryParams(new 
                {
                    address = address, 
                    pageNum = page
                })
                .GetJsonAsync<InsightApiTransactionsResponse>();
        }
    }
}
