
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MlbDataPump.Model
{
    internal abstract class Player
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id
        {
            get;
            set;
        }

        public string First
        {
            get;
            set;
        }

        public string Last
        {
            get;
            set;
        }

        [NotMapped, ForeignKey("Team")]
        public int TeamId
        {
            get;
            set;
        }

        public Team Team
        {
            get;
            set;
        }
    }
}
