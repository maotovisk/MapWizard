namespace MapWizard.CLI.ConsoleUtils;

public static class IoParser
{
    extension(string[] args)
    {
        public bool ArgumentExists(string argumentName)
        {
            return args.Contains(argumentName);
        }

        public string? GetArgumentValue(string argumentName)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == argumentName && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        public string GetArgumentValue(string argumentName, string defaultValue)
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
}