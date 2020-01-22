using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
#nullable enable
namespace Dapper.ProviderTools.Internal
{
    internal sealed class DynamicBulkCopy : BulkCopy
    {
        internal static BulkCopy? Create(object? wrapped)
            => wrapped == null ? null : new DynamicBulkCopy(wrapped);

        private DynamicBulkCopy(object wrapped)
            => _wrapped = wrapped;

        private readonly dynamic _wrapped;

        public override string DestinationTableName
        {
            get => _wrapped.DestinationTableName;
            set => _wrapped.DestinationTableName = value;
        }

        public override object Wrapped => _wrapped;

        public override void AddColumnMapping(string sourceColumn, string destinationColumn)
            => _wrapped.ColumnMappings.Add(sourceColumn, destinationColumn);

        public override void AddColumnMapping(int sourceColumn, int destinationColumn)
            => _wrapped.ColumnMappings.Add(sourceColumn, destinationColumn);

        public override void WriteToServer(DataTable source)
            => _wrapped.WriteToServer(source);
        public override void WriteToServer(DataRow[] source)
            => _wrapped.WriteToServer(source);

        public override void WriteToServer(IDataReader source)
            => _wrapped.WriteToServer(source);

        public override Task WriteToServerAsync(DbDataReader source, CancellationToken cancellationToken)
            => _wrapped.WriteToServer(source, cancellationToken);

        public override Task WriteToServerAsync(DataTable source, CancellationToken cancellationToken)
            => _wrapped.WriteToServer(source, cancellationToken);
        public override Task WriteToServerAsync(DataRow[] source, CancellationToken cancellationToken)
            => _wrapped.WriteToServer(source, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_wrapped is IDisposable d)
                {
                    try { d.Dispose(); } catch { }
                }
            }
        }
    }
}
