using System.Net;
using System.Net.Sockets;
using Seyerdin.ServerModernized.Configuration;
using Seyerdin.ServerModernized.Networking;
using Seyerdin.ServerModernized.Protocol;

namespace Seyerdin.ServerModernized.Hosting;

public sealed class LegacyCompatibilityServer
{
    private readonly ServerOptions options;
    private readonly TcpListener listener;

    public LegacyCompatibilityServer(ServerOptions options)
    {
        this.options = options;
        listener = new TcpListener(IPAddress.Any, options.Port);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        listener.Start();
        Console.WriteLine("Listening for legacy TCP connections.");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken);
                _ = Task.Run(() => HandleClientAsync(new ConnectionContext(client), cancellationToken), cancellationToken);
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

    private async Task HandleClientAsync(ConnectionContext connection, CancellationToken cancellationToken)
    {
        using var client = connection.Client;
        using var stream = client.GetStream();
        var readBuffer = new byte[1024];

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await stream.ReadAsync(readBuffer, cancellationToken);
                if (bytesRead == 0)
                {
                    return;
                }

                connection.Buffer.AddRange(readBuffer[..bytesRead]);

                while (LegacyProtocolCodec.TryReadPacket(
                           connection.Buffer,
                           connection.PacketsRead,
                           out var packet))
                {
                    connection.PacketsRead++;
                    if (packet is null)
                    {
                        continue;
                    }

                    var shouldClose = await HandlePacketAsync(stream, packet, cancellationToken);
                    if (shouldClose)
                    {
                        return;
                    }
                }
            }
        }
        catch (InvalidDataException ex)
        {
            Console.WriteLine($"Dropped client due to invalid packet: {ex.Message}");
        }
        catch (IOException)
        {
        }
        catch (SocketException)
        {
        }
    }

    private async Task<bool> HandlePacketAsync(NetworkStream stream, LegacyPacket packet, CancellationToken cancellationToken)
    {
        switch (packet.PacketId)
        {
            case 5:
                await SendRegistryPingResponseAsync(stream, cancellationToken);
                return true;
            default:
                Console.WriteLine($"Ignoring unimplemented legacy packet {packet.PacketId} ({packet.Payload.Length} bytes).");
                return false;
        }
    }

    private Task SendRegistryPingResponseAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        Span<byte> payload = stackalloc byte[5];
        payload[0] = 0;

        var version = options.CurrentClientVersion;
        payload[1] = (byte)((version >> 24) & 0xFF);
        payload[2] = (byte)((version >> 16) & 0xFF);
        payload[3] = (byte)((version >> 8) & 0xFF);
        payload[4] = (byte)(version & 0xFF);

        var frame = LegacyProtocolCodec.CreateServerRawFrame(payload);
        return stream.WriteAsync(frame, cancellationToken).AsTask();
    }
}
