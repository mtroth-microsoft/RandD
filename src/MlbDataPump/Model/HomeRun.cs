
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MlbDataPump.Model
{
    internal sealed class HomeRun
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id
        {
            get;
            set;
        }

        public int Runners
        {
            get;
            set;
        }

        public int Inning
        {
            get;
            set;
        }

        public string GameId
        {
            get;
            set;
        }

        public Hitter Hitter
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
