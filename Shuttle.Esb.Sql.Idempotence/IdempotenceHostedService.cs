using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Esb.Sql.Idempotence;

public class IdempotenceHostedService : IHostedService
{
    private readonly IPipelineFactory _pipelineFactory;
    private readonly IdempotenceObserver _idempotenceObserver;

    public IdempotenceHostedService(IPipelineFactory pipelineFactory, IServiceProvider serviceProvider)
    {
        _pipelineFactory = Guard.AgainstNull(pipelineFactory);

        _pipelineFactory.PipelineCreated += PipelineCreated;

        _idempotenceObserver = serviceProvider.GetRequiredService<IdempotenceObserver>();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _pipelineFactory.PipelineCreated -= PipelineCreated;

        await Task.CompletedTask;
    }

    private void PipelineCreated(object? sender, PipelineEventArgs args)
    {
        Guard.AgainstNull(args);

        if (args.Pipeline.GetType() != typeof(StartupPipeline))
        {
            return;
        }

        args.Pipeline.AddObserver(_idempotenceObserver);
    }
}