﻿
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MlbDataPump.Model
{
    [Table("FileMetadata")]
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
    }
}