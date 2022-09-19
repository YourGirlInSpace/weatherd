namespace weatherd.datasources.pakbus
{
    public static class PakbusUtilities
    {
        /// <summary>
        /// Calculates the Pakbus signature for a given buffer.
        /// </summary>
        /// <param name="buf">The buffer to calculate the signature for.</param>
        /// <param name="length">The length of the buffer.</param>
        /// <param name="seed">The initial signature seed.  This defaults to 0xAAAA.</param>
        /// <returns>The two byte signature for the given buffer.</returns>
        internal static ushort ComputeSignature(byte[] buf, int length, ushort seed = 0xAAAA)
        {
            ushort j, n;
            ushort ret = seed;

            for (n = 0; n < length; n++)
            {
                j = ret;
                ret = (ushort)((ret << 1) & 0x01FF);
                if (ret >= 0x100)
                    ret++;

                ret = (ushort)(((ret + (j >> 8) + buf[n]) & 0xFF) | (j << 8));
            }

            return ret;
        }

        /// <summary>
        /// Calculates the 'nullifier' for a provided signature.
        /// </summary>
        /// <param name="sig">The signature to nullify.</param>
        /// <returns>A two byte sequence that 'nulls out' a signature.</returns>
        /// <remarks>
        ///     This method generates a "nullifier" for a provided signature.
        ///     This is appended to the end of a buffer in lieu of the actual
        ///     signature value.  When calculating the signature of this new
        ///     buffer, the result will be zero.  This is how to authenticate
        ///     a received message.
        /// </remarks>
        internal static byte[] CalculateSignatureNullifier(ushort sig)
        {
            ushort tmp = (ushort)(0x1FF & (sig << 1));
            if (tmp >= 0x100)
                tmp++;

            byte null1 = (byte)(0x00FF & (0x100 - (sig >> 8) - (0x00FF & tmp)));
            byte msb = (byte)(0x00FF & sig);
            byte null0 = (byte)(0x00FF & (0x100 - (0xFF & msb)));

            return new[] { null1, null0 };
        }
    }
}
