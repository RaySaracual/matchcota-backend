using Matchcota.Core.Entities;
using Matchcota.Infrastructure.Persistence;
using Matchcota.Services.Dogs.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Matchcota.Services.Dogs;

public sealed class DogsService(MatchcotaDbContext dbContext) : IDogsService
{
    private readonly MatchcotaDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<DogSummary>> GetMyDogsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Dogs
            .AsNoTracking()
            .Where(d => d.OwnerId == userId && d.IsActive)
            .OrderBy(d => d.CreatedAtUtc)
            .Select(d => new DogSummary(
                d.Id,
                d.Name,
                d.Breed,
                d.Media.OrderBy(m => m.CreatedAtUtc).Select(m => m.MediaUrl).FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }

    public async Task<DogSummary> CreateDogAsync(
        Guid userId,
        CreateDogRequest request,
        CancellationToken cancellationToken)
    {
        var dog = new Dog
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            Name = request.Name.Trim(),
            Breed = request.Breed.Trim(),
            Bio = (request.Bio ?? string.Empty).Trim(),
            BirthDate = request.BirthDate,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _dbContext.Dogs.Add(dog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DogSummary(dog.Id, dog.Name, dog.Breed, null);
    }
}
