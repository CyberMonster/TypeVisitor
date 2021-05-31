using System;

namespace TypeVisitor.Converters
{
    public interface IConverter
    {
        public Type ManagedType { get; }

        public object ConvertToView(object value);
        public object ConvertFromView(object value);
    }

    public interface IConverter<T> : IConverter
    {
        public object ConvertToView(T value);
        public new T ConvertFromView(object value);
    }
}
