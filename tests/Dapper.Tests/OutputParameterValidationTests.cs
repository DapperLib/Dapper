using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Dapper.Tests;

public class OutputParameterValidationTests
{
    [Fact]
    public void Output_RejectsAnonymousPropertyWithHelpfulError()
    {
        var target = new { TotalCount = 0 };
        var args = new DynamicParameters(target);
        var error = Assert.Throws<InvalidOperationException>(() => args.Output(target, x => x.TotalCount));
        Assert.Contains("TotalCount", error.Message);
        Assert.Contains("setter", error.Message);
    }

    [Fact]
    public void Output_RejectsGetterOnlyPropertyWithHelpfulError()
    {
        var target = new ReadOnlyTarget();
        var error = Assert.Throws<InvalidOperationException>(() => new DynamicParameters().Output(target, x => x.TotalCount));
        Assert.Contains("TotalCount", error.Message);
        Assert.Contains("setter", error.Message);
    }

    [Fact]
    public void Output_RejectsNestedGetterOnlyPropertyWithHelpfulError()
    {
        var target = new { Child = new ReadOnlyTarget() };
        var error = Assert.Throws<InvalidOperationException>(() => new DynamicParameters().Output(target, x => x.Child.TotalCount));
        Assert.Contains("TotalCount", error.Message);
        Assert.Contains("setter", error.Message);
    }

    [Fact]
    public void Output_PopulatesPublicSetter()
    {
        var target = new WritableTarget();
        var args = new DynamicParameters(target).Output(target, x => x.Value);
        Complete(args, "Value");
        Assert.Equal(42, target.Value);
    }

    [Fact]
    public void Output_PopulatesPrivateSetter()
    {
        var target = new PrivateSetterTarget();
        var args = new DynamicParameters(target).Output(target, x => x.Value);
        Complete(args, "Value");
        Assert.Equal(42, target.Value);
    }

    [Fact]
    public void Output_PopulatesNestedPropertyThroughGetterOnlyParent()
    {
        var target = new { Child = new WritableTarget() };
        var args = new DynamicParameters(new { }).Output(target, x => x.Child.Value);
        Complete(args, "ChildValue");
        Assert.Equal(42, target.Child.Value);
    }

    [Fact]
    public void Output_PopulatesField()
    {
        var target = new FieldTarget();
        var args = new DynamicParameters(new { }).Output(target, x => x.Value);
        Complete(args, "Value");
        Assert.Equal(42, target.Value);
    }

    private static void Complete(DynamicParameters args, string name)
    {
        using var command = new SqlCommand();
        using var connection = new SqlConnection();
        var identity = new SqlMapper.Identity("set @" + name + " = 42", CommandType.Text, connection, typeof(int), null);
        ((SqlMapper.IDynamicParameters)args).AddParameters(command, identity);
        command.Parameters[name].Value = 42;
        ((SqlMapper.IParameterCallbacks)args).OnCompleted();
    }

    private class ReadOnlyTarget { public int TotalCount => 0; }
    private class WritableTarget { public int Value { get; set; } }
    private class PrivateSetterTarget { public int Value { get; private set; } }
    private class FieldTarget { public int Value = -1; }
}
