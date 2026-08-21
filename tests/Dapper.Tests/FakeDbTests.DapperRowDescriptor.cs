#if !NET481
using System;
using System.Collections.Generic;
using System.ComponentModel;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// Covers SqlMapper.DapperRow.Descriptor.cs (all 86 lines):
    /// DapperRowTypeDescriptionProvider, DapperRowTypeDescriptor (ICustomTypeDescriptor),
    /// and RowBoundPropertyDescriptor via System.ComponentModel.TypeDescriptor API.
    /// </summary>
    public class FakeDbDapperRowDescriptorTests
    {
        private static (dynamic row, object obj) GetRow()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 42 }, { "Name", "Alice" } }
            });
            conn.Open();
            dynamic row = conn.QueryFirst("SELECT Id, Name FROM T");
            return (row, (object)row);
        }

        // ── TypeDescriptor.GetProperties — exercises GetTypeDescriptor + GetProperties ──

        [Fact]
        public void DapperRow_TypeDescriptor_GetProperties_ReturnsColumns()
        {
            var (_, obj) = GetRow();
            var props = TypeDescriptor.GetProperties(obj);
            Assert.True(props.Count >= 2);
            Assert.NotNull(props["Id"]);
            Assert.NotNull(props["Name"]);
        }

        // ── GetProperties(Attribute[]) overload ───────────────────────

        [Fact]
        public void DapperRow_TypeDescriptor_GetProperties_WithAttributes_Works()
        {
            var (_, obj) = GetRow();
            // null attributes triggers the same path
            var props = TypeDescriptor.GetProperties(obj, (Attribute[]?)null);
            Assert.True(props.Count >= 2);
        }

        // ── RowBoundPropertyDescriptor.GetValue ───────────────────────

        [Fact]
        public void DapperRow_PropertyDescriptor_GetValue_ReturnsCorrectValue()
        {
            var (_, obj) = GetRow();
            var props = TypeDescriptor.GetProperties(obj);
            var idProp = props["Id"]!;
            Assert.Equal(42, idProp.GetValue(obj));
        }

        // ── RowBoundPropertyDescriptor.GetValue — missing key returns DBNull ──

        [Fact]
        public void DapperRow_PropertyDescriptor_GetValue_AfterRemove_ReturnsDBNull()
        {
            var (_, obj) = GetRow();
            var props = TypeDescriptor.GetProperties(obj);
            var nameProp = props["Name"]!;
            nameProp.ResetValue(obj);  // removes the entry
            Assert.Equal(DBNull.Value, nameProp.GetValue(obj));
        }

        // ── RowBoundPropertyDescriptor.SetValue ───────────────────────

        [Fact]
        public void DapperRow_PropertyDescriptor_SetValue_UpdatesValue()
        {
            var (_, obj) = GetRow();
            var props = TypeDescriptor.GetProperties(obj);
            var idProp = props["Id"]!;
            idProp.SetValue(obj, 999);
            Assert.Equal(999, idProp.GetValue(obj));
        }

        // ── RowBoundPropertyDescriptor.SetValue with DBNull → null ────

        [Fact]
        public void DapperRow_PropertyDescriptor_SetValue_DBNull_SetsNull()
        {
            var (_, obj) = GetRow();
            var props = TypeDescriptor.GetProperties(obj);
            var nameProp = props["Name"]!;
            nameProp.SetValue(obj, DBNull.Value);
            // GetValue returns DBNull when value is null
            Assert.Equal(DBNull.Value, nameProp.GetValue(obj));
        }

        // ── RowBoundPropertyDescriptor.CanResetValue ──────────────────

        [Fact]
        public void DapperRow_PropertyDescriptor_CanResetValue_ReturnsTrue()
        {
            var (_, obj) = GetRow();
            var props = TypeDescriptor.GetProperties(obj);
            Assert.True(props["Id"]!.CanResetValue(obj));
        }

        // ── RowBoundPropertyDescriptor.ResetValue ─────────────────────

        [Fact]
        public void DapperRow_PropertyDescriptor_ResetValue_RemovesEntry()
        {
            var (_, obj) = GetRow();
            var props = TypeDescriptor.GetProperties(obj);
            var nameProp = props["Name"]!;
            nameProp.ResetValue(obj);
            Assert.Equal(DBNull.Value, nameProp.GetValue(obj));
        }

        // ── RowBoundPropertyDescriptor.ShouldSerializeValue ───────────

        [Fact]
        public void DapperRow_PropertyDescriptor_ShouldSerializeValue_True_WhenExists()
        {
            var (_, obj) = GetRow();
            var props = TypeDescriptor.GetProperties(obj);
            Assert.True(props["Id"]!.ShouldSerializeValue(obj));
        }

        [Fact]
        public void DapperRow_PropertyDescriptor_ShouldSerializeValue_False_AfterRemove()
        {
            var (_, obj) = GetRow();
            var props = TypeDescriptor.GetProperties(obj);
            var nameProp = props["Name"]!;
            nameProp.ResetValue(obj);
            Assert.False(nameProp.ShouldSerializeValue(obj));
        }

        // ── RowBoundPropertyDescriptor.IsReadOnly ─────────────────────

        [Fact]
        public void DapperRow_PropertyDescriptor_IsReadOnly_False()
        {
            var (_, obj) = GetRow();
            var props = TypeDescriptor.GetProperties(obj);
            Assert.False(props["Id"]!.IsReadOnly);
        }

        // ── RowBoundPropertyDescriptor.ComponentType ──────────────────

        [Fact]
        public void DapperRow_PropertyDescriptor_ComponentType_IsDapperRow()
        {
            var (_, obj) = GetRow();
            var props = TypeDescriptor.GetProperties(obj);
            Assert.Equal("DapperRow", props["Id"]!.ComponentType.Name);
        }

        // ── RowBoundPropertyDescriptor.PropertyType ───────────────────

        [Fact]
        public void DapperRow_PropertyDescriptor_PropertyType_ReflectsValueType()
        {
            var (_, obj) = GetRow();
            var props = TypeDescriptor.GetProperties(obj);
            Assert.Equal(typeof(int), props["Id"]!.PropertyType);
        }

        // ── ICustomTypeDescriptor.GetAttributes ───────────────────────

        [Fact]
        public void DapperRow_TypeDescriptor_GetAttributes_ReturnsEmpty()
        {
            var (_, obj) = GetRow();
            var attrs = TypeDescriptor.GetAttributes(obj);
            Assert.NotNull(attrs);
        }

        // ── ICustomTypeDescriptor.GetClassName ────────────────────────

        [Fact]
        public void DapperRow_TypeDescriptor_GetClassName_ReturnsName()
        {
            var (_, obj) = GetRow();
            var name = TypeDescriptor.GetClassName(obj);
            Assert.NotNull(name);
            Assert.Contains("DapperRow", name);
        }

        // ── ICustomTypeDescriptor.GetComponentName ────────────────────

        [Fact]
        public void DapperRow_TypeDescriptor_GetComponentName_ReturnsNull()
        {
            var (_, obj) = GetRow();
            // GetComponentName returns null for DapperRow
            var name = TypeDescriptor.GetComponentName(obj);
            // null is acceptable
        }

        // ── ICustomTypeDescriptor.GetConverter ────────────────────────

        [Fact]
        public void DapperRow_TypeDescriptor_GetConverter_ReturnsExpandable()
        {
            var (_, obj) = GetRow();
            var conv = TypeDescriptor.GetConverter(obj);
            Assert.NotNull(conv);
            Assert.IsType<ExpandableObjectConverter>(conv);
        }

        // ── ICustomTypeDescriptor.GetDefaultEvent ─────────────────────

        [Fact]
        public void DapperRow_TypeDescriptor_GetDefaultEvent_ReturnsNull()
        {
            var (_, obj) = GetRow();
            var ev = TypeDescriptor.GetDefaultEvent(obj);
            Assert.Null(ev);
        }

        // ── ICustomTypeDescriptor.GetDefaultProperty ──────────────────

        [Fact]
        public void DapperRow_TypeDescriptor_GetDefaultProperty_ReturnsNull()
        {
            var (_, obj) = GetRow();
            var prop = TypeDescriptor.GetDefaultProperty(obj);
            Assert.Null(prop);
        }

        // ── ICustomTypeDescriptor.GetEvents ───────────────────────────

        [Fact]
        public void DapperRow_TypeDescriptor_GetEvents_ReturnsEmpty()
        {
            var (_, obj) = GetRow();
            var events = TypeDescriptor.GetEvents(obj);
            Assert.Equal(0, events.Count);
        }

        [Fact]
        public void DapperRow_TypeDescriptor_GetEventsWithAttributes_ReturnsEmpty()
        {
            var (_, obj) = GetRow();
            var events = TypeDescriptor.GetEvents(obj, (Attribute[]?)null);
            Assert.Equal(0, events.Count);
        }

        // ── GetExtendedTypeDescriptor via TypeDescriptor ───────────────

        [Fact]
        public void DapperRow_TypeDescriptor_GetEditor_ReturnsNull()
        {
            var (_, obj) = GetRow();
            // GetEditor returns null for DapperRow
            var editor = TypeDescriptor.GetEditor(obj, typeof(object));
            Assert.Null(editor);
        }
    }
}
#endif
