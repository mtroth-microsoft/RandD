
using System.ComponentModel.DataAnnotations.Schema;

namespace MlbDataPump.Model
{
    [Table("Hitters", Schema = "mlb")]
    internal sealed class Hitter : Player
    {
    }
}
