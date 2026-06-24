using FpsGodPc.Services;

var progress = new Progress<string>(Console.WriteLine);
var service = new SpaceBattleBenchmarkService(new PresentMonService(new ProcessRunner()));
var (success, message) = await service.EnsureInstalledAsync(progress);
Console.WriteLine(success ? "INSTALL_OK" : "INSTALL_FAIL");
Console.WriteLine(message);
Environment.Exit(success ? 0 : 1);
