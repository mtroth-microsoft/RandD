
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Infrastructure.DataAccess;

namespace MlbDataPump.Model
{
    [Table("FileStaging", Schema = "mlb")]
    internal sealed class FileStaging
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

        public string Content
        {
            get;
            set;
        }

        [InsertedTime]
        public DateTimeOffset InsertedTime
        {
            get;
            set;
        }

        [UpdatedTime]
        public DateTimeOffset UpdatedTime
        {
            get;
            set;
        }
    }
}
