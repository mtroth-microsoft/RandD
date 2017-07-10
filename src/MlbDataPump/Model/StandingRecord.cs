using System;

namespace MlbDataPump.Model
{
    internal sealed class StandingRecord
    {
        public int Id
        {
            get;
            set;
        }

        public string DivisionCode
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public int Wins
        {
            get;
            set;
        }

        public int Losses
        {
            get;
            set;
        }

        public decimal Pct
        {
            get;
            set;
        }

        public decimal? GB
        {
            get;
            set;
        }

        public decimal? WCGB
        {
            get;
            set;
        }

        public DateTimeOffset Date
        {
            get;
            set;
        }

        public string Outcome
        {
            get;
            set;
        }

        public int Us
        {
            get;
            set;
        }

        public int Them
        {
            get;
            set;
        }

        public string GameCode
        {
            get;
            set;
        }

        public string Opponent
        {
            get;
            set;
        }

        public int Trend
        {
            get;
            set;
        }

        public int W
        {
            get;
            set;
        }

        public int L
        {
            get;
            set;
        }

        public int WinsAtHome
        {
            get;
            set;
        }

        public int LossesAtHome
        {
            get;
            set;
        }

        public int WinsOnRoad
        {
            get;
            set;
        }

        public int LossesOnRoad
        {
            get;
            set;
        }
    }
}
