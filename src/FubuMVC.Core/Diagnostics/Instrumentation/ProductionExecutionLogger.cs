using System.Collections.Generic;
using FubuMVC.Core.ServiceBus.Runtime;

namespace FubuMVC.Core.Diagnostics.Instrumentation
{
    public class ProductionExecutionLogger : IExecutionLogger
    {
        private readonly IExecutionLogStorage _storage;

        public ProductionExecutionLogger(IExecutionLogStorage storage)
        {
            _storage = storage;
        }

        public void Record(IChainExecutionLog log, IDictionary<string, object> http)
        {
            log.RecordHeaders(http);
            _storage.Store(log);
        }

        public void Record(IChainExecutionLog log, Envelope envelope)
        {
            log.RecordHeaders(envelope);

            _storage.Store(log);
        }
    }
}