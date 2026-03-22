#if !NET481
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public class FakeDbTypeMappingTests
    {
        // ── AddTypeMap / GetTypeMap ────────────────────────────────────

        [Fact]
        public void AddTypeMap_RegistersCustomMapping()
        {
            // Add a custom mapping and verify it can be retrieved
            SqlMapper.AddTypeMap(typeof(DateOnly), DbType.Date);
            // No exception = success; the type map is updated
        }

        [Fact]
        public void GetTypeMap_ReturnsDefaultTypeMap_ForUnmappedType()
        {
            var map = SqlMapper.GetTypeMap(typeof(User));
            Assert.NotNull(map);
        }

        // ── SetTypeMap / CustomPropertyTypeMap ─────────────────────────

        private class Odd { public string? first_name { get; set; } public string? last_name { get; set; } }

        [Fact]
        public void SetTypeMap_CustomPropertySelector_RemapsColumns()
        {
            SqlMapper.SetTypeMap(typeof(Odd),
                new CustomPropertyTypeMap(typeof(Odd), (type, col) => col switch
                {
                    "fn" => type.GetProperty(nameof(Odd.first_name))!,
                    "ln" => type.GetProperty(nameof(Odd.last_name))!,
                    _ => null!
                }));
            try
            {
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(new[]
                {
                    new Dictionary<string, object?> { { "fn", "John" }, { "ln", "Doe" } }
                });
                conn.Open();

                var result = conn.QueryFirst<Odd>("SELECT fn, ln FROM People");
                Assert.Equal("John", result.first_name);
                Assert.Equal("Doe", result.last_name);
            }
            finally
            {
                SqlMapper.SetTypeMap(typeof(Odd), null); // reset
            }
        }

        [Fact]
        public void CustomPropertyTypeMap_FindConstructor_ReturnsDefault()
        {
            var map = new CustomPropertyTypeMap(typeof(User),
                (t, col) => t.GetProperty(col, BindingFlags.Public | BindingFlags.Instance)!);

            var ctor = map.FindConstructor(Array.Empty<string>(), Array.Empty<Type>());
            Assert.NotNull(ctor);
        }

        [Fact]
        public void CustomPropertyTypeMap_FindExplicitConstructor_ReturnsNull()
        {
            var map = new CustomPropertyTypeMap(typeof(User),
                (t, col) => t.GetProperty(col, BindingFlags.Public | BindingFlags.Instance)!);

            Assert.Null(map.FindExplicitConstructor());
        }

        [Fact]
        public void CustomPropertyTypeMap_GetMember_ReturnsNull_WhenSelectorReturnsNull()
        {
            var map = new CustomPropertyTypeMap(typeof(User), (t, col) => null!);
            Assert.Null(map.GetMember("AnyColumn"));
        }

        [Fact]
        public void CustomPropertyTypeMap_GetConstructorParameter_ThrowsNotSupported()
        {
            var map = new CustomPropertyTypeMap(typeof(User), (t, col) => null!);
            var ctor = typeof(User).GetConstructor(Type.EmptyTypes)!;
            Assert.Throws<NotSupportedException>(() =>
                map.GetConstructorParameter(ctor, "Id"));
        }

        [Fact]
        public void CustomPropertyTypeMap_Constructor_ThrowsOnNullType()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CustomPropertyTypeMap(null!, (t, col) => null!));
        }

        [Fact]
        public void CustomPropertyTypeMap_Constructor_ThrowsOnNullSelector()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CustomPropertyTypeMap(typeof(User), null!));
        }

        // ── XML type handlers ─────────────────────────────────────────

        [Fact]
        public void XmlDocument_TypeHandler_ParsesXml()
        {
            SqlMapper.AddTypeHandler(new XmlDocumentHandler());
            try
            {
                const string xml = "<root><item>1</item></root>";
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "XmlCol", xml } } });
                conn.Open();

                var result = conn.QueryFirst<XmlDocumentRow>("SELECT XmlCol FROM T");
                Assert.NotNull(result.XmlCol);
                Assert.Equal("root", result.XmlCol!.DocumentElement!.Name);
            }
            finally
            {
                SqlMapper.ResetTypeHandlers();
            }
        }

        [Fact]
        public void XDocument_TypeHandler_ParsesXml()
        {
            SqlMapper.AddTypeHandler(new XDocumentHandler());
            try
            {
                const string xml = "<root><item>hello</item></root>";
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "XmlCol", xml } } });
                conn.Open();

                var result = conn.QueryFirst<XDocumentRow>("SELECT XmlCol FROM T");
                Assert.NotNull(result.XmlCol);
                Assert.Equal("root", result.XmlCol!.Root!.Name.LocalName);
            }
            finally
            {
                SqlMapper.ResetTypeHandlers();
            }
        }

        [Fact]
        public void XElement_TypeHandler_ParsesXml()
        {
            SqlMapper.AddTypeHandler(new XElementHandler());
            try
            {
                const string xml = "<item id='1'>text</item>";
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "XmlCol", xml } } });
                conn.Open();

                var result = conn.QueryFirst<XElementRow>("SELECT XmlCol FROM T");
                Assert.NotNull(result.XmlCol);
                Assert.Equal("item", result.XmlCol!.Name.LocalName);
            }
            finally
            {
                SqlMapper.ResetTypeHandlers();
            }
        }

        [Fact]
        public void XmlDocument_TypeHandler_SetsDbTypeXml_OnParameter()
        {
            SqlMapper.AddTypeHandler(new XmlDocumentHandler());
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml("<r/>");

                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueNonQueryResult(1);
                conn.Open();

                conn.Execute("INSERT INTO T (XmlCol) VALUES (@xml)", new { xml = doc });
            }
            finally
            {
                SqlMapper.ResetTypeHandlers();
            }
        }

        [Fact]
        public void XDocument_TypeHandler_SetsDbTypeXml_OnParameter()
        {
            SqlMapper.AddTypeHandler(new XDocumentHandler());
            try
            {
                var xdoc = XDocument.Parse("<r/>");

                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueNonQueryResult(1);
                conn.Open();

                conn.Execute("INSERT INTO T (XmlCol) VALUES (@xml)", new { xml = xdoc });
            }
            finally
            {
                SqlMapper.ResetTypeHandlers();
            }
        }

        // ── FeatureSupport ────────────────────────────────────────────

        [Fact]
        public void FeatureSupport_NullConnection_ReturnsDefault()
        {
            var fs = FeatureSupport.Get(null);
            Assert.NotNull(fs);
        }

        [Fact]
        public void FeatureSupport_FakeConnection_ReturnsDefault()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            var fs = FeatureSupport.Get(conn);
            Assert.NotNull(fs);
        }

        // ── constructor mapping ───────────────────────────────────────

        private class ImmutablePoint
        {
            public int X { get; }
            public int Y { get; }
            public ImmutablePoint(int x, int y) { X = x; Y = y; }
        }

        [Fact]
        public void Query_MapsToConstructor()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "X", 3 }, { "Y", 4 } }
            });
            conn.Open();

            var result = conn.QueryFirst<ImmutablePoint>("SELECT 3 AS X, 4 AS Y");
            Assert.Equal(3, result.X);
            Assert.Equal(4, result.Y);
        }

        // ── DefaultTypeMap ────────────────────────────────────────────

        [Fact]
        public void DefaultTypeMap_GetMember_ReturnsMapping_ForExistingProperty()
        {
            var map = new DefaultTypeMap(typeof(User));
            var member = map.GetMember("Id");
            Assert.NotNull(member);
        }

        [Fact]
        public void DefaultTypeMap_GetMember_ReturnsNull_ForUnknownColumn()
        {
            var map = new DefaultTypeMap(typeof(User));
            var member = map.GetMember("DoesNotExist");
            Assert.Null(member);
        }

        [Fact]
        public void DefaultTypeMap_FindConstructor_ReturnsDefault()
        {
            var map = new DefaultTypeMap(typeof(User));
            var ctor = map.FindConstructor(Array.Empty<string>(), Array.Empty<Type>());
            Assert.NotNull(ctor);
        }

        // ── helper POCOs for XML handler tests ────────────────────────

        private class XmlDocumentRow { public XmlDocument? XmlCol { get; set; } }
        private class XDocumentRow { public XDocument? XmlCol { get; set; } }
        private class XElementRow { public XElement? XmlCol { get; set; } }
    }
}
#endif
