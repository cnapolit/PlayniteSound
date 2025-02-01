using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayniteSounds.Common.Extensions;

public static class ArrayExtensions
{
    public static IEnumerable<TOut> Select<TOut>(this Array array, Func<object, TOut> func)
        => from object item in array select func(item);

    public static IEnumerable<TOut> Select<TEnum, TOut>(Func<TEnum, TOut> func) where TEnum : Enum
        => from TEnum enumValue in Enum.GetValues(typeof(TEnum)) select func(enumValue);

    public static IEnumerable<TEnum> Where<TEnum>(Func<TEnum, bool> func) where TEnum : Enum
        => from TEnum enumValue in Enum.GetValues(typeof(TEnum)) where func(enumValue) select enumValue;
}