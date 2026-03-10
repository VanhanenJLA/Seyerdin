using System.Net;
using System.Net.Sockets;
using Seyerdin.ServerModernized.Configuration;
using Seyerdin.ServerModernized.Domain;
using Seyerdin.ServerModernized.Infrastructure;
using Seyerdin.ServerModernized.Networking;
using Seyerdin.ServerModernized.Protocol;

namespace Seyerdin.ServerModernized.Hosting;

public sealed class LegacyCompatibilityServer
{
    private readonly ServerOptions options;
    private readonly TcpListener listener;
    private readonly ILegacyAccountStore accountStore;
    private readonly LegacyClassCatalog classCatalog;
    private readonly HashSet<string> activeUsers = new(StringComparer.OrdinalIgnoreCase);
    private readonly object activeUsersGate = new();
    private byte nextPlayerIndex = 1;

    public LegacyCompatibilityServer(ServerOptions options)
    {
        this.options = options;
        listener = new TcpListener(IPAddress.Any, options.Port);
        accountStore = new JsonLegacyAccountStore(options.AccountsFilePath);
        classCatalog = LegacyClassCatalog.Load(options.ClassesFilePath);
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

                    var shouldClose = await HandlePacketAsync(connection, stream, packet, cancellationToken);
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
        finally
        {
            if (!string.IsNullOrWhiteSpace(connection.UserName))
            {
                lock (activeUsersGate)
                {
                    activeUsers.Remove(connection.UserName);
                }
            }
        }
    }

    private Task<bool> HandlePacketAsync(
        ConnectionContext connection,
        NetworkStream stream,
        LegacyPacket packet,
        CancellationToken cancellationToken)
    {
        return connection.State switch
        {
            LegacySessionState.NotConnected => HandlePreLoginPacketAsync(connection, stream, packet, cancellationToken),
            LegacySessionState.Connected => HandleConnectedPacketAsync(connection, stream, packet, cancellationToken),
            LegacySessionState.Playing => HandlePlayingPacketAsync(connection, stream, packet, cancellationToken),
            _ => HandleConnectedPacketAsync(connection, stream, packet, cancellationToken),
        };
    }

    private async Task<bool> HandlePreLoginPacketAsync(
        ConnectionContext connection,
        NetworkStream stream,
        LegacyPacket packet,
        CancellationToken cancellationToken)
    {
        switch (packet.PacketId)
        {
            case 0:
                return await HandleCreateAccountAsync(connection, stream, packet, cancellationToken);
            case 1:
                return await HandleLogOnAsync(connection, stream, packet, cancellationToken);
            case 5:
                await SendRegistryPingResponseAsync(stream, cancellationToken);
                return true;
            case 29:
                return false;
            case 61:
                return await HandleVersionAsync(connection, stream, packet, cancellationToken);
            case 92:
                connection.UniqueId = LegacyEncoding.GetString(packet.PayloadMemory.Span);
                return false;
            case 93:
                connection.IniUniqueId = LegacyEncoding.GetString(packet.PayloadMemory.Span);
                return false;
            case 255:
                await SendRawStringAsync(stream, $"{GetOnlineUserCount()}\0", cancellationToken);
                return true;
            default:
                Console.WriteLine($"Ignoring unimplemented pre-login packet {packet.PacketId} ({packet.Payload.Length} bytes).");
                return false;
        }
    }

    private Task<bool> HandleConnectedPacketAsync(
        ConnectionContext connection,
        NetworkStream stream,
        LegacyPacket packet,
        CancellationToken cancellationToken)
    {
        return packet.PacketId switch
        {
            2 => HandleCreateCharacterAsync(connection, stream, packet, cancellationToken),
            3 => HandleChangePasswordAsync(connection, stream, packet, cancellationToken),
            4 => HandleDeleteAccountAsync(connection, stream, packet, cancellationToken),
            5 => HandlePlayAsync(connection, stream, cancellationToken),
            _ => LogIgnoredConnectedPacket(packet),
        };
    }

    private Task<bool> LogIgnoredConnectedPacket(LegacyPacket packet)
    {
        Console.WriteLine($"Ignoring unimplemented connected packet {packet.PacketId} ({packet.Payload.Length} bytes).");
        return Task.FromResult(false);
    }

    private Task<bool> LogIgnoredPlayingPacket(LegacyPacket packet)
    {
        Console.WriteLine($"Ignoring unimplemented playing packet {packet.PacketId} ({packet.Payload.Length} bytes).");
        return Task.FromResult(false);
    }

    private async Task<bool> HandleVersionAsync(
        ConnectionContext connection,
        NetworkStream stream,
        LegacyPacket packet,
        CancellationToken cancellationToken)
    {
        if (packet.Payload.Length != 5)
        {
            return true;
        }

        connection.ClientVersion = packet.Payload[0];
        if (connection.ClientVersion == options.CurrentClientVersion)
        {
            return false;
        }

        await SendPacketAsync(stream, 0, new byte[] { 6 }, cancellationToken);
        return true;
    }

    private async Task<bool> HandleCreateAccountAsync(
        ConnectionContext connection,
        NetworkStream stream,
        LegacyPacket packet,
        CancellationToken cancellationToken)
    {
        if (connection.ClientVersion != options.CurrentClientVersion)
        {
            await SendOutdatedClientAsync(stream, cancellationToken);
            return true;
        }

        var payloadText = LegacyEncoding.GetString(packet.PayloadMemory.Span);
        var firstSeparator = payloadText.IndexOf('\0');
        if (firstSeparator <= 0 || firstSeparator >= payloadText.Length - 1)
        {
            return true;
        }

        var userName = payloadText[..firstSeparator].Trim();
        var remainder = payloadText[(firstSeparator + 1)..];
        var secondSeparator = remainder.IndexOf('\0');
        if (secondSeparator < 0)
        {
            return true;
        }

        var password = remainder[..secondSeparator].Trim();
        var email = remainder[(secondSeparator + 1)..].Trim();

        if (userName.Length is < 3 or > 15 || !LegacyEncoding.IsValidName(userName))
        {
            return true;
        }

        if (await accountStore.ExistsByUserNameAsync(userName, cancellationToken))
        {
            await SendPacketAsync(stream, 1, new byte[] { 1 }, cancellationToken);
            return true;
        }

        var account = new LegacyAccountRecord
        {
            UserName = userName,
            PasswordCipherText = LegacyEncoding.Cryp(password.Length > 64 ? password[..64] : password),
            Email = email.Length > 100 ? email[..100] : email,
            Access = 0,
            Character = null,
        };

        await accountStore.CreateAsync(account, cancellationToken);
        await SendPacketAsync(stream, 2, ReadOnlyMemory<byte>.Empty, cancellationToken);
        return true;
    }

    private async Task<bool> HandleLogOnAsync(
        ConnectionContext connection,
        NetworkStream stream,
        LegacyPacket packet,
        CancellationToken cancellationToken)
    {
        if (connection.ClientVersion != options.CurrentClientVersion)
        {
            await SendOutdatedClientAsync(stream, cancellationToken);
            return true;
        }

        var payloadText = LegacyEncoding.GetString(packet.PayloadMemory.Span);
        var separator = payloadText.IndexOf('\0');
        if (separator <= 0 || separator >= payloadText.Length - 1)
        {
            return true;
        }

        var userName = payloadText[..separator];
        var suppliedPassword = payloadText[(separator + 1)..];
        var account = await accountStore.FindByUserNameAsync(userName, cancellationToken);

        if (account is null || suppliedPassword != LegacyEncoding.Cryp(account.PasswordCipherText))
        {
            await SendPacketAsync(stream, 0, new byte[] { 1 }, cancellationToken);
            return true;
        }

        var alreadyOnline = false;
        lock (activeUsersGate)
        {
            if (activeUsers.Contains(userName))
            {
                alreadyOnline = true;
            }
            else
            {
                activeUsers.Add(userName);
            }
        }

        if (alreadyOnline)
        {
            await SendPacketAsync(stream, 0, new byte[] { 2 }, cancellationToken);
            return true;
        }

        connection.UserName = userName;
        connection.Account = account;
        connection.State = LegacySessionState.Connected;

        var playerIndex = ReservePlayerIndex();
        var characterPayload = LegacyPacketWriter.BuildCharacterDataPayload(account.Character, account.Access, playerIndex);
        await SendPacketBodyAsync(stream, characterPayload, cancellationToken);

        if (!string.IsNullOrWhiteSpace(options.Motd))
        {
            await SendPacketAsync(stream, 4, LegacyEncoding.GetBytes(options.Motd), cancellationToken);
        }

        return false;
    }

    private async Task<bool> HandleCreateCharacterAsync(
        ConnectionContext connection,
        NetworkStream stream,
        LegacyPacket packet,
        CancellationToken cancellationToken)
    {
        if (connection.Account is null)
        {
            return true;
        }

        var payload = packet.Payload;
        if (payload.Length < 4)
        {
            return true;
        }

        var separatorIndex = Array.IndexOf(payload, (byte)0, 3);
        if (separatorIndex < 0)
        {
            return true;
        }

        var characterName = LegacyEncoding.GetString(payload.AsSpan(3, separatorIndex - 3)).Trim();

        if (characterName.Length is < 3 or > 16 || !LegacyEncoding.IsValidName(characterName))
        {
            return true;
        }

        if (await accountStore.ExistsCharacterNameAsync(characterName, connection.UserName, cancellationToken))
        {
            await SendPacketAsync(stream, 13, ReadOnlyMemory<byte>.Empty, cancellationToken);
            return false;
        }

        var selectedClass = classCatalog.ResolveOrDefault(payload[0]);
        var gender = payload[2] > 1 ? (byte)1 : payload[2];
        var sprite = (byte)(181 + ((selectedClass.Id - 1) * 8) + (gender * 4));

        connection.Account.Character = new CharacterRecord
        {
            Name = characterName,
            Level = 1,
            ClassId = selectedClass.Id,
            Gender = gender,
            Sprite = sprite,
            Hp = selectedClass.StartHp,
            Energy = selectedClass.StartEnergy,
            Mana = selectedClass.StartMana,
            MaxHp = selectedClass.StartHp,
            MaxEnergy = selectedClass.StartEnergy,
            MaxMana = selectedClass.StartMana,
            Strength = selectedClass.StartStrength,
            Agility = selectedClass.StartAgility,
            Endurance = selectedClass.StartEndurance,
            Wisdom = selectedClass.StartWisdom,
            Constitution = selectedClass.StartConstitution,
            Intelligence = selectedClass.StartIntelligence,
            Status = 2,
            GuildId = 0,
            GuildRank = 0,
            Experience = 0,
            Squelched = 0,
            StatusEffect = 0,
            StatPoints = 3,
            SkillPoints = 3,
            GuildName = string.Empty,
        };

        await accountStore.UpdateAsync(connection.Account, cancellationToken);

        var playerIndex = ReservePlayerIndex();
        var characterPayload = LegacyPacketWriter.BuildCharacterDataPayload(
            connection.Account.Character,
            connection.Account.Access,
            playerIndex);
        await SendPacketBodyAsync(stream, characterPayload, cancellationToken);
        return false;
    }

    private async Task<bool> HandleChangePasswordAsync(
        ConnectionContext connection,
        NetworkStream stream,
        LegacyPacket packet,
        CancellationToken cancellationToken)
    {
        if (connection.Account is null || packet.Payload.Length == 0)
        {
            return true;
        }

        var newPassword = LegacyEncoding.GetString(packet.PayloadMemory.Span);
        connection.Account.PasswordCipherText = LegacyEncoding.Cryp(newPassword);
        await accountStore.UpdateAsync(connection.Account, cancellationToken);
        await SendPacketAsync(stream, 5, ReadOnlyMemory<byte>.Empty, cancellationToken);
        return false;
    }

    private async Task<bool> HandleDeleteAccountAsync(
        ConnectionContext connection,
        NetworkStream stream,
        LegacyPacket packet,
        CancellationToken cancellationToken)
    {
        if (connection.Account is null)
        {
            return true;
        }

        if (packet.Payload.Length != 0)
        {
            return true;
        }

        await accountStore.DeleteAsync(connection.Account.UserName, cancellationToken);
        connection.Account = null;
        return true;
    }

    private async Task<bool> HandlePlayAsync(
        ConnectionContext connection,
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        if (connection.Account?.Character is null || connection.Account.Character.Level == 0)
        {
            return true;
        }

        connection.State = LegacySessionState.Playing;
        await SendPacketBodyAsync(stream, LegacyWorldPacketFactory.BuildJoinedGameBody(), cancellationToken);
        await SendPacketBodyAsync(stream, LegacyWorldPacketFactory.BuildHourBody(12), cancellationToken);
        await SendPacketBodyAsync(stream, LegacyWorldPacketFactory.BuildJoinedMapBody(connection.Account.Character), cancellationToken);
        return false;
    }

    private async Task<bool> HandlePlayingPacketAsync(
        ConnectionContext connection,
        NetworkStream stream,
        LegacyPacket packet,
        CancellationToken cancellationToken)
    {
        switch (packet.PacketId)
        {
            case 29:
                return false;
            case 30:
                return true;
            case 45:
                if (packet.Payload.Length != 0)
                {
                    return true;
                }

                await SendPacketBodyAsync(stream, LegacyWorldPacketFactory.BuildMapDataBody(), cancellationToken);
                await SendPacketBodyAsync(stream, LegacyWorldPacketFactory.BuildDoneSendingMapBody(), cancellationToken);
                return false;
            case 92:
                return false;
            default:
                return await LogIgnoredPlayingPacket(packet);
        }
    }

    private async Task SendRegistryPingResponseAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        Span<byte> payload = stackalloc byte[5];
        payload[0] = (byte)Math.Clamp(GetOnlineUserCount(), 0, byte.MaxValue);

        var version = options.CurrentClientVersion;
        payload[1] = (byte)((version >> 24) & 0xFF);
        payload[2] = (byte)((version >> 16) & 0xFF);
        payload[3] = (byte)((version >> 8) & 0xFF);
        payload[4] = (byte)(version & 0xFF);

        var frame = LegacyProtocolCodec.CreateServerRawFrame(payload);
        await stream.WriteAsync(frame, cancellationToken);
    }

    private Task SendOutdatedClientAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var message = $"Your client is outdated, please visit {options.DownloadSite}! Download the newest update and unzip it into your Seyerdin Online folder.";
        return SendPacketAsync(stream, 0, LegacyEncoding.GetBytes("\0" + message), cancellationToken);
    }

    private Task SendPacketAsync(
        NetworkStream stream,
        byte packetId,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        var frame = LegacyProtocolCodec.CreateServerPacket(packetId, payload.Span);
        return stream.WriteAsync(frame, cancellationToken).AsTask();
    }

    private Task SendPacketBodyAsync(NetworkStream stream, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        var frame = LegacyProtocolCodec.CreateServerRawFrame(body.Span);
        return stream.WriteAsync(frame, cancellationToken).AsTask();
    }

    private Task SendRawStringAsync(NetworkStream stream, string value, CancellationToken cancellationToken)
    {
        return SendPacketBodyAsync(stream, LegacyEncoding.GetBytes(value), cancellationToken);
    }

    private int GetOnlineUserCount()
    {
        lock (activeUsersGate)
        {
            return activeUsers.Count;
        }
    }

    private byte ReservePlayerIndex()
    {
        if (nextPlayerIndex == byte.MaxValue)
        {
            nextPlayerIndex = 1;
        }

        return nextPlayerIndex++;
    }
}
