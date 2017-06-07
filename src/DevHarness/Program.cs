using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace DevHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            Uri root = new Uri("http://server/");
            Uri url = new Uri("/Entities?$compute=cast(Prop1, 'Edm.String') as Property1AsString, tolower(Prop1) as Property1Lower", UriKind.Relative);

            EdmModel model = new EdmModel();
            EdmEntityType elementType = model.AddEntityType("DevHarness", "Entity");
            
            EdmTypeReference typeReference = new EdmStringTypeReference(EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.String), false);
            EdmProperty property = new EdmStructuralProperty(elementType, "Prop1", typeReference);
            elementType.AddProperty(property);
            EdmEntityContainer container = model.AddEntityContainer("Default", "Container");
            container.AddEntitySet("Entities", elementType);

            ODataUriParser parser = new ODataUriParser(model, root, url);
            ComputeClause clause = parser.ParseCompute();
        }
    }
}
