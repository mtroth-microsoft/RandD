using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MlbDataPump.Model
{
    [Table("GameTypeMap")]
    internal sealed class GameTypeMap
    {
        public int Id
        {
            get;
            set;
        }

        public DateTime StartDate
        {
            get;
            set;
        }

        public DateTime EndDate
        {
            get;
            set;
        }

        public string GameTypeName
        {
            get;
            set;
        }

        public int GameTypeId
        {
            get;
            set;
        }

        public string GameTypeValue
        {
            get;
            set;
        }
    }
}
