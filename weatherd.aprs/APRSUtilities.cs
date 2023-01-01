namespace weatherd.aprs
{
    public static class APRSUtilities
    {
        public static string GetPasscode(string stationCall)
        {
            string rootcall = GetRootCallsign(stationCall);

            int hash = 0x73e2;
            int i = 0;
            int len = rootcall.Length;

            while (i < len)
            {
                hash ^= rootcall[i] << 8;
                if (i + 1 < len)
                    hash ^= rootcall[i + 1];
                i += 2;
            }

            int rez = hash & 0x7fff;

            return rez.ToString("00000");
        }
        
        public static string GetRootCallsign(string stationCall)
        {
            for (int i = 0; i < stationCall.Length; i++)
            {
                if (!char.IsLetterOrDigit(stationCall[i]))
                {
                    return stationCall.Substring(0, i);
                }
            }

            return stationCall;
        }
    }
}
