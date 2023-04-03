
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Infrastructure.DataAccess;
using Infrastructure.DataAccess.OdataExpressionModel;
using Microsoft.AspNet.OData;
using Microsoft.OData.Edm;

namespace MlbDataPump
{
    internal sealed class PageStore
    {
        public bool AddPage(Model.FileMetadata metadata)
        {
            if (metadata.EventDate.Date < metadata.StartTime.Date)
            {
                Model.FileStaging result = QueryHelper
                    .Read<Model.FileStaging>(string.Format("Address eq '{0}'", metadata.Address))
                    .ToList()
                    .SingleOrDefault();

                if (result == null)
                {
                    Regex regex = new Regex("<div id=\"espnfitt\">(.*?)\n");
                    var blob = regex.Match(metadata.Blob.Replace("xlink:", string.Empty));

                    XElement xml = XElement.Parse(blob.Value);
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
                GameLoader.HandlePreviews(metadata);
                return true;
            }

            return false;
        }
    }
}
