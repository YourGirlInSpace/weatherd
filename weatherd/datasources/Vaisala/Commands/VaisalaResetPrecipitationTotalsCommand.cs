using System.Text;

namespace weatherd.datasources.Vaisala.Commands
{
    public class VaisalaResetPrecipitationTotalsCommand : VaisalaCommand
    {
        /// <inheritdoc />
        public VaisalaResetPrecipitationTotalsCommand(string sensorId)
            : base(sensorId)
        { }

        /// <inheritdoc />
        public override string Compile()
        {
            StringBuilder commandBuilder = new StringBuilder();
            CompileHeader(ref commandBuilder);
            commandBuilder.Append("C");
            commandBuilder.Append(CR);

            return commandBuilder.ToString();
        }
    }
}
