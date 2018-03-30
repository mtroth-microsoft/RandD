
namespace BuildManager
{
    internal sealed class QueueBuild
    {
        public BuildDefinitionId definition { get; set; }

        public string sourceBranch { get; set; }

        public string parameters { get; set; }
    }
}
