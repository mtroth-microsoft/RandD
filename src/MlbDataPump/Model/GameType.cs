/*
  insert mlb.GameTypeMap select '2023-10-03 00:00:00.000', '2023-10-05 00:00:00.000', 'WildCard', 70, 'F'
  insert mlb.GameTypeMap select '2023-10-07 00:00:00.000', '2023-10-14 00:00:00.000', 'DivisionalPlayoff', 68, 'D'
  insert mlb.GameTypeMap select '2023-10-15 00:00:00.000', '2023-10-24 00:00:00.000', 'LeaguePlayOff', 76, 'L'
  insert mlb.GameTypeMap select '2023-10-27 00:00:00.000', '2023-11-04 00:00:00.000', 'WorldSeries', 87, 'W'
*/
namespace MlbDataPump.Model
{
    public enum GameType
    {
        Exhibition = 'E',
        AllStar = 'A',
        Spring = 'S',
        Regular = 'R',
        WildCard = 'F',
        DivisionalPlayoff = 'D',
        LeaguePlayOff = 'L',
        WorldSeries = 'W'
    }
}