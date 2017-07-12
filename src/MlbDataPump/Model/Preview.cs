
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MlbDataPump.Model
{
    [Table("Previews")]
    internal sealed class Preview
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id
        {
            get;
            set;
        }

        public string GameId
        {
            get;
            set;
        }

        public DateTimeOffset Date
        {
            get;
            set;
        }

        public GameType GameType
        {
            get;
            set;
        }

        public string Address
        {
            get;
            set;
        }

        [NotMapped, ForeignKey("HomeTeam")]
        public int HomeTeamId
        {
            get;
            set;
        }

        public Team HomeTeam
        {
            get;
            set;
        }

        [NotMapped, ForeignKey("AwayTeam")]
        public int AwayTeamId
        {
            get;
            set;
        }

        public Team AwayTeam
        {
            get;
            set;
        }
    }
}
