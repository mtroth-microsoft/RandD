
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
        public bool AddPage(Model.FileMetadata metadata, XElement xml)
        {
            if (Verify(xml) == true)
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

                    EdmEntityObject eeo = QueryHelper.CreateEntity(file);

                    MlbModel model = new MlbModel(new Uri(metadata.Address));
                    WriteRequest request = new WriteRequest() { Entity = eeo };
                    model.Post<Model.FileStaging>(request);
                }
            }
            else
            {
                GameLoader.HandlePreviews(metadata, xml);
                return true;
            }

            return false;
        }

        private static bool Verify(XElement xml)
        {
            foreach (XElement child in xml.Elements())
            {
                if (child.Name.LocalName == "game")
                {
                    if (VerifyStatus(child) == false)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool VerifyStatus(XElement child)
        {
            foreach (XElement sub in child.Elements())
            {
                if (sub.Name.LocalName == "status")
                {
                    if (sub.Attribute("status").Value == "Preview" ||
                        sub.Attribute("status").Value == "InProgress")
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
