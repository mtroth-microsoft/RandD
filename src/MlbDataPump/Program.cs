using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Infrastructure.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace MlbDataPump
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceCollection collection = new ServiceCollection();
            collection.AddSingleton<IConnectionFactory>(new DefaultConnectionFactory());
            collection.AddSingleton<IMessageLogger>(new LoggingHelper());

            Container.Initialize(collection.BuildServiceProvider());
            // new MlbModel(null).GetModel();
            QueryHelper.GetStandings();

            //var metadata = QueryHelper.ReadCustom<Model.FileMetadata>("&$top=1&$orderby=EventDate desc&$filter=Status eq 5")
            //    .ToList()
            //    .SingleOrDefault();
            Stage();
            Transform();
            Prune();
            //var team = QueryHelper.Read<Model.Team>("Name eq 'Mariners' and City eq 'Seattle'").ToList().SingleOrDefault();
            //var games = QueryHelper.Read<Model.Game>(string.Format("(HomeTeam/Id eq {0} or AwayTeam/Id eq {0}) and year(Date) eq 2017", team.Id)).ToList();
        }

        private static void Prune()
        {
            QueryHelper.Prune();
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
                string operation = "STAGE:: ";
                metadata = AddressHelper.Instance.GetNextAddress();
                if (metadata != null)
                {
                    try
                    {
                        XElement page = WebRequestHelper.LoadPage(metadata);
                        bool preview = store.AddPage(metadata, page);
                        if (preview == false)
                        {
                            AddressHelper.Instance.Ack(metadata);
                            Console.WriteLine(operation + metadata.Address);
                        }
                        else
                        {
                            operation = "PREVIEW:: ";
                            AddressHelper.Instance.SetPreview(metadata);
                            Console.WriteLine(operation + metadata.Address);
                        }
                    }
                    catch (Exception ex)
                    {
                        bool isxml = (ex is XmlException);
                        bool isweb = (ex is WebException);
                        AddressHelper.Instance.Nack(metadata, !(isxml || isweb));
                        Console.WriteLine(operation + ex.Message);
                    }
                }
            }
            while (metadata != null);
        }
    }
}
