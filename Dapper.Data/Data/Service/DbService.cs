
namespace Dapper.Data.Service
{
	public interface IDbService
	{
		IDbContext Db { get; }
	}

	public abstract class DbService : IDbService
	{
		public IDbContext Db
		{ get; private set; }


		protected DbService(IDbContext db)
		{ Db = db; }
	}


}
