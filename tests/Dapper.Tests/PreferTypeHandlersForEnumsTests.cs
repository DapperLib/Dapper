using System;
using System.Data;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    [Collection(NonParallelDefinition.Name)]
    public sealed class SystemSqlClientPreferTypeHandlersForEnumsTests : PreferTypeHandlersForEnumsTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    [Collection(NonParallelDefinition.Name)]
    public sealed class MicrosoftSqlClientPreferTypeHandlersForEnumsTests : PreferTypeHandlersForEnumsTests<MicrosoftSqlClientProvider> { }
#endif

    public abstract class PreferTypeHandlersForEnumsTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
    {
        private enum Color
        {
            Red = 1,
            Green = 2,
            Blue = 3
        }

        private class ColorResult
        {
            public Color Value { get; set; }
        }

        private class NullableColorResult
        {
            public Color? Value { get; set; }
        }

        /// <summary>
        /// A TypeHandler that stores enum values as their name strings
        /// and parses them back from strings.
        /// </summary>
        private class StringEnumHandler<TEnum> : SqlMapper.TypeHandler<TEnum> where TEnum : struct, Enum
        {
            public static readonly StringEnumHandler<TEnum> Instance = new();
            public int ParseCallCount;
            public int SetValueCallCount;

            public override TEnum Parse(object? value)
            {
                ParseCallCount++;
                return (TEnum)Enum.Parse(typeof(TEnum), (string)value!);
            }

            public override void SetValue(IDbDataParameter parameter, TEnum value)
            {
                SetValueCallCount++;
                parameter.DbType = DbType.AnsiString;
                parameter.Value = value.ToString();
            }
        }

        [Fact]
        public void EnumTypeHandler_WriteAndRead_UsesHandlerWhenEnabled()
        {
            var handler = new StringEnumHandler<Color>();
            var oldSetting = SqlMapper.Settings.PreferTypeHandlersForEnums;
            try
            {
                SqlMapper.ResetTypeHandlers();
                SqlMapper.AddTypeHandler(typeof(Color), handler);
                SqlMapper.Settings.PreferTypeHandlersForEnums = true;
                SqlMapper.PurgeQueryCache();

                // Round-trip: write as string, read back via handler
                var result = connection.Query<ColorResult>(
                    "SELECT @Value AS Value", new { Value = Color.Green }).Single();

                Assert.Equal(Color.Green, result.Value);
                Assert.True(handler.SetValueCallCount > 0, "SetValue should have been called");
                Assert.True(handler.ParseCallCount > 0, "Parse should have been called");
            }
            finally
            {
                SqlMapper.Settings.PreferTypeHandlersForEnums = oldSetting;
                SqlMapper.ResetTypeHandlers();
                SqlMapper.PurgeQueryCache();
            }
        }

        [Fact]
        public void EnumTypeHandler_NullableWithNull_ReturnsNull()
        {
            var handler = new StringEnumHandler<Color>();
            var oldSetting = SqlMapper.Settings.PreferTypeHandlersForEnums;
            try
            {
                SqlMapper.ResetTypeHandlers();
                SqlMapper.AddTypeHandler(typeof(Color), handler);
                SqlMapper.Settings.PreferTypeHandlersForEnums = true;
                SqlMapper.PurgeQueryCache();

                Color? input = null;
                var result = connection.Query<NullableColorResult>(
                    "SELECT @Value AS Value", new { Value = input }).Single();

                Assert.Null(result.Value);
            }
            finally
            {
                SqlMapper.Settings.PreferTypeHandlersForEnums = oldSetting;
                SqlMapper.ResetTypeHandlers();
                SqlMapper.PurgeQueryCache();
            }
        }
    }
}
