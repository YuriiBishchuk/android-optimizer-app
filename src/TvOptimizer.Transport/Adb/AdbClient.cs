using System;
using System.Buffers.Binary;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TvOptimizer.Transport.Adb;

public sealed class AdbClient : IAsyncDisposable
{
    internal const uint CnxN = 0x4E584E43, Auth = 0x48545541;
    internal const uint Open = 0x4E45504F, Okay = 0x59414B4F;
    internal const uint Clse = 0x45534C43, Wrte = 0x45545257;
    private static readonly byte[] Hello = Encoding.ASCII.GetBytes(
        "host::features=shell_v2,cmd,stat_v2,ls_v2,fixed_push_mkdir,apex,abb,abi");

    private readonly string _host;
    private readonly int _port;
    private readonly bool _tls;
    private readonly RSA _rsa;
    private TcpClient? _tcp;
    private Stream? _s;
    private readonly SemaphoreSlim _wl = new(1, 1);
    private readonly TaskCompletionSource<bool> _authed = new();
    private TaskCompletionSource<bool> _done = new();
    private readonly List<byte> _buf = new();
    // Paired device public key (X.509 SubjectPublicKeyInfo) for wireless debugging pairing.
    private byte[]? _pairedDevicePublicKey;

    public AdbClient(string h, int p, bool tls = false, RSA? k = null)
    {
        _host = h;
        _port = p;
        _tls = tls;
        _rsa = k ?? RSA.Create(2048);
    }

    /// <summary>
    /// Pair with the device using TLS and RSA signature exchange (wireless debugging pairing).
    /// </summary>
    public async Task PairAsync(CancellationToken ct = default)
    {
        // Use a temporary connection for pairing.
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(_host, _port, ct);
        Stream s = tcp.GetStream();
        if (_tls)
        {
            var ssl = new SslStream(s, false, (a, b, c, d) => true);
            await ssl.AuthenticateAsClientAsync(_host);
            s = ssl;
        }

        try
        {
            // Step 1: Send CNXN (empty payload)
            await SendPacketAsync(s, CnxN, 0x01000000u, 4096u, Array.Empty<byte>(), ct);

            // Step 2: Receive CNXN from device and extract its public key.
            var hdr = new byte[24];
            await ReadExactAsync(s, hdr, ct);
            uint cmd = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(0, 4));
            uint a0 = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(4, 4));
            uint a1 = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(8, 4));
            uint len = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(12, 4));
            byte[] pl = len > 0 ? new byte[len] : Array.Empty<byte>();
            if (len > 0) await ReadExactAsync(s, pl, ct);

            if (cmd != CnxN)
            {
                throw new IOException("Expected CNXN from device during pairing");
            }

            // The payload of the CNXN is the device's public key.
            _pairedDevicePublicKey = pl;

            // Step 3: Send AUTH (a0=1) with signature of the device's public key.
            var signature = _rsa.SignData(_pairedDevicePublicKey, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
            await SendPacketAsync(s, Auth, 1u, 0u, signature, ct);

            // Step 4: Receive AUTH (a0=2) from device and verify the signature.
            await ReadExactAsync(s, hdr, ct);
            cmd = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(0, 4));
            a0 = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(4, 4));
            a1 = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(8, 4));
            len = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(12, 4));
            pl = len > 0 ? new byte[len] : Array.Empty<byte>();
            if (len > 0) await ReadExactAsync(s, pl, ct);

            if (cmd != Auth || a0 != 2u)
            {
                throw new IOException("Expected AUTH a0=2 from device during pairing");
            }

            using var deviceRsa = RSA.Create();
            deviceRsa.ImportSubjectPublicKeyInfo(_pairedDevicePublicKey, out _);
            if (!deviceRsa.VerifyData(
                    _pairedDevicePublicKey,   // What we signed: the device's public key.
                    pl,
                    HashAlgorithmName.SHA1,
                    RSASignaturePadding.Pkcs1))
            {
                throw new CryptographicException("Failed to verify device's signature during pairing");
            }

            // Pairing successful. The connection will be disposed by the using statement.
        }
        finally
        {
            await s.DisposeAsync();
            tcp.Dispose();
        }
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _tcp = new TcpClient();
        await _tcp.ConnectAsync(_host, _port, ct);
        Stream s = _tcp.GetStream();
        if (_tls)
        {
            var ssl = new SslStream(s, false, (a, b, c, d) => true);
            await ssl.AuthenticateAsClientAsync(_host);
            s = ssl;
        }
        _s = s;
        _ = Task.Run(ReadLoop);

        bool usePublicKeyExchange = _pairedDevicePublicKey != null;

        if (usePublicKeyExchange)
        {
            // Public key exchange authentication (used after pairing).
            // Step 1: Send CNXN (empty payload)
            await SendAsync(CnxN, 0x01000000u, 4096u, Array.Empty<byte>(), ct);

            // Step 2: Receive CNXN from device and get its public key.
            var hdr = new byte[24];
            await ReadExactAsync(s, hdr, ct);
            uint cmd = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(0, 4));
            uint a0 = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(4, 4));
            uint a1 = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(8, 4));
            uint len = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(12, 4));
            byte[] pl = len > 0 ? new byte[len] : Array.Empty<byte>();
            if (len > 0) await ReadExactAsync(s, pl, ct);

            if (cmd != CnxN)
            {
                throw new IOException("Expected CNXN from device");
            }

            byte[] devicePublicKey = pl;

            // Step 3: Send AUTH (a0=1) with signature of the device's public key.
            var signature = _rsa.SignData(devicePublicKey, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
            await SendAsync(Auth, 1u, 0u, signature, ct);

            // Step 4: Receive AUTH (a0=2) from device and verify.
            await ReadExactAsync(s, hdr, ct);
            cmd = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(0, 4));
            a0 = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(4, 4));
            a1 = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(8, 4));
            len = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(12, 4));
            pl = len > 0 ? new byte[len] : Array.Empty<byte>();
            if (len > 0) await ReadExactAsync(s, pl, ct);

            if (cmd != Auth || a0 != 2u)
            {
                throw new IOException("Expected AUTH a0=2 from device");
            }

            using var deviceRsa = RSA.Create();
            deviceRsa.ImportSubjectPublicKeyInfo(devicePublicKey, out _);
            if (!deviceRsa.VerifyData(
                    devicePublicKey,   // What we signed: the device's public key.
                    pl,
                    HashAlgorithmName.SHA1,
                    RSASignaturePadding.Pkcs1))
            {
                throw new CryptographicException("Failed to verify device's signature");
            }

            // Wait for the CNXN from the device that indicates the session is ready.
            await ReadExactAsync(s, hdr, ct);
            cmd = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(0, 4));
            if (cmd != CnxN)
            {
                throw new IOException("Expected CNXN from device after Auth exchange");
            }

            _authed.TrySetResult(true);
        }
        else
        {
            // Fallback to challenge-response authentication.
            await SendAsync(CnxN, 0x01000000u, 4096u, Hello, ct);
            await _authed.Task.WaitAsync(TimeSpan.FromSeconds(15), ct);
        }
    }

    /// <summary>
    /// Execute a shell command and return its combined stdout/stderr as text.
    /// Supports cancellation and timeout.
    /// </summary>
    public async Task<string> ShellAsync(string c, int tSec = 20, CancellationToken ct = default)
    {
        _buf.Clear();
        _done = new TaskCompletionSource<bool>();
        await SendAsync(Open, 1, 0, Encoding.UTF8.GetBytes("shell:" + c), ct);
        await _done.Task.WaitAsync(TimeSpan.FromSeconds(tSec), ct);
        return Encoding.UTF8.GetString(_buf.ToArray());
    }

    private async Task SendAsync(uint cmd, uint a0, uint a1, byte[] pl, CancellationToken ct = default)
    {
        await _wl.WaitAsync(ct);
        try
        {
            var h = new byte[24];
            BinaryPrimitives.WriteUInt32LittleEndian(h.AsSpan(0, 4), cmd);
            BinaryPrimitives.WriteUInt32LittleEndian(h.AsSpan(4, 4), a0);
            BinaryPrimitives.WriteUInt32LittleEndian(h.AsSpan(8, 4), a1);
            BinaryPrimitives.WriteUInt32LittleEndian(h.AsSpan(12, 4), (uint)pl.Length);
            uint sum = 0; foreach (var b in pl) sum += b;
            BinaryPrimitives.WriteUInt32LittleEndian(h.AsSpan(16, 4), sum);
            BinaryPrimitives.WriteUInt32LittleEndian(h.AsSpan(20, 4), cmd ^ 0xFFFFFFFF);
            await _s!.WriteAsync(h, ct);
            if (pl.Length > 0) await _s.WriteAsync(pl, ct);
            await _s.FlushAsync(ct);
        }
        finally { _wl.Release(); }
    }

    private static async Task SendPacketAsync(Stream stream, uint cmd, uint a0, uint a1, byte[] pl, CancellationToken ct)
    {
        var h = new byte[24];
        BinaryPrimitives.WriteUInt32LittleEndian(h.AsSpan(0, 4), cmd);
        BinaryPrimitives.WriteUInt32LittleEndian(h.AsSpan(4, 4), a0);
        BinaryPrimitives.WriteUInt32LittleEndian(h.AsSpan(8, 4), a1);
        BinaryPrimitives.WriteUInt32LittleEndian(h.AsSpan(12, 4), (uint)pl.Length);
        uint sum = 0; foreach (var b in pl) sum += b;
        BinaryPrimitives.WriteUInt32LittleEndian(h.AsSpan(16, 4), sum);
        BinaryPrimitives.WriteUInt32LittleEndian(h.AsSpan(20, 4), cmd ^ 0xFFFFFFFF);
        await stream.WriteAsync(h, ct);
        if (pl.Length > 0) await stream.WriteAsync(pl, ct);
        await stream.FlushAsync(ct);
    }

    private static async Task ReadExactAsync(Stream stream, byte[] b, CancellationToken ct)
    {
        int off = 0;
        while (off < b.Length)
        {
            int n = await stream.ReadAsync(b.AsMemory(off), ct);
            if (n == 0) throw new IOException("ADB closed");
            off += n;
        }
    }

    private async Task ReadLoop()
    {
        var s = _s!;
        var hdr = new byte[24];
        while (true)
        {
            try
            {
                await ReadExactAsync(s, hdr, default);
                uint cmd = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(0, 4));
                uint a0 = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(4, 4));
                uint a1 = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(8, 4));
                uint len = BinaryPrimitives.ReadUInt32LittleEndian(hdr.AsSpan(12, 4));
                byte[] pl = len > 0 ? new byte[len] : Array.Empty<byte>();
                if (len > 0) await ReadExactAsync(s, pl, default);
                if (cmd == Auth && a0 == 1)
                {
                    var sig = _rsa.SignData(pl, HashAlgorithmName.SHA1,
                        RSASignaturePadding.Pkcs1);
                    await SendAsync(Auth, 2, 0, sig);
                }
                else if (cmd == CnxN)
                {
                    _authed.TrySetResult(true);
                }
                else if (cmd == Wrte)
                {
                    await SendAsync(Okay, 1, a0, Array.Empty<byte>());
                    _buf.AddRange(pl);
                }
                else if (cmd == Clse)
                {
                    await SendAsync(Clse, 1, a0, Array.Empty<byte>());
                    _done.TrySetResult(true);
                }
            }
            catch { break; }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_s is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else
            _s?.Dispose();

        _tcp?.Dispose();
        _wl.Dispose();
        await Task.CompletedTask;
    }
}