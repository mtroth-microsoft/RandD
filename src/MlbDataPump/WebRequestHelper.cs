
using System.IO;
using System.Net;
using System.Xml.Linq;

namespace MlbDataPump
{
    internal sealed class WebRequestHelper
    {
        public static XElement LoadPage(Model.FileMetadata metadata)
        {
            HttpWebRequest request = WebRequest.Create(metadata.Address) as HttpWebRequest;
            WebResponse response = request.GetResponse();
            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string xml = reader.ReadToEnd();
                    return XElement.Parse(xml);
                }
            }
        }
    }
}
