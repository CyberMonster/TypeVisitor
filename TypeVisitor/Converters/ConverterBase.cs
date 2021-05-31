using System;

namespace TypeVisitor.Converters
{
    public abstract class ConverterBase<T> : IConverter<T>
    {
        public Type ManagedType => typeof(T);

        public abstract object ConvertToView(T value);

        public abstract T ConvertFromView(object value);

        public object ConvertToView(object value)
            => ConvertToView((T)(value ?? default(T)));

        object IConverter.ConvertFromView(object value)
            => ConvertFromView(value);
    }
}
