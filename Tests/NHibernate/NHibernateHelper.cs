using System.Data;
using NHibernate;
using NHibernate.Cfg;

namespace SqlMapper.NHibernate
{
    public class NHibernateHelper
    {
        private static ISessionFactory _sessionFactory;

        private static ISessionFactory SessionFactory
        {
            get
            {
                if (_sessionFactory == null)
                {
                    var configuration = new Configuration();
                    configuration.Configure(@"..\..\NHibernate\hibernate.cfg.xml");
                    configuration.AddAssembly(typeof(Post).Assembly);
                    configuration.AddXmlFile(@"..\..\NHibernate\Post.hbm.xml");
                    _sessionFactory = configuration.BuildSessionFactory();
                }

                return _sessionFactory;
            }
        }

        public static IStatelessSession OpenSession()
        {
            return SessionFactory.OpenStatelessSession();
        }
    }
}
