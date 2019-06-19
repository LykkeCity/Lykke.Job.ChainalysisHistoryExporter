using System;
using System.Collections.Generic;

namespace ReportSnapshotUploader
{
    public sealed class Settings
    {
        public static readonly IDictionary<string, string> CommandLineMappings = new Dictionary<string, string>
        {
            {"-cs", nameof(AzureConnectionString)},
            {"-f", nameof(FilePath)}
        };

        public string AzureConnectionString { get; set; }
        public string FilePath { get; set; }
        
        public bool Validate()
        {
            if (AzureConnectionString == null)
            {
                Console.WriteLine($"'{nameof(AzureConnectionString)}' or command line '-cs' parameter is not specified");
                return false;
            }

            if (FilePath == null)
            {
                Console.WriteLine($"'{nameof(FilePath)}' or command line '-f' parameter is not specified");
                return false;
            }

            return true;
        }
    }
}
