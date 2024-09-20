namespace MapWizard.CLI.ConsoleUtils;

public class IoParser
{
    public static bool ArgumentExists(string[] args, string argumentName)
    {
        return args.Contains(argumentName);
    }
    
    public static string GetArgumentValue(string[] args, string argumentName)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == argumentName && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }

        return null;
    }
    
    public static string GetArgumentValue(string[] args, string argumentName, string defaultValue)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == argumentName && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }

        return defaultValue;
    }
}