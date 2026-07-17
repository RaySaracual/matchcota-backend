using Matchcota.Services.Dogs.Contracts;

namespace Matchcota.Services.Dogs;

public interface IDogsService
{
    Task<IReadOnlyList<DogSummary>> GetMyDogsAsync(Guid userId, CancellationToken cancellationToken);

    Task<DogSummary> CreateDogAsync(Guid userId, CreateDogRequest request, CancellationToken cancellationToken);
}
