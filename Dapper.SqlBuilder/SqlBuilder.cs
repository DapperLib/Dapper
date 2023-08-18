using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dapper
{
    public class SqlBuilder
    {
        private readonly Dictionary<string, Clauses> _data = new Dictionary<string, Clauses>();
        private int _seq;

        private class Clause
        {
            public Clause(string sql, object? parameters, bool isInclusive)
            {
                Sql = sql;
                Parameters = parameters;
                IsInclusive = isInclusive;
            }
            public string Sql { get; }
            public object? Parameters { get; }
            public bool IsInclusive { get; }
        }

        private class Clauses : List<Clause>
        {
            private readonly string _joiner, _prefix, _postfix;

            public Clauses(string joiner, string prefix = "", string postfix = "")
            {
                _joiner = joiner;
                _prefix = prefix;
                _postfix = postfix;
            }

            public string ResolveClauses(DynamicParameters p)
            {
                foreach (var item in this)
                {
                    p.AddDynamicParams(item.Parameters);
                }
                return this.Any(a => a.IsInclusive)
                    ? _prefix +
                      string.Join(_joiner,
                          this.Where(a => !a.IsInclusive)
                              .Select(c => c.Sql)
                              .Union(new[]
                              {
                                  " ( " +
                                  string.Join(" OR ", this.Where(a => a.IsInclusive).Select(c => c.Sql).ToArray()) +
                                  " ) "
                              }).ToArray()) + _postfix
                    : _prefix + string.Join(_joiner, this.Select(c => c.Sql).ToArray()) + _postfix;
            }
        }

        public class Template
        {
            private readonly string _sql;
            private readonly SqlBuilder _builder;
            private readonly object? _initParams;
            private int _dataSeq = -1; // Unresolved

            public Template(SqlBuilder builder, string sql, dynamic? parameters)
            {
                _initParams = parameters;
                _sql = sql;
                _builder = builder;
            }

            private static readonly Regex _regex = new Regex(@"\/\*\*.+?\*\*\/", RegexOptions.Compiled | RegexOptions.Multiline);

            private void ResolveSql()
            {
                if (_dataSeq != _builder._seq)
                {
                    var p = new DynamicParameters(_initParams);

                    rawSql = _sql;

                    foreach (var pair in _builder._data)
                    {
                        rawSql = rawSql.Replace("/**" + pair.Key + "**/", pair.Value.ResolveClauses(p));
                    }
                    parameters = p;

                    // replace all that is left with empty
                    rawSql = _regex.Replace(rawSql, "");

                    _dataSeq = _builder._seq;
                }
            }

            private string? rawSql;
            private object? parameters;

            public string RawSql
            {
                get { ResolveSql(); return rawSql!; }
            }

            public object? Parameters
            {
                get { ResolveSql(); return parameters; }
            }
        }

        public Template AddTemplate(string sql, dynamic? parameters = null) =>
            new Template(this, sql, (object?)parameters);

        protected SqlBuilder AddClause(string name, string sql, object? parameters, string joiner, string prefix = "", string postfix = "", bool isInclusive = false)
        {
            if (!_data.TryGetValue(name, out var clauses))
            {
                clauses = new Clauses(joiner, prefix, postfix);
                _data[name] = clauses;
            }
            clauses.Add(new Clause(sql, parameters, isInclusive));
            _seq++;
            return this;
        }

        public SqlBuilder Intersect(string sql, dynamic? parameters = null) =>
            AddClause("intersect", sql, (object?)parameters, "\nINTERSECT\n ", "\n ", "\n", false);

        public SqlBuilder InnerJoin(string sql, dynamic? parameters = null) =>
            AddClause("innerjoin", sql, (object?)parameters, "\nINNER JOIN ", "\nINNER JOIN ", "\n", false);

        public SqlBuilder LeftJoin(string sql, dynamic? parameters = null) =>
            AddClause("leftjoin", sql, (object?)parameters, "\nLEFT JOIN ", "\nLEFT JOIN ", "\n", false);

        public SqlBuilder RightJoin(string sql, dynamic? parameters = null) =>
            AddClause("rightjoin", sql, (object?)parameters, "\nRIGHT JOIN ", "\nRIGHT JOIN ", "\n", false);

        public SqlBuilder Where(string sql, dynamic? parameters = null) =>
            AddClause("where", sql, (object?)parameters, " AND ", "WHERE ", "\n", false);

        public SqlBuilder OrWhere(string sql, dynamic? parameters = null) =>
            AddClause("where", sql, (object?)parameters, " OR ", "WHERE ", "\n", true);

        public SqlBuilder OrderBy(string sql, dynamic? parameters = null) =>
            AddClause("orderby", sql, (object?)parameters, " , ", "ORDER BY ", "\n", false);

        public SqlBuilder Select(string sql, dynamic? parameters = null) =>
            AddClause("select", sql, (object?)parameters, " , ", "", "\n", false);

        public SqlBuilder AddParameters(dynamic parameters) =>
            AddClause("--parameters", "", (object?)parameters, "", "", "", false);

        public SqlBuilder Join(string sql, dynamic? parameters = null) =>
            AddClause("join", sql, (object?)parameters, "\nJOIN ", "\nJOIN ", "\n", false);

        public SqlBuilder GroupBy(string sql, dynamic? parameters = null) =>
            AddClause("groupby", sql, (object?)parameters, " , ", "\nGROUP BY ", "\n", false);

        public SqlBuilder Having(string sql, dynamic? parameters = null) =>
            AddClause("having", sql, (object?)parameters, "\nAND ", "HAVING ", "\n", false);

        public SqlBuilder Set(string sql, dynamic? parameters = null) =>
             AddClause("set", sql, (object?)parameters, " , ", "SET ", "\n", false);

    }
}
