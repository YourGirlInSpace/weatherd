using System;
using System.Diagnostics;

namespace weatherd.datasources.Vaisala
{
    [DebuggerDisplay("{Value}")]
    public record VaisalaAlarmValue<T> : VaisalaValue<T>
    {
        public new static VaisalaAlarmValue<T> NoValue => new();

        public bool InAlarm { get; private set; }

        public VaisalaAlarmValue() => InAlarm = false;

        public VaisalaAlarmValue(T value, VaisalaValueType type)
            : base(value, type) =>
            InAlarm = false;

        public VaisalaAlarmValue(T value, VaisalaValueType type, bool inAlarm)
            : base(value, type) =>
            InAlarm = inAlarm;

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
        public new static VaisalaAlarmValue<T> Parse(ref SpanSplitEnumerator enumerator, ref bool isValid)
        {
            VaisalaAlarmValue<T> result = Parse(ref enumerator);

            if (result == null)
                isValid = false;

            return result;
        }
        
        public static VaisalaAlarmValue<T> Parse(ref SpanSplitEnumerator splitEnum)
        {
            if (!splitEnum.MoveNext())
                return null;

            bool inAlarm = splitEnum.Current[0] == '*';

            if (inAlarm && splitEnum.Current.Length == 1 && !splitEnum.MoveNext())
                return null;

            ReadOnlySpan<char> data = splitEnum.Current;

            if (data.Trim()[0] == '/')
                return NoValue;

            if (data[0] == '*')
            {
                inAlarm = true;
                data = data[1..];
            }

            if (System.Type.GetTypeCode(typeof(T)) switch
                {
                    // By default, enums are represented as an integer.
                    TypeCode.Int32 when typeof(T).IsEnum => new VaisalaAlarmValue<T>().ParseProtected(
                        data, VaisalaValueType.Enum),
                    TypeCode.Int32 => new VaisalaAlarmValue<T>().ParseProtected(data, VaisalaValueType.Integer),
                    TypeCode.Single => new VaisalaAlarmValue<T>().ParseProtected(data, VaisalaValueType.Float),
                    TypeCode.String => new VaisalaAlarmValue<T>().ParseProtected(data, VaisalaValueType.String),
                    _ => null
                } is not VaisalaAlarmValue<T> baseValue)
                return null;

            baseValue.InAlarm = inAlarm;
            return baseValue;
        }
    }
}
