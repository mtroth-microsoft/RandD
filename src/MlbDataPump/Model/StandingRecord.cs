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
    }
}
