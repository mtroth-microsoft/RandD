using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FriendsLoader
{
    class Program
    {
        static void Main(string[] args)
        {

            string text = File.ReadAllText(@"F:\Dump\friends_20230112.json");
            DateTimeOffset loadedAt = DateTimeOffset.UtcNow;
            int downloadedAt = 20230112;

            var list = new List<Friend>();
            var friends = (JObject.Parse(text).Children().First() as JProperty).Value as JArray;
            foreach (var node in friends)
            {
                var item = node as JObject;
                string name = item["name"].ToString();
                long timestamp = long.Parse(item["timestamp"].ToString());
                Friend f = new Friend() { Name = name, Timestamp = timestamp, DownloadedAt = downloadedAt, LoadAt = loadedAt, };
                list.Add(f);
            }
        }
    }
}
