﻿
using System;
using Infrastructure.DataAccess;
using Microsoft.OData.Edm;
using MlbDataPump.Model;

namespace MlbDataPump
{
    public sealed class MlbModel : AutoGeneratedProxy
    {
        public MlbModel(Uri url) : base(url)
        {
        }

        public override IEdmModel GetModel()
        {
            //TypeCache.Type<Game>().HasSingle(p => p.HomeTeam).WithMany().Map(p => { p.MapLeftKey("HomeTeamId"); p.MapRightKey("Id"); });
            //TypeCache.Type<Game>().HasSingle(p => p.AwayTeam).WithMany().Map(p => { p.MapLeftKey("AwayTeamId"); p.MapRightKey("Id"); });

            //TypeCache.Type<Game>().HasSingle(p => p.WinningPitcher).WithMany().Map(p => { p.MapLeftKey("WinningPitcherId"); p.MapRightKey("Id"); });
            //TypeCache.Type<Game>().HasSingle(p => p.LosingPitcher).WithMany().Map(p => { p.MapLeftKey("LosingPitcherId"); p.MapRightKey("Id"); });
            //TypeCache.Type<Game>().HasSingle(p => p.SavingPitcher).WithMany().Map(p => { p.MapLeftKey("SavingPitcherId"); p.MapRightKey("Id"); });

            //TypeCache.Type<Game>().HasMany(p => p.HomeRuns).WithSingle(p => p.Game).Map(p => { p.MapLeftKey("Id"); p.MapRightKey("GameId"); });

            //TypeCache.Type<Player>().HasSingle(p => p.Team).WithMany().Map(p => { p.MapLeftKey("TeamId"); p.MapRightKey("Id"); });

            //TypeCache.Type<HomeRun>().HasSingle(p => p.Hitter).WithMany().Map(p => { p.MapLeftKey("HitterId"); p.MapRightKey("Id"); });
            //TypeCache.Type<HomeRun>().HasSingle(p => p.Team).WithMany().Map(p => { p.MapLeftKey("TeamId"); p.MapRightKey("Id"); });

            return null;
        }
    }
}
