using System.Threading;
using FluentAssertions;
using weatherd.datasources;
using weatherd.datasources.pakbus;
using Xunit;

namespace weatherd.tests.datasources.Pakbus
{
    public class PakbusConnectionTests
    {
        private const int LocalNodeID = 2048;
        private const int RemoteNodeID = 1024;

        [Fact]
        public void Start_WithTestInterface_ShouldReturnOK()
        {
            // Arrange
            TestSerialInterface tsi = new TestSerialInterface();
            PakbusConnection connection = new PakbusConnection(tsi, LocalNodeID, RemoteNodeID);
            AutoResetEvent _waiter = new AutoResetEvent(false);
            PakbusResult result = new PakbusResult();
            bool success = true;

            // Act
            bool ok = connection.Start(callbackResult =>
            {
                result = callbackResult;
                _waiter.Set();
            }, b =>
            {
                success = b;
                _waiter.Set();
            });

            _waiter.WaitOne();

            // Assert
            ok.Should().BeTrue();
            success.Should().BeTrue();

            result.Should().NotBeNull();
            result.Get<int>("RECNO").Should().Be(1764);
        }
    }
}
