using System.Text;
using weatherd.datasources.Vaisala.Messages;

namespace weatherd.datasources.Vaisala.Commands
{
    public class VaisalaPollMessageCommand : VaisalaCommand
    {
        public VaisalaMessageType MessageType { get; }

        /// <inheritdoc />
        public VaisalaPollMessageCommand(string sensorId, VaisalaMessageType messageType)
            : base(sensorId)
        {
            MessageType = messageType;
        }

        /// <inheritdoc />
        public override string Compile()
        {
            StringBuilder commandBuilder = new StringBuilder();
            CompileHeader(ref commandBuilder);
            commandBuilder.Append((int) MessageType);
            commandBuilder.Append(CR);

            return commandBuilder.ToString();
        }
    }
}
