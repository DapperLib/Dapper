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

    // private QueryCommand<Post> _getPosts;
    // private QueryCommand<Post> _getPostsStream;

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

        var c = _repository.Posts.Where(it => it.Id == NORM.Param<int>(0));
        _queryBufferedCompiled = c.ToCommand().Compile();
        _queryUnbufferedCompiled = c.ToCommand().Compile(false);
        _getPostByIdCompiled = c.FirstOrFirstOrDefaultCommand().Compile();
        _getPostById = _repository.Posts.Where(it => it.Id == NORM.Param<int>(0)).FirstOrFirstOrDefaultCommand();
        // _getPosts = _repository.Posts.Limit(QueryLimit).ToCommand().Compile(true);
        // _getPostsStream = _repository.Posts.Limit(QueryLimit).ToCommand().Compile(false);
        //Console.WriteLine("Setup complete");
    }
    [Benchmark(Description = "First")]
    public Post First()
    {
        Step();
        return _repository.Posts.Where(it => it.Id == i).FirstOrDefault();
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
    // [Benchmark(Description = "Query<T> (buffered) compiled")]
    // public Post CompiledQueryBuffered()
    // {
    //     return _getPosts.ToList().FirstOrDefault();
    // }
    // [Benchmark(Description = "Query<T> (unbuffered) compiled")]
    // public Post CompiledQueryUnbuffered()
    // {
    //     foreach (var p in _getPostsStream.AsEnumerable())
    //         return p;

    //     return null;
    // }
    // [Benchmark(Description = "Query<T> (buffered)")]
    // public Post QueryBuffered()
    // {
    //     return _repository.Posts.Limit(QueryLimit).ToList().First();
    // }
    // [Benchmark(Description = "Query<T> (unbuffered)")]
    // public Post QueryUnbuffered()
    // {
    //     foreach (var p in _repository.Posts.Limit(QueryLimit).ToCommand().AsEnumerable())
    //         return p;

    //     return null;
    // }
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
