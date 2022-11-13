namespace weatherd.datasources.Vaisala.Messages
{
    public class VaisalaPrecipitationMessage : VaisalaMessage
    {
        public const int Parts = 4;
        
        public VaisalaValue<int> OneMinuteAverageVisibility { get; private set; }
        public VaisalaValue<WeatherCode> InstantaneousWeather { get; private set; }
        public VaisalaValue<float> OneMinuteWaterIntensity { get; private set; }

        public VaisalaPrecipitationMessage() :
            base(VaisalaMessageType.Precipitation) { }

        /// <inheritdoc />
        protected override VaisalaMessage Parse(SpanSplitEnumerator spanSplit)
        {
            if (!ParseAlarms(ref spanSplit))
                return null;
            
            bool isValid = true;

            OneMinuteAverageVisibility = VaisalaValue<int>.Parse(ref spanSplit, ref isValid);
            InstantaneousWeather = VaisalaValue<WeatherCode>.Parse(ref spanSplit, ref isValid);
            OneMinuteWaterIntensity = VaisalaValue<float>.Parse(ref spanSplit, ref isValid);

            return !isValid ? null : this;
        }
    }
}
