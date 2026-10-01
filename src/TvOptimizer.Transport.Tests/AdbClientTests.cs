using System.Net.Security;
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers.Binary;
using TvOptimizer.Transport.Adb;
using Xunit;

namespace TvOptimizer.Transport.Tests
{
    public class AdbClientTests
    {
        [Fact]
        public async Task PairAsync_PerformsPairingProtocolAndCompletesSuccessfully()
        {
            // Arrange
            var hostRsa = RSA.Create(2048);
            var deviceRsa = RSA.Create(2048);

            var hostPublicKeyBytes = hostRsa.ExportSubjectPublicKeyInfo();
            var devicePublicKeyBytes = deviceRsa.ExportSubjectPublicKeyInfo();

            var pairingPort = GetRandomPort();
            var host = "127.0.0.1";

            // Create a self-signed certificate for the server (for TLS)
            var certificate = CreateTestCertificate();

            // Start a fake device server that implements the pairing protocol (with TLS)
            var cts = new CancellationTokenSource();
            var serverTask = Task.Run(() => FakeDeviceServerAsync(host, pairingPort, certificate, hostPublicKeyBytes, devicePublicKeyBytes, hostRsa, deviceRsa, cts.Token));

            // Act
            await using var client = new AdbClient(host, pairingPort, tls: true, hostRsa);
            await client.PairAsync(CancellationToken.None);

            // Assert
            cts.Cancel();
            await serverTask;
        }

        private static int GetRandomPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private async Task FakeDeviceServerAsync(string host, int port, X509Certificate2 serverCertificate, byte[] hostPublicKey, byte[] devicePublicKey, RSA hostRsa, RSA deviceRsa, CancellationToken ct)
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync(ct);
                    _ = HandleClientAsync(client, serverCertificate, hostPublicKey, devicePublicKey, hostRsa, deviceRsa, ct);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                listener.Stop();
            }
        }

        private async Task HandleClientAsync(TcpClient client, X509Certificate2 serverCertificate, byte[] hostPublicKey, byte[] devicePublicKey, RSA hostRsa, RSA deviceRsa, CancellationToken ct)
        {
            using var netStream = client.GetStream();
            // Server side of TLS: authenticate as server using the certificate.
            using var sslStream = new SslStream(netStream, false, (sender, certificate, chain, errors) => true);
            await sslStream.AuthenticateAsServerAsync(serverCertificate, clientCertificateRequired: false, enabledSslProtocols: System.Security.Authentication.SslProtocols.Tls12, checkCertificateRevocation: false);

            var buffer = new byte[24];

            // Read initial CNXN from host
            await ReadExactAsync(sslStream, buffer, ct);
            var cmd = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(0, 4));
            var a0 = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(4, 4));
            var a1 = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(8, 4));
            var length = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(12, 4));
            var sum = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(16, 4));
            var magic = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(20, 4));

            // CNXN command is 0x4E584E43
            Assert.Equal(0x4E584E43u, cmd);
            Assert.Equal(0x01000000u, a0);
            Assert.Equal(4096u, a1);

            // Read host public key payload
            var hostKeyPayload = new byte[length];
            await ReadExactAsync(sslStream, hostKeyPayload, ct);

            // Verify the payload contains the host public key (we expect it to be exactly the public key)
            Assert.Equal(hostPublicKey, hostKeyPayload);

            // Now send CNXN from device with device public key
            await SendPacketAsync(sslStream, 0x4E584E43u, 0u, 0u, devicePublicKey, ct);

            // Read AUTH from host (a0=1)
            await ReadExactAsync(sslStream, buffer, ct);
            cmd = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(0, 4));
            a0 = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(4, 4));
            a1 = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(8, 4));
            length = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(12, 4));
            sum = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(16, 4));
            magic = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(20, 4));

            // AUTH command is 0x48545541
            Assert.Equal(0x48545541u, cmd);
            Assert.Equal(1u, a0);

            var authPayload = new byte[length];
            await ReadExactAsync(sslStream, authPayload, ct);

            // Verify the AUTH payload is a signature of the device public key by the host
            bool verified = false;
            try
            {
                verified = hostRsa.VerifyData(
                    devicePublicKey,
                    authPayload,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
            }
            catch
            {
                verified = false;
            }

            Assert.True(verified, "Host signature verification failed");

            // Send AUTH from device (a0=2) with signature of device public key
            var authSignature = deviceRsa.SignData(devicePublicKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            await SendPacketAsync(sslStream, 0x48545541u, 2u, 0u, authSignature, ct);

            // Wait for client to close (or we can close after a delay)
            await Task.Delay(100, ct);
        }

        private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken ct)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(offset), ct);
                if (read == 0)
                    throw new IOException("Connection closed");
                offset += read;
            }
        }

        private static async Task SendPacketAsync(Stream stream, uint cmd, uint a0, uint a1, byte[] payload, CancellationToken ct)
        {
            var hdr = new byte[24];
            BinaryPrimitives.WriteUInt32LittleEndian(hdr.AsSpan(0, 4), cmd);
            BinaryPrimitives.WriteUInt32LittleEndian(hdr.AsSpan(4, 4), a0);
            BinaryPrimitives.WriteUInt32LittleEndian(hdr.AsSpan(8, 4), a1);
            BinaryPrimitives.WriteUInt32LittleEndian(hdr.AsSpan(12, 4), (uint)payload.Length);
            uint sum = 0;
            foreach (var b in payload) sum += b;
            BinaryPrimitives.WriteUInt32LittleEndian(hdr.AsSpan(16, 4), sum);
            BinaryPrimitives.WriteUInt32LittleEndian(hdr.AsSpan(20, 4), cmd ^ 0xFFFFFFFF);
            await stream.WriteAsync(hdr, ct);
            if (payload.Length > 0)
                await stream.WriteAsync(payload, ct);
            await stream.FlushAsync(ct);
        }

        /// <summary>
        /// Creates a self-signed certificate for testing purposes.
        /// </summary>
        private static X509Certificate2 CreateTestCertificate()
        {
            var distinguishedName = new X500DistinguishedName("CN=localhost");
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, false));
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
            var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
            return new X509Certificate2(certificate.Export(X509ContentType.Pfx, "test"), "test");
        }
    }
}