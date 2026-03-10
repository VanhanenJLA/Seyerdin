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

    public async Task<bool> ExistsCharacterNameAsync(string characterName, string? exceptUserName, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var accounts = await LoadAsync(cancellationToken);
            return accounts.Any(account =>
                !string.Equals(account.UserName, exceptUserName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(account.Character?.Name, characterName, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            gate.Release();
        }
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

    public async Task UpdateAsync(LegacyAccountRecord account, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var accounts = await LoadAsync(cancellationToken);
            var index = accounts.FindIndex(existing =>
                string.Equals(existing.UserName, account.UserName, StringComparison.OrdinalIgnoreCase));

            if (index < 0)
            {
                throw new InvalidOperationException($"Account '{account.UserName}' does not exist.");
            }

            accounts[index] = account;
            await SaveAsync(accounts, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task DeleteAsync(string userName, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var accounts = await LoadAsync(cancellationToken);
            accounts.RemoveAll(existing =>
                string.Equals(existing.UserName, userName, StringComparison.OrdinalIgnoreCase));
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
