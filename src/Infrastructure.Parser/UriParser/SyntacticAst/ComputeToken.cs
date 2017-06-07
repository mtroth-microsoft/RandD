//---------------------------------------------------------------------
// <copyright file="ComputeToken.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Infrastructure.Parser.UriParser
{
    using System.Collections.Generic;

    /// <summary>
    /// Query token representing an Aggregate token.
    /// </summary>
    public sealed class ComputeToken : QueryToken
    {
        private QueryToken expression;

        private string alias;

        /// <summary>
        /// Create an ComputeToken.
        /// </summary>
        /// <param name="expression">The computation token.</param>
        /// <param name="alias">The alias for the computation.</param>
        public ComputeToken(QueryToken expression, string alias)
        {
            ExceptionUtils.CheckArgumentNotNull(expression, "expression");
            ExceptionUtils.CheckArgumentNotNull(alias, "alias");

            this.expression = expression;
            this.alias = alias;
        }

        /// <summary>
        /// Gets the kind of this token.
        /// </summary>
        public override QueryTokenKind Kind
        {
            get { return QueryTokenKind.Compute; }
        }

        /// <summary>
        /// Gets the QueryToken.
        /// </summary>
        public QueryToken Expression
        {
            get
            {
                return this.expression;
            }
        }

        /// <summary>
        /// Gets the alias of the computation.
        /// </summary>
        public string Alias
        {
            get
            {
                return this.alias;
            }
        }

        /// <summary>
        /// Accept a <see cref="ISyntacticTreeVisitor{T}"/> to walk a tree of <see cref="QueryToken"/>s.
        /// </summary>
        /// <typeparam name="T">Type that the visitor will return after visiting this token.</typeparam>
        /// <param name="visitor">An implementation of the visitor interface.</param>
        /// <returns>An object whose type is determined by the type parameter of the visitor.</returns>
        public override T Accept<T>(ISyntacticTreeVisitor<T> visitor)
        {
            return default(T);
        }
    }
}
