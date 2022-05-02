
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MlbDataPump.Model
{
    [Table("Teams")]
    internal sealed class Team
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id
        {
            get;
            set;
        }

        public string City
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string Code
        {
            get;
            set;
        }

        public string DivisionCode
        {
            get;
            set;
        }

        public int LeagueId
        {
            get;
            set;
        }

        public string EspnName
        { 
            get; 
            set; 
        }
    }
}
