using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Infrastructure.DataAccess;
using Ninject;

public partial class _Default : System.Web.UI.Page
{
    public List<StandingRecord> ALRecords
    {
        get;
        private set;
    }

    public List<StandingRecord> NLRecords
    {
        get;
        private set;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        IKernel kernel = new StandardKernel();
        kernel.Bind<IConnectionFactory>().To<DefaultConnectionFactory>();
        kernel.Bind<IMessageLogger>().To<LoggingHelper>();
        Container.Initialize(kernel);

        this.ALRecords = GetStandings(103);
        this.NLRecords = GetStandings(104);
    }

    private static List<StandingRecord> GetStandings(int leagueId)
    {
        DynamicProcedure<StandingRecord> sp = new DynamicProcedure<StandingRecord>(new MlbType());
        sp.Name = "mlb.GetGameByGameOutcomes";
        sp.Assign("DivisionCode", null);
        sp.Assign("LeagueId", leagueId);
        List<StandingRecord> results = sp.Execute().ToList();
        return results;
    }
}