using BenchmarkDotNet.Attributes;

using System;
using System.Linq;
using System.ComponentModel;
using DevExpress.Xpo;
using DevExpress.Data.Filtering;

namespace Dapper.Tests.Performance
{
    [Description("DevExpress.XPO")]
    public class XpoBenchmarks : BenchmarkBase
    {
        public UnitOfWork _session;

        [GlobalSetup]
        public void Setup()
        {
            BaseSetup();
            IDataLayer dataLayer = XpoDefault.GetDataLayer(_connection, DevExpress.Xpo.DB.AutoCreateOption.SchemaAlreadyExists);
            dataLayer.Dictionary.GetDataStoreSchema(typeof(Xpo.Post));
            _session = new UnitOfWork(dataLayer, dataLayer);
            _session.IdentityMapBehavior = IdentityMapBehavior.Strong;
            _session.TypesManager.EnsureIsTypedObjectValid();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _session.Dispose();
        }

        [Benchmark(Description = "GetObjectByKey<T>")]
        public Xpo.Post GetObjectByKey()
        {
            Step();
            return _session.GetObjectByKey<Xpo.Post>(i, true);
        }

        [Benchmark(Description = "FindObject<T>")]
        public Xpo.Post FindObject()
        {
            Step();
            CriteriaOperator _findCriteria = new BinaryOperator()
            {
                OperatorType = BinaryOperatorType.Equal,
                LeftOperand = new OperandProperty("Id"),
                RightOperand = new ConstantValue(i)
            };
            return _session.FindObject<Xpo.Post>(_findCriteria);
        }

        [Benchmark(Description = "Query<T>")]
        public Xpo.Post Query()
        {
            Step();
            return _session.Query<Xpo.Post>().First(p => p.Id == i);
        }
    }
}
