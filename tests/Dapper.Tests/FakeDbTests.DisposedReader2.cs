#if !NET481
using System;
using System.Collections;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Additional DisposedReader coverage for methods not yet tested.
    /// </summary>
    public class FakeDbDisposedReader2Tests
    {
        [Fact]
        public void DisposedReader_Depth_ReturnsZero()
            => Assert.Equal(0, DisposedReader.Instance.Depth);

        [Fact]
        public void DisposedReader_FieldCount_ReturnsZero()
            => Assert.Equal(0, DisposedReader.Instance.FieldCount);

        [Fact]
        public void DisposedReader_IsClosed_ReturnsTrue()
            => Assert.True(DisposedReader.Instance.IsClosed);

        [Fact]
        public void DisposedReader_HasRows_ReturnsFalse()
            => Assert.False(DisposedReader.Instance.HasRows);

        [Fact]
        public void DisposedReader_RecordsAffected_ReturnsMinusOne()
            => Assert.Equal(-1, DisposedReader.Instance.RecordsAffected);

        [Fact]
        public void DisposedReader_VisibleFieldCount_ReturnsZero()
            => Assert.Equal(0, DisposedReader.Instance.VisibleFieldCount);

        [Fact]
        public void DisposedReader_Close_DoesNotThrow()
        {
            // Close is a no-op — should not throw
            DisposedReader.Instance.Close();
        }

        [Fact]
        public void DisposedReader_GetSchemaTable_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetSchemaTable());

        [Fact]
        public void DisposedReader_GetEnumerator_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetEnumerator());

        [Fact]
        public void DisposedReader_GetFieldValue_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetFieldValue<int>(0));

        [Fact]
        public void DisposedReader_GetProviderSpecificFieldType_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetProviderSpecificFieldType(0));

        [Fact]
        public void DisposedReader_GetProviderSpecificValue_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetProviderSpecificValue(0));

        [Fact]
        public void DisposedReader_GetProviderSpecificValues_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetProviderSpecificValues(Array.Empty<object>()));

        [Fact]
        public void DisposedReader_GetChar_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetChar(0));

        [Fact]
        public void DisposedReader_GetBytes_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetBytes(0, 0, null, 0, 0));

        [Fact]
        public void DisposedReader_GetChars_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.GetChars(0, 0, null, 0, 0));

        [Fact]
        public void DisposedReader_IndexerByInt_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance[0]);

        [Fact]
        public void DisposedReader_IndexerByString_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance["x"]);

        [Fact]
        public async Task DisposedReader_NextResultAsync_ThrowsObjectDisposedException()
        {
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                DisposedReader.Instance.NextResultAsync(CancellationToken.None));
        }

        [Fact]
        public void DisposedReader_NextResult_Throws()
            => Assert.Throws<ObjectDisposedException>(() => DisposedReader.Instance.NextResult());
    }
}
#endif
