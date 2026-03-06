using Serilog;
using Serilog.Events;

namespace PauloETL.Gui;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Debug)
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(AppContext.BaseDirectory, "PauloETLGui_.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 10,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        Log.CloseAndFlush();
    }
}
