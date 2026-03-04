using System.Runtime.CompilerServices;
using System.Text;

namespace MapWizard.Tools.HelperExtensions;

public static class MapWizardLogger
{
    private const string LogFileName = "mapwizard.log";
    private static readonly Lock SyncRoot = new();

    public static void LogException(
        Exception ex,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        try
        {
            var dataDirectory = MapWizardPathResolver.ResolveDataDirectoryPath();
            Directory.CreateDirectory(dataDirectory);

            var logPath = Path.Combine(dataDirectory, LogFileName);
            var builder = new StringBuilder();
            builder.Append('[');
            builder.Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
            builder.Append("] ");
            builder.Append(Path.GetFileName(sourceFilePath));
            builder.Append(':');
            builder.Append(lineNumber);
            builder.Append(" (");
            builder.Append(memberName);
            builder.AppendLine(")");
            builder.AppendLine(ex.ToString());
            builder.AppendLine();

            lock (SyncRoot)
            {
                File.AppendAllText(logPath, builder.ToString());
            }
        }
        catch
        {
            // Logging must never throw.
        }
    }
}
