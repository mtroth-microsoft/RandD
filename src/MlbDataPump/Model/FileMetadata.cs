
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MlbDataPump.Model
{
    [Table("FileMetadata", Schema = "mlb")]
    internal sealed class FileMetadata
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id
        {
            get;
            set;
        }

        public string Address
        {
            get;
            set;
        }

        public byte Status
        {
            get;
            set;
        }

        public DateTimeOffset StartTime
        {
            get;
            set;
        }

        public DateTimeOffset? EndTime
        {
            get;
            set;
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset EventDate
        {
            get;
            set;
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string AddressEx
        {
            get;
            set;
        }

        internal string Blob
        {
            get;
            set;
        }

        internal bool Converted
        {
            get;
            set;
        }
    }
}
