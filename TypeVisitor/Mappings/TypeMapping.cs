using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using TypeVisitor.Converters;

namespace TypeVisitor.Mappings
{
    public class TypeMapping<T> : TypeMapping
    {
        private protected TypeMapping(TypeMapping inheritedMappingType) : base(inheritedMappingType) { }

        public static new TypeMapping<MapType> CreateMapping<MapType>(Type inheritedMappingType = null)
        {
            TypeMapping inheritMapping = null;
            if (inheritedMappingType is not null)
                Mappings.TryGetValue(inheritedMappingType, out inheritMapping);

            var mapping = new TypeMapping<MapType>(inheritMapping);
            Mappings.Add(typeof(MapType), mapping);
            return mapping;
        }

        public static new TypeMapping GetMapping<MapType>()
        {
            Mappings.TryGetValue(typeof(MapType), out var mapping);
            if (mapping is null)
            {
                mapping = new TypeMapping<MapType>(null);
                Mappings.Add(typeof(MapType), mapping);
            }

            return mapping;
        }
    }

    public class TypeMapping
    {
        private protected static TypeMapping GlobalMapping { get; private set; }

        private protected static Dictionary<Type, TypeMapping> Mappings { get; private set; } = new();

        public List<IgnoreDefinition> IgnoreMembers { get; set; } = new();
        public List<AliasDefinition> AliasesCollection { get; set; } = new();
        public List<ConverterDefinition> ConverterDefinitions { get; set; } = new();

        public static TypeMapping<MapType> CreateMapping<MapType>(Type inheritedMappingType = null)
            => TypeMapping<MapType>.CreateMapping<MapType>(inheritedMappingType);

        public static TypeMapping GetMapping<MapType>()
            => TypeMapping<MapType>.GetMapping<MapType>();

        public static TypeMapping GetGlobalMapping()
            => GlobalMapping;

        public static TypeMapping ConfigureGlobalMapping()
            => GlobalMapping = new TypeMapping(GlobalMapping);

        public static void ResetMappings()
        {
            GlobalMapping = null;
            Mappings = new();
        }

        private protected TypeMapping(TypeMapping inheritedMapping)
        {
            if (inheritedMapping is null)
                return;

            IgnoreMembers = inheritedMapping.IgnoreMembers;
            AliasesCollection = inheritedMapping.AliasesCollection;
        }
    }

    public class IgnoreDefinition : SelectorBasedDefinition
    {
        public Regex Selector { get; set; }

        internal IgnoreDefinition(Type type, MemberInfo member, Expression expression, bool checkOriginalMemberType = false) : base(type, member, expression, checkOriginalMemberType) { }

        internal IgnoreDefinition(Regex selector, bool checkOriginalMemberType = false) : base(null, null, null, checkOriginalMemberType)
            => Selector = selector;
    }

    public class AliasDefinition : SelectorBasedDefinition
    {
        public Regex Selector { get; set; }
        public string Alias { get; set; }

        internal AliasDefinition(string alias, Type type, MemberInfo member, Expression expression, bool checkOriginalMemberType = false) : base(type, member, expression, checkOriginalMemberType)
            => Alias = alias;

        internal AliasDefinition(Regex selector, string alias, bool checkOriginalMemberType = false) : base(null, null, null, checkOriginalMemberType)
        {
            Selector = selector;
            Alias = alias;
        }
    }

    public class ConverterDefinition : SelectorBasedDefinition
    {
        public IConverter Converter { get; set; }

        internal ConverterDefinition(IConverter converter, Type type, MemberInfo member, Expression expression, bool checkOriginalMemberType = false) : base(type, member, expression, checkOriginalMemberType)
            => Converter = converter;

        internal ConverterDefinition(IConverter converter, bool checkOriginalMemberType = false) : base(converter.ManagedType, null, null, checkOriginalMemberType)
            => Converter = converter;

        internal ConverterDefinition(IConverter converter, Func<SelectorBasedDefinition, Type, MemberInfo, Type, bool> checkPredicate, bool checkOriginalMemberType = false) : base(checkPredicate, null, null, null, checkOriginalMemberType)
            => Converter = converter;
    }

    public class SelectorBasedDefinition
    {
        public Type Type { get; set; }
        public MemberInfo Member { get; set; }
        public Expression Expression { get; set; }
        public bool CheckOriginalMemberType { get; set; }
        public Func<SelectorBasedDefinition, Type, MemberInfo, Type, bool> CheckPredicate { get; set; }

        public SelectorBasedDefinition(Type type, MemberInfo member, Expression expression, bool checkOriginalMemberType)
        {
            Type = type;
            Member = member;
            Expression = expression;
            CheckOriginalMemberType = checkOriginalMemberType;
        }

        public SelectorBasedDefinition(Func<SelectorBasedDefinition, Type, MemberInfo, Type, bool> checkPredicate, Type type, MemberInfo member, Expression expression, bool checkOriginalMemberType)
        {
            CheckPredicate = checkPredicate;
            Type = type;
            Member = member;
            Expression = expression;
            CheckOriginalMemberType = checkOriginalMemberType;
        }

        public static bool CollisionPredicate(SelectorBasedDefinition definition, Type destinationType, MemberInfo member, Type memberType)
        {
            if (memberType is null || (member.GetCustomAttributes(typeof(CompilerGeneratedAttribute), true)?.Any() ?? false))
                return false;
            if (definition.CheckPredicate is not null)
                return definition.CheckPredicate(definition, destinationType, member, memberType);
            else if (definition.Member is null)
                return definition.Type == destinationType;
            else return definition.Type == destinationType
                && definition.CheckOriginalMemberType
                    ? definition.Member.GetMemberType().FullName == member.GetMemberType().FullName
                    : definition.Member.GetMemberType().FullName == memberType.FullName
                && definition.Member.MemberType == member.MemberType
                && definition.Member.Name == member.Name;
        }
    }
}
