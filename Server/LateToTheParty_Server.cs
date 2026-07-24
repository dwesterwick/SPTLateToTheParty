using LateToTheParty.Helpers;
using LateToTheParty.Utils;
using LateToTheParty.Utils.ModIntegrityTests;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace LateToTheParty;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader + LateToTheParty_Server.LOAD_ORDER_OFFSET)]
public class LateToTheParty_Server : IOnLoad
{
    public const int LOAD_ORDER_OFFSET = 1;

    private LoggingUtil _loggingUtil;
    private ConfigUtil _configUtil;
    private ModIntegrityTestingUtil _modIntegrityTestingUtil;

    public LateToTheParty_Server(LoggingUtil loggingUtil, ConfigUtil configUtil, ModIntegrityTestingUtil modIntegrityTestingUtil)
    {
        _loggingUtil = loggingUtil;
        _configUtil = configUtil;
        _modIntegrityTestingUtil = modIntegrityTestingUtil;
    }

    public Task OnLoad()
    {
        RunModIntegrityCheck();

        return Task.CompletedTask;
    }

    private void RunModIntegrityCheck()
    {
        _modIntegrityTestingUtil.AddTest<ClientLibraryExistsTest>(_configUtil);

        if (_modIntegrityTestingUtil.RunAllTestsAndVerifyAllPassed())
        {
            return;
        }

        _modIntegrityTestingUtil.LogAllFailureMessages();
        _loggingUtil.Error("Mod integrity check failed. Disabling mod.");

        _configUtil.CurrentConfig.DisableMod();
    }
}
