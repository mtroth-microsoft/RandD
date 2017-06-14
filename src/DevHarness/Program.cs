﻿using System;
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
            Uri url = new Uri("http://server/Entities?$select=Prop1/Edm.String&$filter=Prop2/Edm.String eq 'sdb'&$orderby=Prop2/Edm.String");
            url = new Uri("http://server/Entities?$filter=cast(Prop2, Edm.String) eq 'sdb'");

            EdmModel model = new EdmModel();
            EdmEntityType elementType = model.AddEntityType("DevHarness", "Entity");
            EdmComplexType complexType = model.AddComplexType("DevHarness", "Complex", null, true);
            EdmComplexType derivedType = model.AddComplexType("DevHarness", "Derived", complexType, false);

            EdmComplexTypeReference complexTypeReference = new EdmComplexTypeReference(complexType, false);
            EdmTypeReference typeReference = new EdmStringTypeReference(EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.String), false);
            elementType.AddProperty(new EdmStructuralProperty(elementType, "Prop1", typeReference));
            elementType.AddProperty(new EdmStructuralProperty(elementType, "Prop2", typeReference));
            elementType.AddProperty(new EdmStructuralProperty(elementType, "Prop3", complexTypeReference));
            derivedType.AddProperty(new EdmStructuralProperty(derivedType, "Prop4", typeReference));

            EdmEntityContainer container = model.AddEntityContainer("Default", "Container");
            container.AddEntitySet("Entities", elementType);

            ODataUriParser parser = new ODataUriParser(model, root, url);
            //OrderByClause orderby = parser.ParseOrderBy();
            //SelectExpandClause select = parser.ParseSelectAndExpand();
            FilterClause filter = parser.ParseFilter();
        }

        private static void TestCompute()
        {
            Uri root = new Uri("http://server/");
            Uri url = new Uri("/Entities?$compute=cast(Prop1, 'Edm.String') as Property1AsString, tolower(Prop1) as Property1Lower&$expand=Nav1($compute=cast(Prop1, 'Edm.String') as NavProperty1AsString;$expand=SubNav1($compute=cast(Prop1, 'Edm.String') as SubNavProperty1AsString))", UriKind.Relative);

            EdmModel model = new EdmModel();
            EdmEntityType elementType = model.AddEntityType("DevHarness", "Entity");
            EdmEntityType targetType = model.AddEntityType("DevHarness", "Navigation");
            EdmEntityType subTargetType = model.AddEntityType("DevHarness", "SubNavigation");

            EdmTypeReference typeReference = new EdmStringTypeReference(EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.String), false);
            elementType.AddProperty(new EdmStructuralProperty(elementType, "Prop1", typeReference));
            targetType.AddProperty(new EdmStructuralProperty(targetType, "Prop1", typeReference));
            subTargetType.AddProperty(new EdmStructuralProperty(subTargetType, "Prop1", typeReference));

            EdmNavigationPropertyInfo propertyInfo = new EdmNavigationPropertyInfo();
            propertyInfo.Name = "Nav1";
            propertyInfo.Target = targetType;
            propertyInfo.TargetMultiplicity = EdmMultiplicity.One;
            EdmProperty navigation = EdmNavigationProperty.CreateNavigationProperty(elementType, propertyInfo);
            elementType.AddProperty(navigation);

            EdmNavigationPropertyInfo subPropertyInfo = new EdmNavigationPropertyInfo();
            subPropertyInfo.Name = "SubNav1";
            subPropertyInfo.Target = subTargetType;
            subPropertyInfo.TargetMultiplicity = EdmMultiplicity.One;
            EdmProperty subnavigation = EdmNavigationProperty.CreateNavigationProperty(targetType, subPropertyInfo);
            targetType.AddProperty(subnavigation);

            EdmEntityContainer container = model.AddEntityContainer("Default", "Container");
            container.AddEntitySet("Entities", elementType);

            ODataUriParser parser = new ODataUriParser(model, root, url);
            ComputeClause clause = parser.ParseCompute();
            SelectExpandClause se = parser.ParseSelectAndExpand();

            string compute = "Prop1 mul Prop2 as Product,Prop1 div Prop2 as Ratio,Prop2 mod Prop2 as Remainder";
            UriQueryExpressionParser uqep = new UriQueryExpressionParser(1000);
            var method = uqep.GetType().GetMethod("ParseCompute", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var output = method.Invoke(uqep, new object[] { compute });
        }
    }
}
