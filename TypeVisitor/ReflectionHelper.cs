using System.Reflection;
using System;

namespace TypeVisitor
{
    public static class ReflectionHelper
    {
        public static Type GetMemberType(this MemberInfo member)
            => (member as FieldInfo)?.FieldType ?? (member as PropertyInfo)?.PropertyType;
    }
}
