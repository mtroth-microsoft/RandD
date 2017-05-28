using System.Collections.Generic;
using Microsoft.OData.UriParser;

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

            return;
        }
    }
}
