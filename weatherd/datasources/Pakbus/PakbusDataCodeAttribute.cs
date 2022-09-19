using System;

namespace weatherd.datasources.Pakbus
{
    public class PakbusDataCodeAttribute : Attribute
    {
        public Type EncodeAs { get; set; }
        public bool BigEndian { get; set; }
        public int Quantity { get; set; } = 1;

        public int Size { get; set; }
    }
}
