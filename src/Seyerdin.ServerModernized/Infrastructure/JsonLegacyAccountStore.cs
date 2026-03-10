using System.Text.Json;
using Seyerdin.ServerModernized.Domain;

namespace Seyerdin.ServerModernized.Infrastructure;

public sealed class JsonLegacyAccountStore : ILegacyAccountStore
{
    private readonly string path;
    private readonly SemaphoreSlim gate = new(1, 1);
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    public JsonLegacyAccountStore(string path)
    {
        this.path = path;
    }

    public async Task<LegacyAccountRecord?> FindByUserNameAsync(string userName, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var accounts = await LoadAsync(cancellationToken);
            return accounts.FirstOrDefault(account =>
                string.Equals(account.UserName, userName, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<bool> ExistsByUserNameAsync(string userName, CancellationToken cancellationToken)
    {
        return await FindByUserNameAsync(userName, cancellationToken) is not null;
    }

    public async Task CreateAsync(LegacyAccountRecord account, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var accounts = await LoadAsync(cancellationToken);
            if (accounts.Any(existing =>
                    string.Equals(existing.UserName, account.UserName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Account '{account.UserName}' already exists.");
            }

            accounts.Add(account);
            await SaveAsync(accounts, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<List<LegacyAccountRecord>> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return new List<LegacyAccountRecord>();
        }

        await using var stream = File.OpenRead(path);
        var accounts = await JsonSerializer.DeserializeAsync<List<LegacyAccountRecord>>(
            stream,
            SerializerOptions,
            cancellationToken);

        return accounts ?? new List<LegacyAccountRecord>();
    }

    private async Task SaveAsync(List<LegacyAccountRecord> accounts, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, accounts, SerializerOptions, cancellationToken);
    }
}
