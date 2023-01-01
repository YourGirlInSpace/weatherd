using System;
using System.Diagnostics;

namespace weatherd.datasources.Vaisala
{
    [DebuggerDisplay("{Value}")]
    public record VaisalaValue<T>
    {
        public static VaisalaValue<T> NoValue => new();

        public VaisalaValueType Type { get; private set; }
        public T Value { get; private set; }
        public bool HasValue { get; private set; }

        protected VaisalaValue()
        {
            Value = default;
            Type = VaisalaValueType.None;
            HasValue = false;
        }

        protected VaisalaValue(T value, VaisalaValueType type)
        {
            Value = value;
            Type = type;
            HasValue = true;
        }

        /// <summary>
        /// Moves to the next item in the <see cref="SpanSplitEnumerator"/> and attempts to parse it.
        /// </summary>
        /// <param name="enumerator">The enumerator to span.</param>
        /// <param name="isValid">A pointer to a boolean used as a flag indicating failure status globally.</param>
        /// <returns>A <see cref="VaisalaValue{T}"/> representing the next read value</returns>
        /// <remarks>
        ///     Regarding the <paramref name="isValid"/> parameter:  This parameter is a pointer to
        ///     an external boolean indicating whether any parse attempt succeeded or failed.  If any
        ///     attempt fails, <paramref name="isValid"/> is set to false and further attempts will
        ///     not run.
        /// </remarks>
        public static VaisalaValue<T> Parse(ref SpanSplitEnumerator enumerator, ref bool isValid)
        {
            // If one of the parses fails, don't continue to attempt parsing
            if (!isValid)
                return null;

            if (enumerator.MoveNext())
            {
                var value = Parse(enumerator.Current);

                if (value is not null)
                    return value;
            }

            isValid = false;
            return null;
        }

        protected virtual VaisalaValue<T> ParseProtected(ReadOnlySpan<char> data, VaisalaValueType valueType)
        {
            if (data.Trim()[0] == '/')
                return NoValue;

            try
            {
                Type = valueType;
                switch (valueType)
                {
                    case VaisalaValueType.Enum:
                        if (!int.TryParse(data, out int eValue))
                            return null;
                        
                        Value = (T)Enum.ToObject(typeof(T), eValue);
                        HasValue = true;
                        break;
                    case VaisalaValueType.Integer:
                    case VaisalaValueType.Float:
                    case VaisalaValueType.String:
                        Value = (T)Convert.ChangeType(data.ToString(), typeof(T));
                        HasValue = true;
                        break;
                    case VaisalaValueType.None:
                        return NoValue;
                    default:
                        return null;
                }
            } catch
            {
                return null;
            }

            return this;
        }

        public static VaisalaValue<T> Parse(ReadOnlySpan<char> data)
        {
            if (data.Trim()[0] == '/')
                return NoValue;
            
            return System.Type.GetTypeCode(typeof(T)) switch
            {
                // By default, enums are represented as an integer.
                TypeCode.Int32 when typeof(T).IsEnum => new VaisalaValue<T>().ParseProtected(data, VaisalaValueType.Enum),
                TypeCode.Int32 => new VaisalaValue<T>().ParseProtected(data, VaisalaValueType.Integer),
                TypeCode.Single => new VaisalaValue<T>().ParseProtected(data, VaisalaValueType.Float),
                TypeCode.String => new VaisalaValue<T>().ParseProtected(data, VaisalaValueType.String),
                _ => null
            };
        }
    }
}
