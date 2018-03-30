
using System;
using Infrastructure.DataAccess;

namespace MlbDataPump
{
    internal sealed class MlbType : SqlType
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
