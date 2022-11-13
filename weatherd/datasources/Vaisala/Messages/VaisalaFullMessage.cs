namespace weatherd.datasources.Vaisala.Messages
{
    public class VaisalaFullMessage : VaisalaMessage
    {
        public const int Parts = 10;

        public VaisalaValue<int> OneMinuteAverageVisibility { get; private set; }
        public VaisalaValue<int> TenMinuteAverageVisibility { get; private set; }
        public VaisalaValue<string> NWSWeatherCode { get; private set; }
        public VaisalaValue<WeatherCode> InstantaneousWeather { get; private set; }
        public VaisalaValue<WeatherCode> Weather15Minute { get; private set; }
        public VaisalaValue<WeatherCode> Weather1Hour { get; private set; }
        public VaisalaValue<float> OneMinuteWaterIntensity { get; private set; }
        public VaisalaValue<float> CumulativeWater { get; private set; }
        public VaisalaValue<int> CumulativeSnow { get; private set; }

        public VaisalaFullMessage()
            : base(VaisalaMessageType.Full) { }

        protected VaisalaFullMessage(VaisalaMessageType type)
            : base(type) { }

        /// <inheritdoc />
        protected override VaisalaMessage Parse(SpanSplitEnumerator spanSplit)
        {
            if (!ParseAlarms(ref spanSplit))
                return null;
            
            bool isValid = true;
            
            OneMinuteAverageVisibility = VaisalaValue<int>.Parse(ref spanSplit, ref isValid);
            TenMinuteAverageVisibility = VaisalaValue<int>.Parse(ref spanSplit, ref isValid);
            NWSWeatherCode = VaisalaValue<string>.Parse(ref spanSplit, ref isValid);
            InstantaneousWeather = VaisalaValue<WeatherCode>.Parse(ref spanSplit, ref isValid);
            Weather15Minute = VaisalaValue<WeatherCode>.Parse(ref spanSplit, ref isValid);
            Weather1Hour = VaisalaValue<WeatherCode>.Parse(ref spanSplit, ref isValid);
            OneMinuteWaterIntensity = VaisalaValue<float>.Parse(ref spanSplit, ref isValid);
            CumulativeWater = VaisalaValue<float>.Parse(ref spanSplit, ref isValid);
            CumulativeSnow = VaisalaValue<int>.Parse(ref spanSplit, ref isValid);

            return !isValid ? null : this;
        }
    }
}
