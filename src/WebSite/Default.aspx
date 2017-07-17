<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
      <b>American League</b>
      <table border="1">
        <thead>
            <tr>
                <td>Division</td>
                <td>Name</td>
                <td>Record</td>
                <td>Pct.</td>
                <td>GB</td>
                <td>WCGB</td>
                <td>Last Game</td>
                <td>Last Opponent</td>
                <td>Trend</td>
                <td>Score</td>
                <td>Last 10</td>
                <td>Home Record</td>
                <td>Away Record</td>
                <td>Next Game</td>
                <td>Next Opponent</td>
            </tr>
        </thead>
    <%  string lastDivisionCode = null;
        foreach (StandingRecord item in this.ALRecords)
        {
            string divisionCode = item.DivisionCode;
            if (divisionCode != lastDivisionCode)
            {
    %>
                <tr><td colspan="15"><%= divisionCode == "W" ? "Western Division" : divisionCode == "C" ? "Central Division" : "Eastern Division" %></td></tr>
    <%
            }
    %>
        <tr>
            <td><%=item.DivisionCode %></td>
            <td><%=item.Name %></td>
            <td><%=item.Wins %> - <%=item.Losses %></td>
            <td><%=item.Pct %></td>
            <td><%=item.GB %></td>
            <td><%=item.WCGB %></td>
            <td><%=item.Date %></td>
            <td><%= item.GameCode == "Home" ? "vs " : "@" %><%=item.Opponent %></td>
            <td><%=item.Outcome %> <%=item.Trend %></td>
            <td><%=item.Us %> - <%=item.Them %></td>
            <td><%=item.W %> - <%=item.L %></td>
            <td><%=item.WinsAtHome %> - <%=item.LossesAtHome %></td>
            <td><%=item.WinsOnRoad %> - <%=item.LossesOnRoad %></td>
            <td><%=item.NextGame %></td>
            <td><%=item.NextGameCode == "Home" ? "vs " : "@" %><%=item.NextOpponent %></td>
        </tr>
    <%
            lastDivisionCode = item.DivisionCode;
        }
    %>
      </table>

      <b>National League</b>
      <table border="1">
        <thead>
            <tr>
                <td>Division</td>
                <td>Name</td>
                <td>Record</td>
                <td>Pct.</td>
                <td>GB</td>
                <td>WCGB</td>
                <td>Last Game</td>
                <td>Last Opponent</td>
                <td>Trend</td>
                <td>Score</td>
                <td>Last 10</td>
                <td>Home Record</td>
                <td>Away Record</td>
                <td>Next Game</td>
                <td>Next Opponent</td>
            </tr>
        </thead>
    <%  lastDivisionCode = null;
        foreach (StandingRecord item in this.NLRecords)
        {
            string divisionCode = item.DivisionCode;
            if (divisionCode != lastDivisionCode)
            {
    %>
                <tr><td colspan="15"><%= divisionCode == "W" ? "Western Division" : divisionCode == "C" ? "Central Division" : "Eastern Division" %></td></tr>
    <%
            }
    %>
        <tr>
            <td><%=item.DivisionCode %></td>
            <td><%=item.Name %></td>
            <td><%=item.Wins %> - <%=item.Losses %></td>
            <td><%=item.Pct %></td>
            <td><%=item.GB %></td>
            <td><%=item.WCGB %></td>
            <td><%=item.Date %></td>
            <td><%= item.GameCode == "Home" ? "vs " : "@" %><%=item.Opponent %></td>
            <td><%=item.Outcome %> <%=item.Trend %></td>
            <td><%=item.Us %> - <%=item.Them %></td>
            <td><%=item.W %> - <%=item.L %></td>
            <td><%=item.WinsAtHome %> - <%=item.LossesAtHome %></td>
            <td><%=item.WinsOnRoad %> - <%=item.LossesOnRoad %></td>
            <td><%=item.NextGame %></td>
            <td><%=item.NextGameCode == "Home" ? "vs " : "@" %><%=item.NextOpponent %></td>
        </tr>
    <%
            lastDivisionCode = item.DivisionCode;
        }
    %>
      </table>

    </div>
    </form>
</body>
</html>
