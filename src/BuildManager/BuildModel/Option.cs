
namespace BuildManager
{
    using System.Collections.Generic;

    internal sealed class Option
    {
        public bool enabled { get; set; }

        public Definition definition { get; set; }

        public Dictionary<string, string> inputs { get; set; }
    }
}
