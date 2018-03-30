
namespace BuildManager
{
    using System.Collections.Generic;
    using System.Linq;

    class Program
    {
        static void Main(string[] args)
        {
            // OData.net-master - mtroth";
            // WebApi-master - mtroth";
            // RESTier Master - mtroth";
            string source = "mtroth-microsoft";
            string target = "lensgrinder";
            string name = "OData.net-master - mtroth";
            string newName = "OData.net-master - " + target;

            BuildDefinition created = BuildDefinition.Load(newName).SingleOrDefault();
            if (created == null)
            {
                List<BuildDefinition> bds = BuildDefinition.Load(name);
                BuildDefinition bd = BuildDefinition.Load(bds.First().id);

                bd.name = newName;
                bd.buildNumberFormat = newName.Replace("-", "_").Replace(" ", string.Empty) + "_$(date:yyyyMMdd)$(rev:.r)";
                bd.path = bd.path.Replace(source, "Dynamic");
                bd.repository.id = bd.repository.id.Replace(source, target);
                bd.repository.name = bd.repository.name.Replace(source, target);
                bd.repository.url = bd.repository.url.Replace(source, target);
                bd.repository.properties["apiUrl"] = bd.repository.properties["apiUrl"].ToString().Replace(source, target);
                bd.repository.properties["branchesUrl"] = bd.repository.properties["branchesUrl"].ToString().Replace(source, target);
                bd.repository.properties["cloneUrl"] = bd.repository.properties["cloneUrl"].ToString().Replace(source, target);
                bd.repository.properties["refsUrl"] = bd.repository.properties["refsUrl"].ToString().Replace(source, target);

                created = bd.Create();
            }
            else
            {
                created = BuildDefinition.Load(created.id);
            }

            created.QueueBuild("master");
        }
    }
}
