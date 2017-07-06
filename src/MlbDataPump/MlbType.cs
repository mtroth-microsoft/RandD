
using Infrastructure.DataAccess;

namespace MlbDataPump
{
    internal sealed class MlbType : DatabaseType
    {
        public override string Name
        {
            get
            {
                return "Mlb";
            }
        }
    }
}
