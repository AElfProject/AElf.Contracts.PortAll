using System.Threading.Tasks;
using AElf.Contracts.OracleUser;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Boilerplate.EventHandler
{
    public class QueryDataRecordedLogEventProcessor : LogEventProcessorBase, ITransientDependency
    {
        private readonly ILogger<QueryDataRecordedLogEventProcessor> _logger;

        public QueryDataRecordedLogEventProcessor(IOptionsSnapshot<ContractAddressOptions> contractAddressOptions,
            ILogger<QueryDataRecordedLogEventProcessor> logger) : base(contractAddressOptions)
        {
            _logger = logger;
        }

        public override string ContractName => "OracleUser";
        public override string LogEventName => nameof(QueryDataRecorded);

        public override Task ProcessAsync(LogEvent logEvent)
        {
            var queryDataRecorded = new QueryDataRecorded();
            queryDataRecorded.MergeFrom(logEvent);
            _logger.LogInformation($"[Callback] Query data recorded: {queryDataRecorded}");

            return Task.CompletedTask;
        }
    }
}