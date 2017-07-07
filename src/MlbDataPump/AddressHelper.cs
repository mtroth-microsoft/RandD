
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.OData;
using Infrastructure.DataAccess;
using Infrastructure.DataAccess.OdataExpressionModel;
using Microsoft.OData.Edm;

namespace MlbDataPump
{
    /// <summary>
    /// Status:
    /// 1: staged
    /// 2: running
    /// 3: staging error
    /// 4: transforming
    /// 5: transformed
    /// </summary>
    internal sealed class AddressHelper
    {
        private static AddressHelper instance = new AddressHelper();

        private HashSet<Model.FileMetadata> addresses = new HashSet<Model.FileMetadata>();

        private HashSet<Model.FileMetadata> running = new HashSet<Model.FileMetadata>();

        private HashSet<Model.FileMetadata> returned = new HashSet<Model.FileMetadata>();

        private HashSet<Model.FileMetadata> completed = new HashSet<Model.FileMetadata>();

        private HashSet<Model.FileMetadata> transformed = new HashSet<Model.FileMetadata>();

        private HashSet<Model.FileMetadata> failed = new HashSet<Model.FileMetadata>();

        private DateTime watermark = new DateTime(2010, 1, 1);

        private AddressHelper()
        {
            this.Initialize();
        }

        public static AddressHelper Instance
        {
            get
            {
                return instance;
            }
        }

        public Model.FileMetadata GetNextTransform()
        {
            Model.FileMetadata address = this.completed.OrderBy(p => p.EndTime).Take(1).FirstOrDefault();
            if (address != null)
            {
                Model.FileMetadata metadata = this.Start(address);
                metadata.Status = 4;
                metadata = this.Set(metadata);
                this.completed.Remove(address);
                this.running.Add(metadata);
                return metadata;
            }

            return address;
        }

        public void CommitTransform(Model.FileMetadata metadata, bool success)
        {
            if (this.running.Contains(metadata) == true &&
                this.transformed.Contains(metadata) == false &&
                this.completed.Contains(metadata) == false)
            {
                this.running.Remove(metadata);
                metadata.EndTime = DateTimeOffset.UtcNow;
                if (success == true)
                {
                    this.transformed.Add(metadata);
                    metadata.Status = 5;
                }
                else
                {
                    this.completed.Add(metadata);
                    metadata.Status = 1;
                }

                metadata = this.Set(metadata);
            }
        }

        public Model.FileMetadata GetNextAddress()
        {
            Model.FileMetadata address = this.addresses.OrderBy(p => p.Address).Take(1).FirstOrDefault();
            if (address != null)
            {
                Model.FileMetadata metadata = this.Start(address);
                metadata = this.Set(metadata);
                this.addresses.Remove(address);
                this.running.Add(metadata);
                return metadata;
            }
            else
            {
                address = this.returned.OrderBy(p => p.StartTime).Take(1).FirstOrDefault();
                if (address != null)
                {
                    Model.FileMetadata metadata = this.Start(address);
                    metadata = this.Set(metadata);
                    this.returned.Remove(address);
                    this.running.Add(metadata);
                    return metadata;
                }
            }

            return address;
        }

        public void Ack(Model.FileMetadata metadata)
        {
            if (this.running.Contains(metadata) == true &&
                this.completed.Contains(metadata) == false &&
                metadata != null)
            {
                this.completed.Add(metadata);
                this.running.Remove(metadata);
                metadata.EndTime = DateTimeOffset.UtcNow;
                metadata.Status = 1;
                metadata = this.Set(metadata);
            }
        }

        public void Nack(Model.FileMetadata metadata, bool retry)
        {
            if (this.running.Contains(metadata) == true &&
                this.completed.Contains(metadata) == false &&
                metadata != null)
            {
                if (retry == true)
                {
                    this.returned.Add(metadata);
                }
                else
                {
                    this.failed.Add(metadata);
                }

                this.running.Remove(metadata);
                metadata.EndTime = DateTimeOffset.UtcNow;
                metadata.Status = 3;
                metadata = this.Set(metadata);
            }
        }

        private static string Convert(int value)
        {
            if (value >= 10)
            {
                return value.ToString();
            }
            else
            {
                return "0" + value.ToString();
            }
        }

        private void Initialize()
        {            
            List<Model.FileMetadata> results = QueryHelper.Read<Model.FileMetadata>(null).ToList();

            this.completed = new HashSet<Model.FileMetadata>(results.Where(p => p.Status == 1));
            this.returned = new HashSet<Model.FileMetadata>(results.Where(p => p.Status == 3));
            this.addresses = new HashSet<Model.FileMetadata>(results.Where(p => p.Status == 2 || p.Status == 4));
            this.transformed = new HashSet<Model.FileMetadata>(results.Where(p => p.Status == 5));

            string template = ConfigurationManager.AppSettings["LocationTemplate"];
            DateTime now = DateTime.Now.AddDays(-1);
            DateTime test = this.watermark;
            while (now > test)
            {
                string address = string.Format(template, test.Year, Convert(test.Month), Convert(test.Day));
                if (this.completed.Any(p => p.Address == address) == false &&
                    this.returned.Any(p => p.Address == address) == false &&
                    this.running.Any(p => p.Address == address) == false &&
                    this.transformed.Any(p => p.Address == address) == false &&
                    this.addresses.Any(p => p.Address == address) == false)
                {
                    addresses.Add(new Model.FileMetadata() { Address = address });
                }

                test = test.AddDays(1);
            }
        }

        private Model.FileMetadata Start(Model.FileMetadata metadata)
        {
            metadata.StartTime = DateTimeOffset.UtcNow;
            metadata.EndTime = null;
            metadata.Status = 2;

            return metadata;
        }

        private Model.FileMetadata Set(Model.FileMetadata metadata)
        {
            //IEdmPrimitiveType type0 = EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.Int64);
            //IEdmPrimitiveType type1 = EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.String);
            //IEdmPrimitiveType type2 = EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.Byte);
            //IEdmPrimitiveType type3 = EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.DateTimeOffset);
            //IEdmPrimitiveTypeReference typeKind0 = new EdmPrimitiveTypeReference(type0, false);
            //IEdmPrimitiveTypeReference typeKind1 = new EdmPrimitiveTypeReference(type1, false);
            //IEdmPrimitiveTypeReference typeKind2 = new EdmPrimitiveTypeReference(type2, false);
            //IEdmPrimitiveTypeReference typeKind3 = new EdmPrimitiveTypeReference(type3, false);

            //EdmEntityType edmType = new EdmEntityType("MlbModel", "FileMetadata");
            //edmType.AddProperty(new EdmStructuralProperty(edmType, "Id", typeKind0));
            //edmType.AddProperty(new EdmStructuralProperty(edmType, "Address", typeKind1));
            //edmType.AddProperty(new EdmStructuralProperty(edmType, "Status", typeKind2));
            //edmType.AddProperty(new EdmStructuralProperty(edmType, "StartTime", typeKind3));
            //edmType.AddProperty(new EdmStructuralProperty(edmType, "EndTime", typeKind3));

            //EdmEntityObject eeo = new EdmEntityObject(edmType);
            //eeo.TrySetPropertyValue("Id", metadata.Id);
            //eeo.TrySetPropertyValue("Address", metadata.Address);
            //eeo.TrySetPropertyValue("Status", metadata.Status);
            //eeo.TrySetPropertyValue("StartTime", metadata.StartTime);
            //eeo.TrySetPropertyValue("EndTime", metadata.EndTime);

            EdmEntityObject eeo = QueryHelper.CreateEntity(metadata);

            MlbModel model = new MlbModel(new Uri(metadata.Address));
            WriteRequest request = new WriteRequest() { Entity = eeo };
            if (metadata.Id == default(long))
            {
                return model.Post<Model.FileMetadata>(request);
            }
            else
            {
                Model.FileMetadata result = model.Put<Model.FileMetadata>(request);
                metadata.Id = result.Id;
                metadata.StartTime = result.StartTime;
                metadata.Status = result.Status;
                metadata.EndTime = result.EndTime;
                metadata.Address = result.Address;
                return metadata;
            }
        }
    }
}
