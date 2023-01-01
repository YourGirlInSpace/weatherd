using System;

namespace weatherd.aprs.telemetry
{
    /// <summary>
    /// A reference wrapper for a bit mask.  The purpose of this class is
    ///  to allow <see cref="TelemetryValueMessage"/> and <see cref="TelemetryBitSenseMessage"/> to
    ///  access the same bit mask, where <c>SetFlag</c> on one will affect the other.
    /// </summary>
    internal class FlagsWrapper
    {
        internal byte _flags;

        internal FlagsWrapper()
        {
            _flags = 0;
        }

        internal void SetFlag(int index, bool value)
        {
            if (index < 0 || index >= 7)
                throw new ArgumentOutOfRangeException(nameof(index));

            _flags |= (byte) ((value ? 1 : 0) << (8-index));
        }
        
        internal bool GetFlag(int index)
        {
            if (index < 0 || index >= 7)
                throw new ArgumentOutOfRangeException(nameof(index));

            byte offs = (byte)(1 << (8 - index));

            return (_flags & offs) == offs;
        }
    }
}
