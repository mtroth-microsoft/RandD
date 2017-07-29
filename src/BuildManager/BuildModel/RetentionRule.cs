
namespace BuildManager
{
    using System.Collections.Generic;

    internal sealed class RetentionRule
    {
        public List<string> branches { get; set; }

        public List<string> artificats { get; set; }

        public List<string> artifactTypesToDelete { get; set; }

        public int daysToKeep { get; set; }

        public int minimumToKeep { get; set; }

        public bool deleteBuildRecord { get; set; }

        public bool deleteTestResults { get; set; }
    }
}
