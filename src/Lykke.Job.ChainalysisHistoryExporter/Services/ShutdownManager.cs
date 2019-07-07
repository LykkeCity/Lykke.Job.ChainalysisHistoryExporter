using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Sdk;

namespace Lykke.Job.ChainalysisHistoryExporter.Services
{
    public class ShutdownManager : IShutdownManager
    {
        private readonly ILog _log;
        private readonly IEnumerable<IStopable> _items;

        public ShutdownManager(
            ILogFactory logFactory, 
            IEnumerable<IStopable> items)
        {
            _log = logFactory.CreateLog(this);
            _items = items;
        }

        public async Task StopAsync()
        {
            foreach (var item in _items)
            {
                try
                {
                    item.Stop();
                }
                catch (Exception ex)
                {
                    _log.Warning($"Unable to stop {item.GetType().Name}", ex);
                }
            }
            
            await Task.CompletedTask;
        }
    }
}
