using System;
using System.Collections.Generic;
using Infrastructure.Parser.UriParser;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            List<ExpressionToken> tokens = new List<ExpressionToken>();
            ExpressionLexer lexer = new ExpressionLexer("(foo eq true) or ('yes' eq tolower(bar))", false, false);
            while (true)
            {
                ExpressionToken token = lexer.NextToken();
                if (token.Kind == ExpressionTokenKind.End)
                {
                    break;
                }

                tokens.Add(token);
            }

            Uri url = new Uri("http://server/service/Entities?$filter=(foo eq true) or ('yes' eq tolower(bar)) or foobars/any()&$orderby=bar desc,foo&$top=1&$compute=bar as computedBar");
            List <CustomQueryOptionToken> options = QueryOptionUtils.ParseQueryOptions(url);

            return;
        }
    }
}
