using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositsHistoryProviders.Ripple
{
    /// <summary>
    /// JSON-RPC request with parameters.
    /// </summary>
    /// <typeparam name="TParams">Type of parameters.</typeparam>
    public abstract class RippleRequest<TParams> where TParams : class
    {
        protected RippleRequest(string method, TParams parameters)
        {
            Method = method ?? throw new ArgumentNullException(nameof(method));
            Params = new TParams[] { parameters ?? throw new ArgumentNullException(nameof(parameters)) };
        }

        [JsonProperty("method")]
        public string Method { get; }

        [JsonProperty("params")]
        public TParams[] Params { get; }
    }

    /// <summary>
    /// JSON-RPC request without parameters.
    /// </summary>
    public abstract class RippleRequest : RippleRequest<object>
    {
        protected RippleRequest(string method) : base(method, new { })
        {
        }
    }
}
