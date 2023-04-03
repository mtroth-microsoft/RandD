
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Infrastructure.DataAccess;
using MlbDataPump.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MlbDataPump
{
    internal sealed class WebRequestHelper
    {
        public static void LoadPage(Model.FileMetadata metadata)
        {
            bool convertResponse = true;
            HttpWebRequest request = WebRequest.Create(metadata.AddressEx) as HttpWebRequest;
            WebResponse response = null;
            response = request.GetResponse();
            
            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string xml = reader.ReadToEnd();
                    metadata.Blob = xml;
                    metadata.Converted = convertResponse;
                }
            }
        }
    }
}
