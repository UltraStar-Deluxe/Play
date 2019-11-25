using System.ComponentModel;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace System
{
    [PublicAPI]
    public static class EnumExtensions
    {
        [CanBeNull]
        public static T GetAttribute<T>([NotNull] this Enum value) where T : Attribute
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var type = value.GetType();

            var typeInfo = type.GetTypeInfo();

            if (!typeInfo.IsEnum)
                throw new ArgumentOutOfRangeException(nameof(value));

            var memberInfo = typeInfo.DeclaredMembers.FirstOrDefault(s => s.Name == value.ToString());
            if (memberInfo == null)
                throw new ArgumentNullException(nameof(memberInfo));

            var attribute = memberInfo.GetCustomAttribute<T>();

            return attribute;
        }

        [NotNull]
        public static DescriptionAttribute GetDescriptionAttribute([NotNull] this Enum value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var attribute = value.GetAttribute<DescriptionAttribute>();
            if (attribute == null)
                throw new ArgumentNullException(nameof(attribute));

            return attribute;
        }
    }
}