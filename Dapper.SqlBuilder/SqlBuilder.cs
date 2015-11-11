using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dapper
{
    public class SqlBuilder
    {
        Dictionary<string, Clauses> data = new Dictionary<string, Clauses>();
        int seq;

        class Clause
        {
            public string Sql { get; set; }
            public object Parameters { get; set; }
            public bool IsInclusive { get; set; }
        }

        class Clauses : List<Clause>
        {
            string joiner;
            string prefix;
            string postfix;

#if CSHARP30
            public Clauses(string joiner, string prefix, string postfix)
#else
            public Clauses(string joiner, string prefix = "", string postfix = "")
#endif
            {
                this.joiner = joiner;
                this.prefix = prefix;
                this.postfix = postfix;
            }

            public string ResolveClauses(DynamicParameters p)
            {
                foreach (var item in this)
                {
                    p.AddDynamicParams(item.Parameters);
                }
                return this.Any(a => a.IsInclusive)
                    ? prefix +
                      string.Join(joiner,
                          this.Where(a => !a.IsInclusive)
                              .Select(c => c.Sql)
                              .Union(new[]
                              {
                                  " ( " +
                                  string.Join(" OR ", this.Where(a => a.IsInclusive).Select(c => c.Sql).ToArray()) +
                                  " ) "
                              }).ToArray()) + postfix
                    : prefix + string.Join(joiner, this.Select(c => c.Sql).ToArray()) + postfix;
            }
        }

        public class Template
        {
            readonly string sql;
            readonly SqlBuilder builder;
            readonly object initParams;
            int dataSeq = -1; // Unresolved

#if CSHARP30
            public Template(SqlBuilder builder, string sql, object parameters)
#else
            public Template(SqlBuilder builder, string sql, dynamic parameters)
#endif
            {
                this.initParams = parameters;
                this.sql = sql;
                this.builder = builder;
            }

            static System.Text.RegularExpressions.Regex regex =
                new System.Text.RegularExpressions.Regex(@"\/\*\*.+\*\*\/", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.Multiline);

            void ResolveSql()
            {
                if (dataSeq != builder.seq)
                {
                    DynamicParameters p = new DynamicParameters(initParams);

                    rawSql = sql;

                    foreach (var pair in builder.data)
                    {
                        rawSql = rawSql.Replace("/**" + pair.Key + "**/", pair.Value.ResolveClauses(p));
                    }
                    parameters = p;

                    // replace all that is left with empty
                    rawSql = regex.Replace(rawSql, "");

                    dataSeq = builder.seq;
                }
            }

            string rawSql;
            object parameters;

            public string RawSql { get { ResolveSql(); return rawSql; } }
            public object Parameters { get { ResolveSql(); return parameters; } }
        }


        public SqlBuilder()
        {
        }

#if CSHARP30
        public Template AddTemplate(string sql, object parameters)
#else
        public Template AddTemplate(string sql, dynamic parameters = null)
#endif
        {
            return new Template(this, sql, parameters);
        }

#if CSHARP30
        protected void AddClause(string name, string sql, object parameters, string joiner, string prefix, string postfix, bool isInclusive)
#else
        protected void AddClause(string name, string sql, object parameters, string joiner, string prefix = "", string postfix = "", bool isInclusive = false)
#endif
        {
            Clauses clauses;
            if (!data.TryGetValue(name, out clauses))
            {
                clauses = new Clauses(joiner, prefix, postfix);
                data[name] = clauses;
            }
            clauses.Add(new Clause { Sql = sql, Parameters = parameters, IsInclusive = isInclusive });
            seq++;
        }
        
#if CSHARP30
        public SqlBuilder Intersect(string sql, object parameters)
#else
        public SqlBuilder Intersect(string sql, dynamic parameters = null)
#endif
        {
            AddClause("intersect", sql, parameters, "\nINTERSECT\n ", "\n ", "\n", false);
            return this;
        }
        
#if CSHARP30
        public SqlBuilder InnerJoin(string sql, object parameters)
#else
        public SqlBuilder InnerJoin(string sql, dynamic parameters = null)
#endif
        {
            AddClause("innerjoin", sql, parameters, "\nINNER JOIN ", "\nINNER JOIN ", "\n", false);
            return this;
        }
        
#if CSHARP30
        public SqlBuilder LeftJoin(string sql, object parameters)
#else
        public SqlBuilder LeftJoin(string sql, dynamic parameters = null)
#endif
        {
            AddClause("leftjoin", sql, parameters, "\nLEFT JOIN ", "\nLEFT JOIN ", "\n", false);
            return this;
        }

        
#if CSHARP30
        public SqlBuilder RightJoin(string sql, object parameters)
#else
        public SqlBuilder RightJoin(string sql, dynamic parameters = null)
#endif
        {
            AddClause("rightjoin", sql, parameters, "\nRIGHT JOIN ", "\nRIGHT JOIN ", "\n", false);
            return this;
        }

        
#if CSHARP30
        public SqlBuilder Where(string sql, object parameters)
#else
        public SqlBuilder Where(string sql, dynamic parameters = null)
#endif
        {
            AddClause("where", sql, parameters, " AND ", "WHERE ", "\n", false);
            return this;
        }
        
#if CSHARP30
        public SqlBuilder OrWhere(string sql, object parameters)
#else
        public SqlBuilder OrWhere(string sql, dynamic parameters = null)
#endif
        {
            AddClause("where", sql, parameters, " AND ", "WHERE ", "\n", true);
            return this;
        }
        
#if CSHARP30
        public SqlBuilder OrderBy(string sql, object parameters)
#else
        public SqlBuilder OrderBy(string sql, dynamic parameters = null)
#endif
        {
            AddClause("orderby", sql, parameters, " , ", "ORDER BY ", "\n", false);
            return this;
        }
        
#if CSHARP30
        public SqlBuilder Select(string sql, object parameters)
#else
        public SqlBuilder Select(string sql, dynamic parameters = null)
#endif
        {
            AddClause("select", sql, parameters, " , ", "", "\n", false);
            return this;
        }
        
#if CSHARP30
        public SqlBuilder AddParameters(object parameters)
#else
        public SqlBuilder AddParameters(dynamic parameters)
#endif
        {
            AddClause("--parameters", "", parameters, "", "", "", false);
            return this;
        }
        
#if CSHARP30
        public SqlBuilder Join(string sql, object parameters)
#else
        public SqlBuilder Join(string sql, dynamic parameters = null)
#endif
        {
            AddClause("join", sql, parameters, "\nJOIN ", "\nJOIN ", "\n", false);
            return this;
        }
        
#if CSHARP30
        public SqlBuilder GroupBy(string sql, object parameters)
#else
        public SqlBuilder GroupBy(string sql, dynamic parameters = null)
#endif
        {
            AddClause("groupby", sql, parameters, " , ", "\nGROUP BY ", "\n", false);
            return this;
        }
        
#if CSHARP30
        public SqlBuilder Having(string sql, object parameters)
#else
        public SqlBuilder Having(string sql, dynamic parameters = null)
#endif
        {
            AddClause("having", sql, parameters, "\nAND ", "HAVING ", "\n", false);
            return this;
        }
    }
}
