using NLog;
using OPCDAClientWrapper;
using OPCDAClientWrapper.Configs;
using OPCDAUtils;

namespace OpcClient1;

internal static class Program
{
    private static void Main(string[] args)
    {
        var logger = LogManager.GetLogger("Test");
        try
        {
            logger.Info("Starting application");
            var client = new OpcClient(logger);
            if (client.Start())
            {
                logger.Info("Connected to OPCServer.");
                logger.Info("Waiting for data from OPCServer. Press any key to stop the application.");
                Console.ReadKey();
            }
            else
            {
                logger.Error("Failed to connect to OPCServer");
            }

            client.Stop();
        }
        catch (System.Exception ex)
        {
            logger.Error(ex, "An error occurred");
        }
        finally
        {
            LogManager.Shutdown();
        }
    }
}
internal class OpcClient : IWorker
{
    private readonly ILogger _logger;
    private readonly IOPCClientWrapper<FakeData> _client;
    public OpcClient(ILogger logger)
    {
        _logger = logger;
        _client = OPCClientWrapperFactory.Create<FakeData>(logger, "Test");
        _client.OPCMessages.OnReceivedAsync += OnDataReceived;
    }

    public OPCConfig Config => throw new NotImplementedException();

    public bool Start(CancellationToken cancellationToken = default)
    {
        return StartAsync(cancellationToken).AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        var isOK = _client.LoadConfigAndProcessInit("tags.json");
        if (isOK) isOK = _client.TagsConfigValidate(() => _logger.Info("entry validation."), () => _logger.Info("exit validation."));
        if (isOK)
        {
            isOK = await Task.Run(() => _client.ConnectToOPCServer(), cancellationToken).ConfigureAwait(false);
        }
        if (isOK)
        {
            _client.CyclicRoutinesInit();
        }
        return isOK && _client.IsReady;
    }

    public void Stop()
    {
        if (_client != null)
        {
            _client.OPCMessages.OnReceivedAsync -= OnDataReceived;
            _client.Dispose();
        }
    }

    public ValueTask WriteOPCGroup(Group group, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private Task OnDataReceived(object sender, OPCDAClientWrapper.Events.DataReceivedEventArgs<global::OPCDAUtils.Group> eventArg)
    {
        if (eventArg.Data != null && eventArg.Data.Tags?.Count > 0)
        {
            _logger.Info("OPC Data received");
            foreach (var tag in eventArg.Data.Tags)
            {
                _logger.Info("Tag: {0} Value: {1}", tag.Name, tag.Value);
            }
        }
        return Task.CompletedTask;
    }
}
internal class FakeData { }