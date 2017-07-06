
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.OData;
using Infrastructure.DataAccess;
using Infrastructure.DataAccess.OdataExpressionModel;
using Microsoft.OData.Edm;

namespace MlbDataPump
{
    internal static class QueryHelper
    {
        public static IQueryable<T> Read<T>(string filter)
        {
            string address = "http://host/service/FileMetadata?store=Mlb";
            if (string.IsNullOrEmpty(filter) == false)
            {
                address += "&$filter=" + filter;
            }

            Uri uri = new Uri(address);
            QueryBuilderSettings settings = DataFilterParsingHelper.ExtractToSettings(uri, null);
            DynamicQueryable<T> queryable = new DynamicQueryable<T>(settings, new MlbType(), null);

            return queryable;
        }

        public static EdmEntityObject CreateEntity<T>(T instance)
        {
            EdmEntityType edmType = new EdmEntityType(typeof(T).Namespace, typeof(T).Name);
            EdmEntityObject eeo = new EdmEntityObject(edmType);
            PropertyInfo[] properties = typeof(T).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                IEdmTypeReference reference = GetTypeReference(property.PropertyType);
                edmType.AddProperty(new EdmStructuralProperty(edmType, property.Name, reference));
                object value = property.GetValue(instance);
                eeo.TrySetPropertyValue(property.Name, value);
            }

            return eeo;
        }

        private static IEdmTypeReference GetTypeReference(Type propertyType)
        {
            IEdmPrimitiveType type = null;
            switch (propertyType.Name)
            {
                case "String":
                    type = EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.String);
                    break;

                case "Int64":
                    type = EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.Int64);
                    break;

                case "Byte":
                    type = EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.Byte);
                    break;

                case "DateTimeOffset":
                    type = EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.DateTimeOffset);
                    break;
                case "Nullable`1":
                    return GetTypeReference(propertyType.GetGenericArguments()[0]);
            }

            IEdmPrimitiveTypeReference reference = new EdmPrimitiveTypeReference(type, false);
            return reference;
        }
    }
}
