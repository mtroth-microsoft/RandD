
namespace BuildManager
{
    using System;

    internal sealed class Project
    {
        public Guid id { get; set; }

        public string name { get; set; }

        public string description { get; set; }

        public string url { get; set; }

        public string state { get; set; }

        public int revision { get; set; }
    }
}
