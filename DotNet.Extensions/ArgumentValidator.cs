using System;
using System.Runtime.CompilerServices;

namespace DotNet.Extensions
{
    internal static class ArgumentValidator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNull(string parameterName, object actualValue)
        {
            if (actualValue == null)
            {
                string message = $"{parameterName} cannot be null.";
                throw new ArgumentNullException(parameterName, message);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNotNull(string parameterName, object actualValue)
        {
            if (actualValue != null)
            {
                string message = $"{parameterName} should be null.";
                throw new ArgumentNullException(parameterName, message);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfFalse(string parameterName, bool actualValue)
        {
            if (!actualValue)
            {
                string message = $"{parameterName} should be true.";
                throw new ArgumentNullException(parameterName, message);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfLessThan<T>(string parameterName, T actualValue, T lessThan) where T : IComparable, IFormattable, IConvertible, IComparable<T>, IEquatable<T>
        {
            if (actualValue.CompareTo(lessThan) < 0)
            {
                string message = $"{parameterName} cannot be less than {lessThan}.";
                throw new ArgumentOutOfRangeException(parameterName, actualValue, message);
            }
        }
    }
}
