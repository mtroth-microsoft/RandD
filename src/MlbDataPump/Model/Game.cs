
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MlbDataPump.Model
{
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
