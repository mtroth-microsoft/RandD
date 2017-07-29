
namespace BuildManager
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal sealed class BuildDefinition
    {
        private const string Host = "identitydivision.visualstudio.com";

        private const string Project = "OData";

        private const string Token = null;

        private const string DefinitionsQueryTemplate = "https://{0}/DefaultCollection/{1}/_apis/build/definitions?api-version=2.0{2}";

        private const string DefinitionQueryTemplate = "https://{0}/DefaultCollection/{1}/_apis/build/definitions/{2}?api-version=2.0";

        private const string DefinitionPostTemplate = "https://{0}/DefaultCollection/{1}/_apis/build/definitions?api-version=2.0";

        private const string NameOptionTemplate = "&name={0}";

        public int id { get; set; }

        public string name { get; set; }

        public string type { get; set; }

        public string quality { get; set; }

        public string buildNumberFormat { get; set; }

        public string path { get; set; }

        public Queue queue { get; set; }

        public List<Build> build { get; set; }

        public Repository repository { get; set; }

        public List<Option> options { get; set; }

        public Dictionary<string, Variable> variables { get; set; }

        public List<RetentionRule> retentionRules { get; set; }

        public List<Trigger> triggers { get; set; }

        public DateTimeOffset createdDate { get; set; }

        public string jobAuthorizationScope { get; set; }

        public Author authoredBy { get; set; }

        public string uri { get; set; }

        public int revision { get; set; }

        public string url { get; set; }

        public string comment { get; set; }

        public Project project { get; set; }

        public Links _links { get; set; }

        public string detail { get; set; }

        public static List<BuildDefinition> Load(string name)
        {
            string url = string.Format(
                DefinitionsQueryTemplate, 
                Host, Project, name == null ? string.Empty : string.Format(NameOptionTemplate, name));

            string response = WebQueryHelper.GetData(url, Token);
            JObject obj = JObject.Parse(response);

            return JsonConvert.DeserializeObject<List<BuildDefinition>>(obj["value"].ToString());
        }

        public static BuildDefinition Load(int id)
        {
            string url = string.Format(DefinitionQueryTemplate, Host, Project, id);
            string response = WebQueryHelper.GetData(url, Token);

            BuildDefinition instance = JsonConvert.DeserializeObject<BuildDefinition>(response);
            instance.detail = JObject.Parse(response).ToString();

            return instance;
        }

        public void Create()
        {
            string url = string.Format(DefinitionPostTemplate, Host, Project);
            string payload = JsonConvert.SerializeObject(this);
            string response = WebQueryHelper.PostData(url, payload, Token);
        }
    }
}
