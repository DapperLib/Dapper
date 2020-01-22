using System;
#if NET4X
using Soma.Core;
#endif

namespace Dapper.Tests.Performance
{
    [ServiceStack.DataAnnotations.Alias("Posts")]
#if NET4X
    [Table(Name = "Posts")]
#endif
    [LinqToDB.Mapping.Table(Name = "Posts")]
    public class Post
    {
#if NET4X
        [Id(IdKind.Identity)]
#endif
        [LinqToDB.Mapping.PrimaryKey, LinqToDB.Mapping.Identity]
        public int Id { get; set; }
        [LinqToDB.Mapping.Column, LinqToDB.Mapping.Nullable]
        public string Text { get; set; }
        [LinqToDB.Mapping.Column, LinqToDB.Mapping.NotNull]
        public DateTime CreationDate { get; set; }
        [LinqToDB.Mapping.Column, LinqToDB.Mapping.NotNull]
        public DateTime LastChangeDate { get; set; }
        [LinqToDB.Mapping.Column, LinqToDB.Mapping.Nullable]
        public int? Counter1 { get; set; }
        [LinqToDB.Mapping.Column, LinqToDB.Mapping.Nullable]
        public int? Counter2 { get; set; }
        [LinqToDB.Mapping.Column, LinqToDB.Mapping.Nullable]
        public int? Counter3 { get; set; }
        [LinqToDB.Mapping.Column, LinqToDB.Mapping.Nullable]
        public int? Counter4 { get; set; }
        [LinqToDB.Mapping.Column, LinqToDB.Mapping.Nullable]
        public int? Counter5 { get; set; }
        [LinqToDB.Mapping.Column, LinqToDB.Mapping.Nullable]
        public int? Counter6 { get; set; }
        [LinqToDB.Mapping.Column, LinqToDB.Mapping.Nullable]
        public int? Counter7 { get; set; }
        [LinqToDB.Mapping.Column, LinqToDB.Mapping.Nullable]
        public int? Counter8 { get; set; }
        [LinqToDB.Mapping.Column, LinqToDB.Mapping.Nullable]
        public int? Counter9 { get; set; }
    }
}
