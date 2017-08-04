
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
        public BuildDefinition()
        {
            this.triggers = new List<Trigger>();
        }

        private const string Host = "identitydivision.visualstudio.com";

        private const string Project = "OData";

        private const string Token = "5ohltg2g6zic7gvxkm4o7j5we7x5wqduszwxpgzyvnm4kqedk6uq";

        private const string DefinitionsQueryTemplate = "https://{0}/DefaultCollection/{1}/_apis/build/definitions?api-version=2.0{2}";

        private const string DefinitionQueryTemplate = "https://{0}/DefaultCollection/{1}/_apis/build/definitions/{2}?api-version=2.0";

        private const string DefinitionPostTemplate = "https://{0}/DefaultCollection/{1}/_apis/build/definitions?api-version=2.0";

        private const string DefinitionPutTemplate = "https://{0}/DefaultCollection/{1}/_apis/build/definitions/{2}?api-version=2.0";

        private const string BuildPostTemplate = "https://{0}/DefaultCollection/{1}/_apis/build/builds?api-version=2.0";

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

        //public List<RetentionRule> retentionRules { get; set; }

        //public ProcessParameters processParameters { get; set; }

        public List<Trigger> triggers { get; set; }

        //public DateTimeOffset createdDate { get; set; }

        //public string jobAuthorizationScope { get; set; }

        //public int jobTimeoutInMinutes { get; set; }

        //public int jobCancelTimeoutInMinutes { get; set; }

        //public Author authoredBy { get; set; }

        //public string uri { get; set; }

        //public int revision { get; set; }

        //public string url { get; set; }

        public string comment { get; set; }

        //public Project project { get; set; }

        //public Links _links { get; set; }

        //public string detail { get; set; }

        public List<string> demands { get; set; }

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

            return instance;
        }

        public BuildDefinition Create()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings() { Formatting = Formatting.Indented };
            string url = string.Format(DefinitionPostTemplate, Host, Project);
            string payload = JsonConvert.SerializeObject(this, settings);
            string response = WebQueryHelper.PostData(url, payload, Token);

            return JsonConvert.DeserializeObject<BuildDefinition>(response);
        }

        public BuildDefinition Update()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings() { Formatting = Formatting.Indented };
            string url = string.Format(DefinitionPutTemplate, Host, Project, this.id);
            string payload = JsonConvert.SerializeObject(this, settings);
            string response = WebQueryHelper.PutData(url, payload, Token);

            return JsonConvert.DeserializeObject<BuildDefinition>(response);
        }

        public void QueueBuild(string branch)
        {
            QueueBuild qb = new QueueBuild();
            qb.definition = new BuildDefinitionId() { id = this.id };
            qb.sourceBranch = branch;
            qb.parameters = "{\"BuildConfiguration\":\"release\"}";
            string url = string.Format(BuildPostTemplate, Host, Project);
            string payload = JsonConvert.SerializeObject(qb);
            string response = WebQueryHelper.PostData(url, payload, Token);
        }
    }
}
