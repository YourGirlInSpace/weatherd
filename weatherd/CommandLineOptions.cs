﻿using CommandLine;

namespace weatherd
{
    public class CommandLineOptions
    {
        [Option('v', "verbose", Required = false, HelpText = "Set log output to verbose.")]
        public bool Verbose { get; set; }

        [Option("cloudwatch", Required = false, HelpText = "Emits logs to Cloudwatch")]
        public bool CloudwatchEnabled { get; set; }

        [Option("showpakbus", Required = false, HelpText = "Shows the current contents of the Inlocs table in Pakbus")]
        public bool ShowPakbus { get; set; }
    }
}
