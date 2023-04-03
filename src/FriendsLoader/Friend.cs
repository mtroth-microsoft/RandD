using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FriendsLoader
{
    class Friend
    {
        public string Name { get; set; }

        public long Timestamp { get; set; }

        public DateTimeOffset LoadAt { get; set; }

        public int DownloadedAt { get; set; }
    }
}
