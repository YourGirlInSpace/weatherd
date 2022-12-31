namespace weatherd
{
    public class RecordDefinition
    {
        public RecordDefinition()
        { }

        public RecordDefinition(string Name, string Property, string Unit, string Type)
        {
            this.Name = Name;
            this.Property = Property;
            this.Unit = Unit;
            this.Type = Type;
        }

        public string Name { get; set; }
        public string Property { get; set; }
        public string Unit { get; set; }
        public string Type { get; set; }

        public void Deconstruct(out string Name, out string Property, out string Unit, out string Type)
        {
            Name = this.Name;
            Property = this.Property;
            Unit = this.Unit;
            Type = this.Type;
        }
    }
}
