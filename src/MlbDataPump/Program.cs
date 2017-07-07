using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Infrastructure.DataAccess;
using Ninject;

namespace MlbDataPump
{
    class Program
    {
        static void Main(string[] args)
        {
            IKernel kernel = new StandardKernel();
            kernel.Bind<IConnectionFactory>().To<DefaultConnectionFactory>();
            kernel.Bind<IMessageLogger>().To<LoggingHelper>();
            Container.Initialize(kernel);
            new MlbModel(null).GetModel();

            Stage();
            Transform();
            //var game = QueryHelper.Read<Model.Game>(null).ToList().SingleOrDefault();
            var metadata = QueryHelper.Read<Model.FileMetadata>("Address eq 'http://gd2.mlb.com/components/game/mlb/year_2017/month_07/day_03/uber_scoreboard.xml?store=MlbType'")
                .ToList()
                .SingleOrDefault();
            GameLoader.Transform(metadata);
        }

        private static void Transform()
        {
            Model.FileMetadata metadata = null;
            do
            {
                metadata = AddressHelper.Instance.GetNextTransform();
                if (metadata != null)
                {
                    try
                    {
                        GameLoader.Transform(metadata);
                        AddressHelper.Instance.CommitTransform(metadata, true);
                        Console.WriteLine("TRANSFORM:: " + metadata.Address);
                    }
                    catch (Exception ex)
                    {
                        AddressHelper.Instance.CommitTransform(metadata, false);
                        Console.WriteLine("TRANSFORM:: " + ex.Message);
                    }
                }
            }
            while (metadata != null);
        }

        private static void Stage()
        {
            PageStore store = new PageStore();
            Model.FileMetadata metadata = null;
            do
            {
                metadata = AddressHelper.Instance.GetNextAddress();
                if (metadata != null)
                {
                    try
                    {
                        XElement page = WebRequestHelper.LoadPage(metadata);
                        store.AddPage(metadata, page);
                        AddressHelper.Instance.Ack(metadata);
                        Console.WriteLine("STAGE:: " + metadata.Address);
                    }
                    catch (Exception ex)
                    {
                        bool isxml = (ex is XmlException);
                        bool isweb = (ex is WebException);
                        AddressHelper.Instance.Nack(metadata, !(isxml || isweb));
                        Console.WriteLine("STAGE:: " + ex.Message);
                    }
                }
            }
            while (metadata != null);
        }
    }
}
