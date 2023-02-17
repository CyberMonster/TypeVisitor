using System.Linq.Expressions;
using System;
using System.Text.RegularExpressions;
using TypeVisitor.Converters;
using System.Reflection;

namespace TypeVisitor.Mappings
{
    public static class TypeMappingExtensions
    {
        public static TypeMapping<T> RegisterClassMap<T, U>() where T : class, new() where U : class, new()
            => RegisterClassMap<T>(typeof(U));

        public static TypeMapping<T> RegisterClassMap<T>(Type inheritedTableType = null) where T : class, new()
            => TypeMapping<T>.CreateMapping<T>(inheritedTableType);

        public static TypeMapping<T> IgnoreMember<T>(this TypeMapping<T> mapping, Expression<Func<T, object>> ignoreSelector, bool checkOriginalMemberType = false)
        {
            var member = ignoreSelector.GetMemberExpression();
            mapping.IgnoreMembers.Add(new IgnoreDefinition(member.Expression.Type, member.Member, ignoreSelector, checkOriginalMemberType));
            return mapping;
        }

        public static TypeMapping<T> AddAlias<T>(this TypeMapping<T> mapping, Expression<Func<T, object>> aliasSelector, string alias, bool checkOriginalMemberType = false)
        {
            var member = aliasSelector.GetMemberExpression();
            mapping.AliasesCollection.Add(new AliasDefinition(alias, member.Expression.Type, member.Member, aliasSelector, checkOriginalMemberType));
            return mapping;
        }

        public static TypeMapping<T> AddConverter<T, U>(this TypeMapping<T> mapping, Expression<Func<T, object>> converterSelector, IConverter<U> converter, bool checkOriginalMemberType = false)
        {
            var member = converterSelector.GetMemberExpression();
            mapping.ConverterDefinitions.Add(new ConverterDefinition(converter, member.Expression.Type, member.Member, converterSelector, checkOriginalMemberType));
            return mapping;
        }

        public static void BuildTypeMapping<T>(this TypeMapping<T> _) where T : class, new() { }


        public static TypeMapping IgnoreMember(this TypeMapping mapping, Regex selector)
        {
            mapping.IgnoreMembers.Add(new IgnoreDefinition(selector));
            return mapping;
        }

        public static TypeMapping AddAlias(this TypeMapping mapping, Regex selector, string alias)
        {
            mapping.AliasesCollection.Add(new AliasDefinition(selector, alias));
            return mapping;
        }

        public static TypeMapping AddConverter(this TypeMapping mapping, IConverter converter)
        {
            mapping.ConverterDefinitions.Add(new ConverterDefinition(converter));
            return mapping;
        }

        public static TypeMapping AddConverter(this TypeMapping mapping, IConverter converter, Func<SelectorBasedDefinition, Type, MemberInfo, Type, bool> checkPredicate)
        {
            mapping.ConverterDefinitions.Add(new ConverterDefinition(converter, checkPredicate));
            return mapping;
        }

        public static void BuildTypeMapping(this TypeMapping _) { }

        public static MemberExpression GetMemberExpression<T, FieldType>(this Expression<Func<T, FieldType>> expression)
        {
            var body = expression.Body;
            var unaryExpression = body as UnaryExpression;
            return (MemberExpression)(unaryExpression?.Operand ?? expression.Body);
        }
    }
}
