namespace weatherd.datasources.Vaisala.Messages
{
    public class VaisalaVisibilityMessage : VaisalaMessage
    {
        public const int Parts = 3;

        public VaisalaValue<int> OneMinuteAverageVisibility { get; private set; }
        public VaisalaValue<int> TenMinuteAverageVisibility { get; private set; }

        public VaisalaVisibilityMessage() :
            base(VaisalaMessageType.Visibility) { }

        /// <inheritdoc />
        protected override VaisalaMessage Parse(SpanSplitEnumerator spanSplit)
        {
            if (!ParseAlarms(ref spanSplit))
                return null;

            bool isValid = true;

            OneMinuteAverageVisibility = VaisalaValue<int>.Parse(ref spanSplit, ref isValid);
            TenMinuteAverageVisibility = VaisalaValue<int>.Parse(ref spanSplit, ref isValid);

            return !isValid ? null : this;
        }
    }
}
