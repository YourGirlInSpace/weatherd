using UnitsNet;

namespace weatherd.records
{
    public record ObservedConditions
    {
        /// <summary>
        /// The temperature in degrees celsius
        /// </summary>
        public Temperature Temperature { get; set; }
        /// <summary>
        /// The dewpoint in degrees celsius
        /// </summary>
        public Temperature Dewpoint { get; set; }
    }
}
