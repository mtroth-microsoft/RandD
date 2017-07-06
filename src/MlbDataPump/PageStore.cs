
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.OData;
using System.Xml.Linq;
using Infrastructure.DataAccess;
using Infrastructure.DataAccess.OdataExpressionModel;
using Microsoft.OData.Edm;

namespace MlbDataPump
{
    internal sealed class PageStore
    {
        public void AddPage(Model.FileMetadata metadata, XElement xml)
        {
            Model.FileStaging result = QueryHelper
                .Read<Model.FileStaging>(string.Format("Address eq '{0}'", metadata.Address))
                .ToList()
                .SingleOrDefault();

            if (result == null)
            {
                Model.FileStaging file = new Model.FileStaging();
                file.Content = xml.ToString();
                file.Address = metadata.Address;

                //IEdmPrimitiveType type = EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.String);
                //IEdmPrimitiveType type1 = EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.Int64);
                //IEdmPrimitiveTypeReference typeKind = new EdmPrimitiveTypeReference(type, false);
                //IEdmPrimitiveTypeReference typeKind1 = new EdmPrimitiveTypeReference(type1, false);
                //EdmEntityType edmType = new EdmEntityType("MlbModel", "FileStaging");
                //edmType.AddProperty(new EdmStructuralProperty(edmType, "Content", typeKind));
                //edmType.AddProperty(new EdmStructuralProperty(edmType, "Id", typeKind1));
                //edmType.AddProperty(new EdmStructuralProperty(edmType, "Address", typeKind));

                //EdmEntityObject eeo = new EdmEntityObject(edmType);
                //eeo.TrySetPropertyValue("Content", file.Content);
                //eeo.TrySetPropertyValue("Id", file.Id);
                //eeo.TrySetPropertyValue("Address", file.Address);

                EdmEntityObject eeo = QueryHelper.CreateEntity(file);

                MlbModel model = new MlbModel(new Uri(metadata.Address));
                WriteRequest request = new WriteRequest() { Entity = eeo };
                model.Post<Model.FileStaging>(request);
            }
        }
    }
}
