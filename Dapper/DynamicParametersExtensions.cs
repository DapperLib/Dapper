using System.Reflection;

namespace Dapper
{
    /// <summary>
    /// An Extention Class to DynamicParameters
    /// </summary>

    internal static class DynamicParametersExtensions
    {
        /// <summary>
        /// Add Parameter if exists
        /// </summary>
        /// <param name="dp">Extending DynamicParameter</param>
        /// <param name="paramName">Parameter Name</param>
        /// <param name="param">Parameter with type of dynamic</param>
        internal static void AddIfExists(this DynamicParameters dp, string paramName, dynamic? param)
        {
            if (param is null)
            {
                return;
            }
            dp.Add(paramName, param);
        }

        /// <summary>
        /// Add members of class as parameter; if the member is not null
        /// </summary>
        /// <typeparam name="T">Generic Type</typeparam>
        /// <param name="dp">Extending DynamicParameters</param>
        /// <param name="param"></param>
        public static void AddParametersIfExists<T>(this DynamicParameters dp, T param)
        {
            if (param is not null)
            {
                foreach (PropertyInfo prop in param.GetType().GetProperties())
                {

                    AddIfExists(dp, paramName: prop.Name, param: prop.GetValue(param));

                }
            }
        }
    }
}
