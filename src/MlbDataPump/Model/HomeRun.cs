
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Infrastructure.DataAccess;

namespace MlbDataPump.Model
{
    [Table("HomeRuns")]
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

        public string RawGameId
        {
            get;
            set;
        }

        [NotMapped, ForeignKey("Game")]
        public long GameId
        {
            get;
            set;
        }

        public Game Game
        {
            get;
            set;
        }

        [NotMapped, ForeignKey("Hitter")]
        public int HitterId
        {
            get;
            set;
        }

        public Hitter Hitter
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
