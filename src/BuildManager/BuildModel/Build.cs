
namespace BuildManager
{
    using System.Collections.Generic;

    internal sealed class Build
    {
        public bool enabled { get; set; }

        public bool continueOnError { get; set; }

        public bool alwaysRun { get; set; }

        public string displayName { get; set; }

        public Task task { get; set; }

        public Dictionary<string, string> inputs { get; set; }
    }
}
