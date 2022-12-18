using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PlayniteSounds.Common
{
    internal static class ObjectUtilities
    {
        // https://stackoverflow.com/questions/930433/apply-properties-values-from-one-object-to-another-of-the-same-type-automaticall
        /// <summary>
        /// Extension for 'Object' that copies the properties to a destination object.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        public static void Copy(this object source, object destination)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (destination is null) throw new ArgumentNullException(nameof(destination));

            // Getting the Types of the objects
            Type typeDest = destination.GetType();
            // Collect all the valid properties to map
            var properties = from sourceProperty in source.GetType().GetProperties()
                          let targetProperty = typeDest.GetProperty(sourceProperty.Name)
                          let setMethod = targetProperty?.GetSetMethod(true)
                          where sourceProperty.CanRead
                             && setMethod?.IsPublic is true
                             && (setMethod.Attributes & MethodAttributes.Static) == 0
                             && targetProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType)
                          select new { sourceProperty, targetProperty };

            properties.ForEach(
                p => p.targetProperty.SetValue(destination, p.sourceProperty.GetValue(source, null), null));
        }
    }
}
