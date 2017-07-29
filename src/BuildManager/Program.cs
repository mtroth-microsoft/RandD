using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManager
{
    class Program
    {
        static void Main(string[] args)
        {
            // OData.net-master - mtroth";
            // WebApi-master - mtroth";
            // RESTier Master - mtroth";

            List<BuildDefinition> bds = BuildDefinition.Load("OData.net-master - mtroth");
            BuildDefinition bd = BuildDefinition.Load(bds.First().id);
            bd.name = "Test";
            bd.buildNumberFormat = bd.buildNumberFormat.Replace("mtroth", "Test");
            bd.path = bd.path.Replace("mtroth", "Test");
            bd.repository.id = bd.repository.id.Replace("mtroth", "Test");
            bd.repository.name = bd.repository.name.Replace("mtroth", "Test");
            bd.repository.url = bd.repository.url.Replace("mtroth", "Test");
            bd.repository.properties["apiUrl"] = bd.repository.properties["apiUrl"].ToString().Replace("mtroth", "Test");
            bd.repository.properties["branchesUrl"] = bd.repository.properties["branchesUrl"].ToString().Replace("mtroth", "Test");
            bd.repository.properties["cloneUrl"] = bd.repository.properties["cloneUrl"].ToString().Replace("mtroth", "Test");
            bd.repository.properties["refsUrl"] = bd.repository.properties["refsUrl"].ToString().Replace("mtroth", "Test");

            bd.Create();
        }
    }
}
