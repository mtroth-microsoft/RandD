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
      <table style="border-color:black;border:groove">
        <thead>
            <tr>
                <td style="background-color:blue;color:white;font-weight:bold;border-color:black">Name</td>
                <td style="background-color:blue;color:white;font-weight:bold;border-color:black">Record</td>
                <td style="background-color:blue;color:white;font-weight:bold;border-color:black">Pct.</td>
                <td style="background-color:blue;color:white;font-weight:bold;border-color:black">GB</td>
                <td style="background-color:blue;color:white;font-weight:bold;border-color:black">WCGB</td>
                <td style="background-color:blue;color:white;font-weight:bold;border-color:black">Last Game</td>
                <td style="background-color:blue;color:white;font-weight:bold;border-color:black">Last Opponent</td>
                <td style="background-color:blue;color:white;font-weight:bold;border-color:black">Trend</td>
                <td style="background-color:blue;color:white;font-weight:bold;border-color:black">Score</td>
                <td style="background-color:blue;color:white;font-weight:bold;border-color:black">Last 10</td>
                <td style="background-color:blue;color:white;font-weight:bold;border-color:black">Home Record</td>
                <td style="background-color:blue;color:white;font-weight:bold;border-color:black">Away Record</td>
                <td style="background-color:blue;color:white;font-weight:bold;border-color:black">Next Game</td>
                <td style="background-color:blue;color:white;font-weight:bold;border-color:black">Next Opponent</td>
            </tr>
        </thead>
    <%  string color = "gainsboro";
        string lastDivisionCode = null;
        foreach (StandingRecord item in this.ALRecords)
        {
            string divisionCode = item.DivisionCode;
            if (divisionCode != lastDivisionCode)
            {
                color = "azure";
    %>
                <tr><td colspan="14" style="background-color:black;color:white;font-weight:bold"><%= divisionCode == "W" ? "Western Division" : divisionCode == "C" ? "Central Division" : "Eastern Division" %></td></tr>
    <%
            }

        color = color == "gainsboro" ? "azure" : "gainsboro";
    %>
        <tr>
            <td style="background-color:<%= color %>"><%=item.Name %></td>
            <td style="background-color:<%= color %>"><%=item.Wins %> - <%=item.Losses %></td>
            <td style="background-color:<%= color %>"><%=item.Pct %></td>
            <td style="background-color:<%= color %>"><%=item.GB %></td>
            <td style="background-color:<%= color %>"><%=item.WCGB %></td>
            <td style="background-color:<%= color %>"><%=item.Date %></td>
            <td style="background-color:<%= color %>"><%= item.GameCode == "Home" ? "vs " : "@" %><%=item.Opponent %></td>
            <td style="background-color:<%= color %>"><%=item.Outcome %> <%=item.Trend %></td>
            <td style="background-color:<%= color %>"><%=item.Us %> - <%=item.Them %></td>
            <td style="background-color:<%= color %>"><%=item.W %> - <%=item.L %></td>
            <td style="background-color:<%= color %>"><%=item.WinsAtHome %> - <%=item.LossesAtHome %></td>
            <td style="background-color:<%= color %>"><%=item.WinsOnRoad %> - <%=item.LossesOnRoad %></td>
            <td style="background-color:<%= color %>"><%=item.NextGame %></td>
            <td style="background-color:<%= color %>"><%=item.NextGameCode == "Home" ? "vs " : "@" %><%=item.NextOpponent %></td>
        </tr>
    <%
            lastDivisionCode = item.DivisionCode;
        }
    %>
      </table>

      <b>National League</b>
      <table style="border-color:black;border:groove">
        <thead>
            <tr>
                <td style="background-color:red;color:white;font-weight:bold;border-color:black">Name</td>
                <td style="background-color:red;color:white;font-weight:bold;border-color:black">Record</td>
                <td style="background-color:red;color:white;font-weight:bold;border-color:black">Pct.</td>
                <td style="background-color:red;color:white;font-weight:bold;border-color:black">GB</td>
                <td style="background-color:red;color:white;font-weight:bold;border-color:black">WCGB</td>
                <td style="background-color:red;color:white;font-weight:bold;border-color:black">Last Game</td>
                <td style="background-color:red;color:white;font-weight:bold;border-color:black">Last Opponent</td>
                <td style="background-color:red;color:white;font-weight:bold;border-color:black">Trend</td>
                <td style="background-color:red;color:white;font-weight:bold;border-color:black">Score</td>
                <td style="background-color:red;color:white;font-weight:bold;border-color:black">Last 10</td>
                <td style="background-color:red;color:white;font-weight:bold;border-color:black">Home Record</td>
                <td style="background-color:red;color:white;font-weight:bold;border-color:black">Away Record</td>
                <td style="background-color:red;color:white;font-weight:bold;border-color:black">Next Game</td>
                <td style="background-color:red;color:white;font-weight:bold;border-color:black">Next Opponent</td>
            </tr>
        </thead>
    <%  lastDivisionCode = null;
        foreach (StandingRecord item in this.NLRecords)
        {
            string divisionCode = item.DivisionCode;
            if (divisionCode != lastDivisionCode)
            {
                color = "azure";
    %>
                <tr><td colspan="14" style="background-color:black;color:white;font-weight:bold"><%= divisionCode == "W" ? "Western Division" : divisionCode == "C" ? "Central Division" : "Eastern Division" %></td></tr>
    <%
            }

        color = color == "gainsboro" ? "azure" : "gainsboro";
    %>
        <tr>
            <td style="background-color:<%= color %>"><%=item.Name %></td>
            <td style="background-color:<%= color %>"><%=item.Wins %> - <%=item.Losses %></td>
            <td style="background-color:<%= color %>"><%=item.Pct %></td>
            <td style="background-color:<%= color %>"><%=item.GB %></td>
            <td style="background-color:<%= color %>"><%=item.WCGB %></td>
            <td style="background-color:<%= color %>"><%=item.Date %></td>
            <td style="background-color:<%= color %>"><%= item.GameCode == "Home" ? "vs " : "@" %><%=item.Opponent %></td>
            <td style="background-color:<%= color %>"><%=item.Outcome %> <%=item.Trend %></td>
            <td style="background-color:<%= color %>"><%=item.Us %> - <%=item.Them %></td>
            <td style="background-color:<%= color %>"><%=item.W %> - <%=item.L %></td>
            <td style="background-color:<%= color %>"><%=item.WinsAtHome %> - <%=item.LossesAtHome %></td>
            <td style="background-color:<%= color %>"><%=item.WinsOnRoad %> - <%=item.LossesOnRoad %></td>
            <td style="background-color:<%= color %>"><%=item.NextGame %></td>
            <td style="background-color:<%= color %>"><%=item.NextGameCode == "Home" ? "vs " : "@" %><%=item.NextOpponent %></td>
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
