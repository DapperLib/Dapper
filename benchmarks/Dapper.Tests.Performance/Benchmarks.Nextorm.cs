#if !NET4X
using System.ComponentModel;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using nextorm.core;
using nextorm.sqlserver;

namespace Dapper.Tests.Performance;

[Description("Nextorm")]
public class NextormBenchmarks : BenchmarkBase
{
    private NextormRepository _repository;
    private IPreparedQueryCommand<Post> _getPostByIdPrepared;
    private QueryCommand<Post> _getPostById;
    private IPreparedQueryCommand<Post> _queryBufferedPrepared;
    private IPreparedQueryCommand<Post> _queryUnbufferedPrepared;

    [GlobalSetup]
    public void GlobalSetup() => Setup(false);
    public void Setup(bool withLogging)
    {
        BaseSetup();
        var builder = new DbContextBuilder();
        builder.UseSqlServer(_connection);
        if (withLogging)
        {
            var logFactory = LoggerFactory.Create(config => config.AddConsole().SetMinimumLevel(LogLevel.Debug));
            builder.UseLoggerFactory(logFactory);
            builder.LogSensitiveData(true);
        }

        _repository = new NextormRepository(builder);

        var cmdBuilder = _repository.Posts.Where(it => it.Id == NORM.Param<int>(0));
        _queryBufferedPrepared = cmdBuilder.ToCommand().Prepare();
        _queryUnbufferedPrepared = cmdBuilder.ToCommand().Prepare(false);
        _getPostById = cmdBuilder.FirstOrFirstOrDefaultCommand();
        _getPostByIdPrepared = _getPostById.Prepare();
    }
    [Benchmark(Description = "QueryFirstOrDefault<T>")]
    public Post First()
    {
        Step();
        return _repository.Posts.Where(it => it.Id == i).FirstOrDefault();
    }
    [Benchmark(Description = "Query<T> (buffered)")]
    public Post QueryBuffered()
    {
        Step();
        return _repository.Posts.Where(it => it.Id == i).ToList().FirstOrDefault();
    }
    [Benchmark(Description = "Query<T> (unbuffered)")]
    public Post QueryUnbuffered()
    {
        Step();
        return _repository.Posts.Where(it => it.Id == i).ToEnumerable().FirstOrDefault();
    }
    [Benchmark(Description = "QueryFirstOrDefault<T> with param")]
    public Post FirstParam()
    {
        Step();
        return _getPostById.FirstOrDefault(i);
    }

    [Benchmark(Description = "QueryFirstOrDefault<T> prepared")]
    public Post FirstPrepared()
    {
        Step();
        return _getPostByIdPrepared.FirstOrDefault(_repository.DataContext, i);
    }
    [Benchmark(Description = "Query<T> (buffered prepared)")]
    public Post QueryBufferedPrepared()
    {
        Step();
        return _queryBufferedPrepared.ToList(_repository.DataContext, i).FirstOrDefault();
    }
    [Benchmark(Description = "Query<T> (unbuffered prepared)")]
    public Post QueryUnbufferedPrepared()
    {
        Step();
        return _queryUnbufferedPrepared.ToEnumerable(_repository.DataContext, i).FirstOrDefault();
    }
}

public class NextormRepository
{
    private readonly IDataContext _dataContext;

    public NextormRepository(DbContextBuilder builder) : this(builder.CreateDbContext())
    {
    }
    public NextormRepository(IDataContext dataContext)
    {
        Posts = dataContext.Create<Post>(config => config.Table("posts"));
        _dataContext = dataContext;
    }

    public Entity<Post> Posts { get; set; }
    public IDataContext DataContext => _dataContext;
}
#endif
