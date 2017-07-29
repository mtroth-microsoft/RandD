
namespace BuildManager
{
    using System;

    internal sealed class Author
    {
        public Guid id { get; set; }

        public string displayName { get; set; }

        public string uniqueName { get; set; }

        public string url { get; set; }

        public string imageUrl { get; set; }
    }
}
