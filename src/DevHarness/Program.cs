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
            Uri url = new Uri("/Entities?$compute=cast(Prop1, 'Edm.String') as Property1AsString, tolower(Prop1) as Property1Lower&$expand=Nav1($compute=cast(Prop1, 'Edm.String') as NavProperty1AsString)", UriKind.Relative);

            EdmModel model = new EdmModel();
            EdmEntityType elementType = model.AddEntityType("DevHarness", "Entity");
            EdmEntityType targetType = model.AddEntityType("DevHarness", "Navigation");
            
            EdmTypeReference typeReference = new EdmStringTypeReference(EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.String), false);
            elementType.AddProperty(new EdmStructuralProperty(elementType, "Prop1", typeReference));
            targetType.AddProperty(new EdmStructuralProperty(targetType, "Prop1", typeReference));

            EdmNavigationPropertyInfo propertyInfo = new EdmNavigationPropertyInfo();
            propertyInfo.Name = "Nav1";
            propertyInfo.Target = targetType;
            propertyInfo.TargetMultiplicity = EdmMultiplicity.One;
            EdmProperty navigation = EdmNavigationProperty.CreateNavigationProperty(elementType, propertyInfo);
            elementType.AddProperty(navigation);

            EdmEntityContainer container = model.AddEntityContainer("Default", "Container");
            container.AddEntitySet("Entities", elementType);

            ODataUriParser parser = new ODataUriParser(model, root, url);
            ComputeClause clause = parser.ParseCompute();
            SelectExpandClause se = parser.ParseSelectAndExpand();
        }
    }
}
