// Copyright Bastian Eicher
// Licensed under the MIT License

using System;
using System.ComponentModel;
using System.Globalization;

namespace NanoByte.Common.Values.Design
{
    /// <summary>
    /// Generic type converter for classes that have a constructor with a single string argument and a corresponding <see cref="object.ToString"/> implementation.
    /// </summary>
    /// <typeparam name="T">The type the converter is used for.</typeparam>
    /// <example>
    ///   Add this attribute to the type:
    ///   <code>[TypeConverter(typeof(StringConstructorConverter&lt;NameOfType&gt;))]</code>
    /// </example>
    public class StringConstructorConverter<T> : TypeConverter
    {
        /// <inheritdoc/>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        /// <inheritdoc/>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                var constructor = typeof(T).GetConstructor(new[] {typeof(string)});
                if (constructor != null)
                {
                    try
                    {
                        return string.IsNullOrEmpty(stringValue) ? null : constructor.Invoke(new object[] {stringValue});
                    }
                    catch (Exception ex) when (ex.InnerException != null)
                    {
                        throw ex.InnerException.PreserveStack();
                    }
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <inheritdoc/>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            => value != null && destinationType == typeof(string)
                ? value.ToString()
                : base.ConvertTo(context, culture, value, destinationType);
    }
}
