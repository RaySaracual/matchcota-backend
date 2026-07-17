using Matchcota.Services.Dogs.Contracts;

namespace Matchcota.Services.Dogs;

public interface IDogsService
{
    Task<IReadOnlyList<DogSummary>> GetMyDogsAsync(Guid userId, CancellationToken cancellationToken);

    Task<DogSummary> CreateDogAsync(Guid userId, CreateDogRequest request, CancellationToken cancellationToken);

    Task<DogSummary> UpdateDogAsync(Guid userId, Guid dogId, UpdateDogRequest request, CancellationToken cancellationToken);

    Task SetDogStatusAsync(Guid userId, Guid dogId, bool isActive, CancellationToken cancellationToken);

    Task<bool> DogBelongsToUserAsync(Guid dogId, Guid userId, CancellationToken cancellationToken);

    Task<string> ReplacePrimaryMediaAsync(
        Guid dogId,
        string mediaUrl,
        string mediaType,
        CancellationToken cancellationToken);
}
