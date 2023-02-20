using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PlayniteSounds.Common
{
    public static class ObjectUtilities
    {
        // Based On: https://stackoverflow.com/questions/930433/apply-properties-values-from-one-object-to-another-of-the-same-type-automaticall
        /// <summary>
        /// Extension for 'Object' that copies the properties from a source object.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="source">The source.</param>
        public static void Copy<T>(this T destination, T source) where T : class 
            => RetrieveProperties<T>().ForEach(p => p.SetValue(destination, p.GetValue(source, null), null));

        private static IEnumerable<PropertyInfo> RetrieveProperties<T>() where T : class =>
            from property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            let setMethod = property?.GetSetMethod(true)
            where property.CanRead && setMethod?.IsPublic is true
            select property;
    }
}
