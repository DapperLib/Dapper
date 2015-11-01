using System.Collections.Generic;

namespace Dapper
{
    partial class SqlMapper
    {
        /// <summary>
        /// Represents a placeholder for a value that should be replaced as a literal value in the resulting sql
        /// </summary>
        internal struct LiteralToken
        {
            private readonly string token, member;
            /// <summary>
            /// The text in the original command that should be replaced
            /// </summary>
            public string Token { get { return token; } }

            /// <summary>
            /// The name of the member referred to by the token
            /// </summary>
            public string Member { get { return member; } }
            internal LiteralToken(string token, string member)
            {
                this.token = token;
                this.member = member;
            }

            internal static readonly IList<LiteralToken> None = new LiteralToken[0];
        }
    }
}
