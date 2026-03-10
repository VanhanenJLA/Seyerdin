using Seyerdin.ServerModernized.Domain;

namespace Seyerdin.ServerModernized.Infrastructure;

public interface ILegacyAccountStore
{
    Task<LegacyAccountRecord?> FindByUserNameAsync(string userName, CancellationToken cancellationToken);

    Task<bool> ExistsByUserNameAsync(string userName, CancellationToken cancellationToken);

    Task<bool> ExistsCharacterNameAsync(string characterName, string? exceptUserName, CancellationToken cancellationToken);

    Task CreateAsync(LegacyAccountRecord account, CancellationToken cancellationToken);

    Task UpdateAsync(LegacyAccountRecord account, CancellationToken cancellationToken);
}
