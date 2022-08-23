using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Core.Extensions;
using AElf.Client.Core.Options;
using AElf.Client.Report;
using AElf.Contracts.Report;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler;

internal class ReportProposedLogEventProcessor : LogEventProcessorBase<ReportProposed>
{
    private readonly AElfContractOptions _contractAddressOptions;
    private readonly IReportProvider _reportProvider;
    private readonly IReportService _reportService;
    private readonly IAElfAccountProvider _accountProvider;
    private readonly BridgeOptions _bridgeOptions;
    private readonly AElfClientConfigOptions _aelfClientConfigOptions;
    private readonly AElfChainAliasOptions _aelfChainAliasOptions;

    public override string ContractName => "ReportContract";
    private readonly ILogger<ReportProposedLogEventProcessor> _logger;

    public ReportProposedLogEventProcessor(
        IReportProvider reportProvider,
        IReportService reportService,
        IAElfAccountProvider accountProvider,
        ILogger<ReportProposedLogEventProcessor> logger,
        IOptionsSnapshot<AElfContractOptions> contractAddressOptions,
        IOptionsSnapshot<BridgeOptions> bridgeOptions,
        IOptionsSnapshot<AElfClientConfigOptions> aelfConfigOptions,
        IOptionsSnapshot<AElfChainAliasOptions> aelfChainAliasOptions) : base(contractAddressOptions)
    {
        _logger = logger;
        _contractAddressOptions = contractAddressOptions.Value;
        _reportProvider = reportProvider;
        _reportService = reportService;
        _accountProvider = accountProvider;
        _bridgeOptions = bridgeOptions.Value;
        _aelfClientConfigOptions = aelfConfigOptions.Value;
        _aelfChainAliasOptions = aelfChainAliasOptions.Value;
    }

    public override async Task ProcessAsync(LogEvent logEvent, EventContext context)
    {
        var reportProposed = new ReportProposed();
        reportProposed.MergeFrom(logEvent);

        _logger.LogInformation($"New report: {reportProposed}");
        
        var privateKey = _accountProvider.GetPrivateKey(_aelfClientConfigOptions.AccountAlias);
        
        var sendTxResult = await _reportService.ConfirmReportAsync(_aelfChainAliasOptions.Mapping[context.ChainId.ToString()],new ConfirmReportInput
        {
            Token = reportProposed.Token,
            RoundId = reportProposed.RoundId,
            Signature = SignHelper
                .GetSignature(reportProposed.RawReport, privateKey).RecoverInfo
        });
        _reportProvider.SetReport(reportProposed.Token, reportProposed.RoundId,
            reportProposed.RawReport);
        _logger.LogInformation($"[ConfirmReport] Transaction id ： {sendTxResult.TransactionResult.TransactionId}");
    }
}