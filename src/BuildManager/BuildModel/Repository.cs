
namespace BuildManager
{
    using System.Collections.Generic;

    internal sealed class Repository
    {
        public string id { get; set; }

        public string type { get; set; }

        public string name { get; set; }

        public string url { get; set; }

        public string defaultBranch { get; set; }

        public bool clean { get; set; }

        public bool checkoutSubmodules { get; set; }

        public Dictionary<string, object> properties { get; set; }
    }
}
