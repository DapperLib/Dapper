using System;
using System.ComponentModel;
using System.Linq;
using BenchmarkDotNet.Attributes;
using DevExpress.Data.Access;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using nextorm.core;
using nextorm.sqlserver;

namespace Dapper.Tests.Performance;

[Description("Nextorm")]
public class NextormBenchmarks : BenchmarkBase
{
    private NextormRepository _repository;
    private QueryCommand<Post> _getPostByIdCompiled;
    private QueryCommand<Post> _getPostById;
    private QueryCommand<Post> _queryBufferedCompiled;
    private QueryCommand<Post> _queryUnbufferedCompiled;

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
            builder.LogSensetiveData(true);
        }

        _repository = new NextormRepository(builder);

        var cmdBuilder = _repository.Posts.Where(it => it.Id == NORM.Param<int>(0));
        _queryBufferedCompiled = cmdBuilder.ToCommand().Compile();
        _queryUnbufferedCompiled = cmdBuilder.ToCommand().Compile(false);
        _getPostById = cmdBuilder.FirstOrFirstOrDefaultCommand();
        _getPostByIdCompiled = _getPostById.Compile();
    }
    [Benchmark(Description = "First")]
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
        return _repository.Posts.Where(it => it.Id == i).AsEnumerable().FirstOrDefault();
    }
    [Benchmark(Description = "First with param")]
    public Post FirstParam()
    {
        Step();
        return _getPostById.FirstOrDefault(i);
    }

    [Benchmark(Description = "First compiled")]
    public Post FirstCompiled()
    {
        Step();
        return _getPostByIdCompiled.FirstOrDefault(i);
    }
    [Benchmark(Description = "Query<T> (compiled buffered)")]
    public Post QueryBufferedCompiled()
    {
        Step();
        return _queryBufferedCompiled.ToList(i).FirstOrDefault();
    }
    [Benchmark(Description = "Query<T> (compiled unbuffered)")]
    public Post QueryUnbufferedCompiled()
    {
        Step();
        return _queryUnbufferedCompiled.AsEnumerable(i).FirstOrDefault();
    }
}

public class NextormRepository
{
    public NextormRepository(DbContextBuilder builder) : this(builder.CreateDbContext())
    {
    }
    public NextormRepository(IDataContext dataContext)
    {
        Posts = dataContext.Create<Post>(config => config.Table("posts"));
    }

    public Entity<Post> Posts { get; set; }
}
