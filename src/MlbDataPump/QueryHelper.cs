
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Infrastructure.DataAccess;
using Infrastructure.DataAccess.OdataExpressionModel;
using Microsoft.AspNet.OData;
using Microsoft.OData.Edm;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MlbDataPump
{
    internal static class QueryHelper
    {
        public static void Prune()
        {
            DynamicNonQuery sp = new DynamicNonQuery(new MlbType());
            sp.Name = "mlb.PruneMlb";
            int results = sp.Execute();
        }

        public static void GetStandings()
        {
            DynamicProcedure<Model.StandingRecord> sp = new DynamicProcedure<Model.StandingRecord>(new MlbType());
            sp.Name = "mlb.GetGameByGameOutcomes";
            List<Model.StandingRecord> results = sp.Execute().ToList();
        }

        public static void Write<T>(List<T> instances)
        {
            WriterReader reader = new WriterReader(typeof(T));
            string json = JsonConvert.SerializeObject(instances);
            JArray array = JArray.Parse(json);
            foreach (JObject obj in array)
            {
                reader.Add(obj);
            }

            BulkWriterSettings settings = new BulkWriterSettings() { Store = new MlbType() };
            SqlBulkWriter writer = new SqlBulkWriter();
            writer.LoadAndMerge(reader, settings);
        }

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

        public static IQueryable<T> ReadCustom<T>(string path)
        {
            string address = "http://host/service/FileMetadata?store=Mlb";
            if (string.IsNullOrEmpty(path) == false)
            {
                address += path;
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
