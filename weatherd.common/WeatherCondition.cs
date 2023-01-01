using System;
using System.Runtime.Serialization;
using System.Text;

namespace weatherd
{
    public enum Intensity
    {
        [EnumMember(Value = "-")]
        Light,
        [EnumMember(Value = "")]
        Moderate,
        [EnumMember(Value = "+")]
        Heavy
    }
    
    [Flags]
    public enum Descriptor
    {
        [EnumMember(Value = "")]
        None,
        [EnumMember(Value = "RE")]
        Recent = 0x1,
        [EnumMember(Value = "BC")]
        Patchy = 0x2,
        [EnumMember(Value = "FZ")]
        Freezing = 0x4,
        [EnumMember(Value = "SH")]
        Showers = 0x8
    }

    [Flags]
    public enum Precipitation
    {
        [EnumMember(Value = "")]
        None,
        [EnumMember(Value = "DZ")]
        Drizzle = 0x1,
        [EnumMember(Value = "RA")]
        Rain = 0x2,
        [EnumMember(Value = "SN")]
        Snow = 0x4,
        [EnumMember(Value = "PL")]
        Sleet = 0x8,
        [EnumMember(Value = "UP")]
        Unknown = 0x10,
    }

    public enum Obscuration
    {
        [EnumMember(Value = "")]
        None,
        [EnumMember(Value = "FG")]
        Fog,
        [EnumMember(Value = "BR")]
        Mist,
        [EnumMember(Value = "HZ")]
        Haze
    }

    public enum AdditionalWeather
    {
        [EnumMember(Value = "")]
        None,
        [EnumMember(Value = "SQ")]
        Squall
    }

    public enum WMOCodeTable
    {
        Clear = 0,
        HazeVisGreaterThan1Km = 4,
        HazeVisLessThan1Km = 5,
        Mist = 10,
        FogOld = 20,
        PrecipitationOld = 21,
        DrizzleOld = 22,
        RainOld = 23,
        SnowOld = 24,
        FreezingRainOld = 25,
        Fog = 30,
        PatchyFog = 31,
        FogThinner = 32,
        FogConstant = 33,
        FogWorse = 34,
        Precipitation = 40,
        PrecipitationModerate = 41,
        PrecipitationHeavy = 42,
        Drizzle = 50,
        DrizzleSlight = 51,
        DrizzleModerate = 52,
        DrizzleHeavy = 53,
        FreezingDrizzleSlight = 54,
        FreezingDrizzleModerate = 55,
        FreezingDrizzleHeavy = 56,
        Rain = 60,
        RainLight = 61,
        RainModerate = 62,
        RainHeavy = 63,
        FreezingRainLight = 64,
        FreezingRainModerate = 65,
        FreezingRainHeavy = 66,
        RainAndSnowLight = 67,
        RainAndSnowModerateHeavy = 68,
        Snow = 70,
        SnowLight = 71,
        SnowModerate = 72,
        SnowHeavy = 73,
        SleetLight = 74,
        SleetModerate = 75,
        SleetHeavy = 76,
        ShowersOrIntermittentPrecipitation = 80,
        RainShowersLight = 81,
        RainShowersModerate = 82,
        RainShowersHeavy = 83,
        RainShowersViolent = 84,
        SnowShowersLight = 85,
        SnowShowersModerate = 86,
        SnowShowersHeavy = 87,
        Unknown = 9999
    }

    public class WeatherCondition
    {
        public Intensity Intensity { get; }
        public Descriptor Descriptor { get; set; }
        public Precipitation Precipitation { get; set; }
        public Obscuration Obscuration { get; set; }
        public AdditionalWeather Other { get; }
        
        public WeatherCondition(Precipitation precipitation)
            : this(Intensity.Moderate, Descriptor.None, precipitation, Obscuration.None) { }
        
        public WeatherCondition(Obscuration obscuration)
            : this(Intensity.Moderate, Descriptor.None, Precipitation.None, obscuration) { }
        
        public WeatherCondition(AdditionalWeather other)
            : this(Intensity.Moderate, Descriptor.None, Precipitation.None, Obscuration.None, other) { }
        
        public WeatherCondition(Intensity intensity, Descriptor descriptor, Precipitation precipitation)
            : this(intensity, descriptor, precipitation, Obscuration.None) { }
        
        public WeatherCondition(Descriptor descriptor, Obscuration obscuration)
            : this(Intensity.Moderate, descriptor, Precipitation.None, obscuration) { }

        public WeatherCondition(Descriptor descriptor, Precipitation precipitation)
            : this(Intensity.Moderate, descriptor, precipitation, Obscuration.None) { }
        
        public WeatherCondition(Intensity intensity, Obscuration obscuration)
            : this(intensity, Descriptor.None, Precipitation.None, obscuration) { }

        public WeatherCondition(Intensity intensity, Precipitation precipitation)
            : this(intensity, Descriptor.None, precipitation, Obscuration.None) { }

        public WeatherCondition(Descriptor descriptor, Precipitation precipitation, Obscuration obscuration)
            : this(Intensity.Moderate, descriptor, precipitation, obscuration) { }
        
        public WeatherCondition(Intensity intensity, Descriptor descriptor, Precipitation precipitation, Obscuration obscuration)
            : this(intensity, descriptor, precipitation, obscuration, AdditionalWeather.None) { }
        
        public WeatherCondition(Intensity intensity, Descriptor descriptor, Precipitation precipitation, Obscuration obscuration, AdditionalWeather other)
        {
            Intensity = intensity;
            Descriptor = descriptor;
            Precipitation = precipitation;
            Obscuration = obscuration;
            Other = other;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            StringBuilder metarBuilder = new();

            metarBuilder.Append(Intensity.GetEnumMemberValue());
            
            foreach (Descriptor desc in Descriptor.GetFlags())
                metarBuilder.Append(desc.GetEnumMemberValue());
            foreach (Precipitation prec in Precipitation.GetFlags())
                metarBuilder.Append(prec.GetEnumMemberValue());

            metarBuilder.Append(Obscuration.GetEnumMemberValue());
            metarBuilder.Append(Other.GetEnumMemberValue());

            if (metarBuilder.Length == 0)
                return "";

            return metarBuilder.ToString();
        }

        public static WeatherCondition FromWMOCode(WMOCodeTable codeTable)
        {
            return codeTable switch
            {
                WMOCodeTable.Clear => new WeatherCondition(Precipitation.None),
                WMOCodeTable.HazeVisGreaterThan1Km => new WeatherCondition(Obscuration.Haze),
                WMOCodeTable.HazeVisLessThan1Km => new WeatherCondition(Obscuration.Haze),
                WMOCodeTable.Mist => new WeatherCondition(Obscuration.Mist),
                WMOCodeTable.FogOld => new WeatherCondition(Descriptor.Recent, Obscuration.Fog),
                WMOCodeTable.PrecipitationOld => new WeatherCondition(Descriptor.Recent, Precipitation.Unknown),
                WMOCodeTable.DrizzleOld => new WeatherCondition(Descriptor.Recent, Precipitation.Drizzle),
                WMOCodeTable.RainOld => new WeatherCondition(Descriptor.Recent, Precipitation.Rain),
                WMOCodeTable.SnowOld => new WeatherCondition(Descriptor.Recent, Precipitation.Snow),
                WMOCodeTable.FreezingRainOld => new WeatherCondition(Descriptor.Recent | Descriptor.Freezing,
                                                                     Precipitation.Rain),
                WMOCodeTable.Fog => new WeatherCondition(Obscuration.Fog),
                WMOCodeTable.FogThinner => new WeatherCondition(Obscuration.Fog),
                WMOCodeTable.FogConstant => new WeatherCondition(Obscuration.Fog),
                WMOCodeTable.FogWorse => new WeatherCondition(Obscuration.Fog),
                WMOCodeTable.PatchyFog => new WeatherCondition(Descriptor.Patchy, Obscuration.Fog),
                WMOCodeTable.Precipitation => new WeatherCondition(Precipitation.Unknown),
                WMOCodeTable.PrecipitationModerate => new WeatherCondition(Precipitation.Unknown),
                WMOCodeTable.PrecipitationHeavy => new WeatherCondition(Intensity.Heavy, Precipitation.Unknown),
                WMOCodeTable.Drizzle => new WeatherCondition(Precipitation.Drizzle),
                WMOCodeTable.DrizzleSlight => new WeatherCondition(Intensity.Light, Precipitation.Drizzle),
                WMOCodeTable.DrizzleModerate => new WeatherCondition(Intensity.Moderate, Precipitation.Drizzle),
                WMOCodeTable.DrizzleHeavy => new WeatherCondition(Intensity.Heavy, Precipitation.Drizzle),
                WMOCodeTable.FreezingDrizzleSlight => new WeatherCondition(
                    Intensity.Light, Descriptor.Freezing, Precipitation.Drizzle),
                WMOCodeTable.FreezingDrizzleModerate => new WeatherCondition(
                    Intensity.Moderate, Descriptor.Freezing, Precipitation.Drizzle),
                WMOCodeTable.FreezingDrizzleHeavy => new WeatherCondition(
                    Intensity.Heavy, Descriptor.Freezing, Precipitation.Drizzle),
                WMOCodeTable.Rain => new WeatherCondition(Precipitation.Rain),
                WMOCodeTable.RainLight => new WeatherCondition(Intensity.Light, Precipitation.Rain),
                WMOCodeTable.RainModerate => new WeatherCondition(Intensity.Moderate, Precipitation.Rain),
                WMOCodeTable.RainHeavy => new WeatherCondition(Intensity.Heavy, Precipitation.Rain),
                WMOCodeTable.FreezingRainLight => new WeatherCondition(Intensity.Light, Descriptor.Freezing,
                                                                       Precipitation.Rain),
                WMOCodeTable.FreezingRainModerate => new WeatherCondition(
                    Intensity.Moderate, Descriptor.Freezing, Precipitation.Rain),
                WMOCodeTable.FreezingRainHeavy => new WeatherCondition(Intensity.Heavy, Descriptor.Freezing,
                                                                       Precipitation.Rain),
                WMOCodeTable.RainAndSnowLight => new WeatherCondition(Intensity.Light,
                                                                      Precipitation.Rain | Precipitation.Snow),
                WMOCodeTable.RainAndSnowModerateHeavy => new WeatherCondition(
                    Intensity.Moderate, Precipitation.Rain | Precipitation.Snow),
                WMOCodeTable.Snow => new WeatherCondition(Precipitation.Snow),
                WMOCodeTable.SnowLight => new WeatherCondition(Intensity.Light, Precipitation.Snow),
                WMOCodeTable.SnowModerate => new WeatherCondition(Intensity.Moderate, Precipitation.Snow),
                WMOCodeTable.SnowHeavy => new WeatherCondition(Intensity.Heavy, Precipitation.Snow),
                WMOCodeTable.SleetLight => new WeatherCondition(Intensity.Light, Precipitation.Sleet),
                WMOCodeTable.SleetModerate => new WeatherCondition(Intensity.Moderate, Precipitation.Sleet),
                WMOCodeTable.SleetHeavy => new WeatherCondition(Intensity.Heavy, Precipitation.Sleet),
                WMOCodeTable.ShowersOrIntermittentPrecipitation => new WeatherCondition(
                    Descriptor.Showers, Precipitation.Unknown),
                WMOCodeTable.RainShowersLight => new WeatherCondition(Intensity.Light, Descriptor.Showers,
                                                                      Precipitation.Rain),
                WMOCodeTable.RainShowersModerate => new WeatherCondition(
                    Intensity.Moderate, Descriptor.Showers, Precipitation.Rain),
                WMOCodeTable.RainShowersHeavy => new WeatherCondition(Intensity.Heavy, Descriptor.Showers,
                                                                      Precipitation.Rain),
                WMOCodeTable.RainShowersViolent => new WeatherCondition(AdditionalWeather.Squall),
                WMOCodeTable.SnowShowersLight => new WeatherCondition(Intensity.Light, Descriptor.Showers,
                                                                      Precipitation.Snow),
                WMOCodeTable.SnowShowersModerate => new WeatherCondition(
                    Intensity.Moderate, Descriptor.Showers, Precipitation.Snow),
                WMOCodeTable.SnowShowersHeavy => new WeatherCondition(Intensity.Heavy, Descriptor.Showers,
                                                                      Precipitation.Snow),
                _ => new WeatherCondition(Intensity.Moderate, Descriptor.None, Precipitation.None, Obscuration.None, AdditionalWeather.None)
            };
        }
    }
}
