
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MlbDataPump.Model
{
    [Table("Games")]
    internal sealed class Game
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id
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

        public int Innings
        {
            get;
            set;
        }

        public Score HomeScore
        {
            get;
            set;
        }

        public Score AwayScore
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

        public Record HomeRecord
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

        public Record AwayRecord
        {
            get;
            set;
        }

        [NotMapped, ForeignKey("WinningPitcher")]
        public int WinningPitcherId
        {
            get;
            set;
        }

        public Pitcher WinningPitcher
        {
            get;
            set;
        }

        public Record WinningPitcherRecord
        {
            get;
            set;
        }

        [NotMapped, ForeignKey("LosingPitcher")]
        public int LosingPitcherId
        {
            get;
            set;
        }

        public Pitcher LosingPitcher
        {
            get;
            set;
        }

        public Record LosingPitcherRecord
        {
            get;
            set;
        }

        [NotMapped, ForeignKey("SavingPitcher")]
        public int? SavingPitcherId
        {
            get;
            set;
        }

        public Pitcher SavingPitcher
        {
            get;
            set;
        }

        public Record SavingPitcherRecord
        {
            get;
            set;
        }

        public ICollection<HomeRun> HomeRuns
        {
            get;
            set;
        }
    }
}
