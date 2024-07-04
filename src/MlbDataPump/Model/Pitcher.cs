
using System.ComponentModel.DataAnnotations.Schema;

namespace MlbDataPump.Model
{
    [Table("Pitchers", Schema = "mlb")]
    internal sealed class Pitcher : Player
    {
    }
}
