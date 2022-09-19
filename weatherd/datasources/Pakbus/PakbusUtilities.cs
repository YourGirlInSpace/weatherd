namespace weatherd.datasources.Pakbus
{
    public static class PakbusUtilities
    {
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
