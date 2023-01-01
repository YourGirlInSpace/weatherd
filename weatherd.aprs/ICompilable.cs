namespace weatherd.aprs
{
    public interface ICompilable
    {
        /// <summary>
        /// Compiles this object into a human or machine readable string.
        /// </summary>
        /// <returns>A human or machine readable string.</returns>
        string Compile();
    }
}
