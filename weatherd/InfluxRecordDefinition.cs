namespace weatherd
{
    public class InfluxRecordDefinition
    {
        public InfluxRecordDefinition()
        { }

        public InfluxRecordDefinition(string Name, string Property, string Unit)
        {
            this.Name = Name;
            this.Property = Property;
            this.Unit = Unit;
        }

        public string Name { get; set; }
        public string Property { get; set; }
        public string Unit { get; set; }

        public void Deconstruct(out string Name, out string Property, out string Unit)
        {
            Name = this.Name;
            Property = this.Property;
            Unit = this.Unit;
        }
    }
}