using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Rudine.Web.Util
{
    internal static class MethodInfoExtensions
    {
        /// <summary>
        ///     Invoke a class's method WITHOUT knowing the order of the parameter order signature. All behavior mirror's the
        ///     underlying Invoke called; we just translate everything to and from dictionary
        ///     <string-obj>
        ///         and the original underlying object array
        ///         i.e.
        ///         orderedParameterMethod(parameterA, parameterB, parameterC);
        ///         namedParameterMethod(firstParm:parameterA, secondParam:parameterB, thirdParm:parameterC);
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="obj"></param>
        /// <param name="parametersWithName">
        ///     Case-sensitive keys represent the named parameter, out parameter value parameter have
        ///     there values set when invoke is complete.
        /// </param>
        /// <returns></returns>
        internal static object Invoke(this MethodInfo methodInfo, object obj, Dictionary<string, object> parametersWithName)
        {
            ParameterInfo[] _GetParameters = methodInfo.GetParameters();

            object[] _parameters = _GetParameters
                .OrderBy(parameter => parameter.Position)
                .Select(parameter =>
                            parametersWithName.ContainsKey(parameter.Name)
                                ? parametersWithName[parameter.Name]
                                : parameter.DefaultValue)
                .ToArray();

            object objReturn = methodInfo.Invoke(obj, _parameters);

            foreach (ParameterInfo _parameter in _GetParameters
                .Where(parameter => parameter.IsOut))
                parametersWithName[_parameter.Name] = _parameters[_parameter.Position];

            return objReturn;
        }
    }
}