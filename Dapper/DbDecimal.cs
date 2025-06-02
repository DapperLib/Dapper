using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dapper
{
    /// <summary>
    /// This class represents a SQL Decimal (Numeric) type, it can be used if you need 
    /// to be consistent with query parameter sizes, usually by specifying Precision and Scale
    /// values that match the type in the database.
    /// </summary>
    public sealed class DbDecimal : SqlMapper.ICustomQueryParameter
    {
        private const byte _defaultPrecision = 38;
        private const byte _defaultScale = 8;

        /// <summary>
        /// The value to be inserted or updated in the database
        /// </summary>
        public decimal Value { get; set; } = default;

        /// <summary>
        /// 
        /// </summary>
        public byte Precision { get; set; } = _defaultPrecision;

        /// <summary>
        /// The number of decimal digits that are stored to the right of the 
        /// decimal point.
        /// </summary>
        public byte Scale { get; set; } = _defaultScale;

        /// <summary>
        /// The default constructor used when attaching the individual properties 
        /// of the parameter.
        /// </summary>
        public DbDecimal()
        { }

        /// <summary>
        /// The primary constructor. This is the constructor that should generally
        /// be used to create the object because it accepts values for the properties
        /// used in the 90% use-case.
        /// </summary>
        /// <param name="value">The value to be inserted or updated in the database</param>
        /// <param name="precision">The maximum total number of decimal digits to be stored.</param>
        /// <param name="scale">The number of decimal digits that are stored to the right of the decimal point.</param>
        public DbDecimal(decimal value, byte precision, byte scale)
        {
            this.Value = value;
            this.Precision = precision;
            this.Scale = scale;
        }

        /// <summary>
        /// Add the parameter to the command... internal use only
        /// </summary>
        /// <param name="command"></param>
        /// <param name="name"></param>
        public void AddParameter(IDbCommand command, string name)
        {
            bool add = !command.Parameters.Contains(name);

            IDbDataParameter param;
            if (add)
            {
                param = command.CreateParameter();
                param.ParameterName = name;
            }
            else
            {
                param = (IDbDataParameter)command.Parameters[name];
            }

#pragma warning disable 0618
            param.Value = SqlMapper.SanitizeParameterValue(Value);
#pragma warning restore 0618
            param.Precision = this.Precision;
            param.Scale = this.Scale;
            param.DbType = DbType.Decimal;

            if (add)
            {
                command.Parameters.Add(param);
            }
        }

    }
}
