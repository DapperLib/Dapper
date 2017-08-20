using BenchmarkDotNet.Attributes;
using Dapper.Tests.Performance.NHibernate;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.Transform;
using NHibernate.Util;
using System.Linq;

namespace Dapper.Tests.Performance
{
    public class NHibernateBenchmarks : BenchmarkBase
    {
        private IStatelessSession _sql, _hql, _criteria, _linq, _get;

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            _sql = NHibernateHelper.OpenSession();
            _hql = NHibernateHelper.OpenSession();
            _criteria = NHibernateHelper.OpenSession();
            _linq = NHibernateHelper.OpenSession();
            _get = NHibernateHelper.OpenSession();
        }

        [Benchmark(Description = "SQL", OperationsPerInvoke = Iterations)]
        public Post SQL()
        {
            Step();
            return _sql.CreateSQLQuery(@"select * from Posts where Id = :id")
                .SetInt32("id", i)
                .SetResultTransformer(Transformers.AliasToBean<Post>())
                .List<Post>()[0];
        }

        [Benchmark(Description = "HQL", OperationsPerInvoke = Iterations)]
        public Post HQL()
        {
            Step();
            return _hql.CreateQuery(@"from Post as p where p.Id = :id")
                .SetInt32("id", i)
                .List<Post>()[0];
        }

        [Benchmark(Description = "Criteria", OperationsPerInvoke = Iterations)]
        public Post Criteria()
        {
            Step();
            return _criteria.CreateCriteria<Post>()
                .Add(Restrictions.IdEq(i))
                .List<Post>()[0];
        }

        [Benchmark(Description = "LINQ", OperationsPerInvoke = Iterations)]
        public Post LINQ()
        {
            Step();
            return _linq.Query<Post>().First(p => p.Id == i);
        }

        [Benchmark(Description = "Get<T>", OperationsPerInvoke = Iterations)]
        public Post Get()
        {
            Step();
            return _get.Get<Post>(i);
        }
    }
}