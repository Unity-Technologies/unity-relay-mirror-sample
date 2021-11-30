using System.Collections.Generic;

namespace Unity.Helpers.ServerQuery.Utils
{
    public class CommandLine
    {
        public bool TryGetValueFromCommandLine(string flag, out int value)
        {
            var args = GetCommandLineArgs();
            if (args.TryGetValue($"-{flag}", out var myflag))
            {
                return int.TryParse(myflag, out value);
            }
            
            value = default;
            return false;
        }
        
        public bool TryGetValueFromCommandLine(string flag, out string value)
        {
            var args = GetCommandLineArgs();
            if (args.TryGetValue($"-{flag}", out var myflag))
            {
                value = myflag;
                return true;
            }
            
            value = default;
            return false;
        }
        
        private Dictionary<string, string> GetCommandLineArgs()
        {
            Dictionary<string, string> argumentDictionary = new Dictionary<string, string>();

            var commandLineArgs = System.Environment.GetCommandLineArgs();

            for (int argumentIndex = 0; argumentIndex < commandLineArgs.Length; ++argumentIndex)
            {
                var arg = commandLineArgs[argumentIndex].ToLower();
                if (arg.StartsWith("-"))
                {
                    var value = argumentIndex < commandLineArgs.Length - 1 ? 
                        commandLineArgs[argumentIndex + 1].ToLower() : null;
                    value = (value?.StartsWith("-") ?? false) ? null : value;

                    argumentDictionary.Add(arg, value);
                }
            }
            return argumentDictionary;
        }
    }
}