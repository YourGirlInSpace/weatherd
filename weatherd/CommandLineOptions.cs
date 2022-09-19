using CommandLine;

namespace weatherd
{
    public class CommandLineOptions
    {
        [Option('v', "verbose", Required = false, HelpText = "Set log output to verbose.")]
        public bool Verbose { get; set; }

        [Option("cloudwatch", Required = false, HelpText = "Emits logs to Cloudwatch")]
        public bool CloudwatchEnabled { get; set; }
    }
}
