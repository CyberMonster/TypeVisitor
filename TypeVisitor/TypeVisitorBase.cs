using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using TypeVisitor.Mappings;

namespace TypeVisitor
{
    public abstract class TypeVisitorBase<ContextType> where ContextType : VisitorContext
    {
        protected static readonly BindingFlags BindingFlagsGet = BindingFlags.GetField
                | BindingFlags.GetProperty
                | BindingFlags.Instance
                | BindingFlags.NonPublic
                | BindingFlags.Public;

        protected static readonly BindingFlags BindingFlagsSet = BindingFlags.SetField
                | BindingFlags.SetProperty
                | BindingFlags.Instance
                | BindingFlags.NonPublic
                | BindingFlags.Public;

        protected void Visitor(object item,
            ContextType context,
            Action<ContextType, string> simpleTypeReachedAction,
            Action<ContextType, string> compositeTypeReachedAction,
            Action<ContextType, string> arrayReachedAction)
        {
            if (item is null)
                return;

            context.Item = item;
            context.ItemType = item.GetType();
            var members = context.ItemType
                .GetMembers(BindingFlagsGet)?
                .Where(x => !(x.GetCustomAttributes(typeof(CompilerGeneratedAttribute), true)?.Any() ?? false))
                .ToList() ?? new();
            foreach (var member in members)
            {
                context.RefreshContext(item, member);
                if (IsMemberIgnored(context))
                    continue;

                PrepareMember(context);
                IsNeedSkipMember(context);
                (CheckMemberType(context) switch
                {
                    -1 => null,
                    0 => arrayReachedAction,
                    1 => compositeTypeReachedAction,
                    2 => simpleTypeReachedAction,
                })?.Invoke(context, GetMemberName(context));
            }
        }

        /// <summary>
        /// Return member type flag. Array = 0; Composite object = 1; Simple object = 2; Other members = -1
        /// </summary>
        protected virtual int CheckMemberType(ContextType context)
        {
            if (context.MemberTypeCode == TypeCode.Object)
            {
                var enumerableInterface = context.MemberType.FindInterfaces((x, _) => x.FullName == "System.Collections.IEnumerable", null).FirstOrDefault();
                if (enumerableInterface is not null || context.MemberType.IsArray)
                    return 0;
                else
                    return 1;
            }
            else if (context.MemberTypeCode != TypeCode.Empty)
                return 2;

            return -1;
        }

        protected virtual string GetMemberName(ContextType context)
            => context.TypeMapping.AliasesCollection.FirstOrDefault(x => SelectorBasedDefinition.CollisionPredicate(x, context.ItemType, context.Member, context.MemberType))?.Alias
                ?? TypeMapping.GetGlobalMapping()?.AliasesCollection.FirstOrDefault(x => x.Selector.IsMatch(context.Member.Name))?.Alias
                ?? context.Member.Name;

        protected virtual bool IsMemberIgnored(ContextType context)
            => TypeMapping.GetGlobalMapping()?.IgnoreMembers.Count(x => x.Selector.IsMatch(context.Member.Name)) > 0
                || context.TypeMapping.IgnoreMembers.Any(x => SelectorBasedDefinition.CollisionPredicate(x, context.ItemType, context.Member, context.MemberType));

        protected virtual object GetValue(ContextType context)
        {
            var converter = context.TypeMapping.ConverterDefinitions.FirstOrDefault(x => SelectorBasedDefinition.CollisionPredicate(x, context.ItemType, context.Member, context.MemberType))?.Converter
                ?? TypeMapping.GetGlobalMapping()?.ConverterDefinitions.FirstOrDefault(x => x.Converter.ManagedType == context.MemberType || x.Converter.ManagedType == context.Member.GetMemberType())?.Converter;

            if (converter is null)
                return GetMemberValue(context.Item, context.Member);

            return converter.ConvertToView(GetMemberValue(context.Item, context.Member));
        }

        protected virtual void SetValue(ContextType context, object value)
        {
            var converter = context.TypeMapping.ConverterDefinitions.FirstOrDefault(x => SelectorBasedDefinition.CollisionPredicate(x, context.ItemType, context.Member, context.MemberType))?.Converter
                ?? TypeMapping.GetGlobalMapping()?.ConverterDefinitions.FirstOrDefault(x => x.Converter.ManagedType == context.MemberType)?.Converter;

            if (converter is null)
                SetMemberValue(context.Item, context.Member, value);

            converter.ConvertFromView(GetMemberValue(context.Item, context.Member));
        }

        protected virtual object GetMemberValue(object item, MemberInfo memberInfo)
            => memberInfo.MemberType switch
            {
                MemberTypes.Field => (memberInfo as FieldInfo).GetValue(item),
                MemberTypes.Property => (memberInfo as PropertyInfo).GetValue(item, null),
                _ => throw new NotSupportedException($"Member type {memberInfo.MemberType} is not supported")
            };

        protected virtual void SetMemberValue(object item, MemberInfo memberInfo, object value)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    (memberInfo as FieldInfo).SetValue(item, value);
                    break;
                case MemberTypes.Property:
                    (memberInfo as PropertyInfo).SetValue(item, value, null);
                    break;
                default:
                    throw new NotSupportedException($"Member type {memberInfo.MemberType} is not supported");
            }
        }

        /// <summary>
        /// Transform member before process. memberType can be null
        /// </summary>
        /// <param name="memberType"></param>
        /// <param name="memberTypeCode"></param>
        /// <param name="item"></param>
        protected abstract void PrepareMember(ContextType context);

        /// <summary>
        /// Check member before process. memberType can be null
        /// </summary>
        /// <param name="memberType"></param>
        /// <param name="memberTypeCode"></param>
        /// <param name="item"></param>
        protected abstract bool IsNeedSkipMember(ContextType context);
    }

    [DebuggerDisplay("Name = {MemberType.Name} TypeCode = {MemberTypeCode}")]
    public class VisitorContext
    {
        public TypeMapping TypeMapping { get; set; }
        public object Item { get; set; }
        public Type ItemType { get; set; }
        public MemberInfo Member { get; set; }
        public Type MemberType { get; set; }
        public TypeCode MemberTypeCode { get; set; }

        public VisitorContext() { }
        public VisitorContext(VisitorContext context)
            => CopyContext(context, this);

        public static void CopyContext(VisitorContext sourceContext, VisitorContext destinationContext)
        {
            destinationContext.TypeMapping = sourceContext.TypeMapping;
            destinationContext.Item = sourceContext.Item;
            destinationContext.ItemType = sourceContext.ItemType;
            destinationContext.Member = sourceContext.Member;
            destinationContext.MemberType = sourceContext.MemberType;
            destinationContext.MemberTypeCode = sourceContext.MemberTypeCode;
        }

        public virtual void RefreshContext(object item, MemberInfo member)
        {
            Item = item;
            ItemType = item.GetType();

            Member = member;
            MemberType = Member.GetMemberType();
            MemberTypeCode = Type.GetTypeCode(MemberType);
        }
    }
}
