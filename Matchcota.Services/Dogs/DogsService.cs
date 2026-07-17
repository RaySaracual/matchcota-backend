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
            .Where(d => d.OwnerId == userId)
            .OrderBy(d => d.CreatedAtUtc)
            .Select(d => new DogSummary(
                d.Id,
                d.Name,
                d.Breed,
                d.Media.OrderBy(m => m.CreatedAtUtc).Select(m => m.MediaUrl).FirstOrDefault(),
                d.Bio,
                d.IsActive,
                d.Latitude,
                d.Longitude))
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

        return new DogSummary(dog.Id, dog.Name, dog.Breed, null, dog.Bio, dog.IsActive, dog.Latitude, dog.Longitude);
    }

    public async Task<DogSummary> UpdateDogAsync(
        Guid userId,
        Guid dogId,
        UpdateDogRequest request,
        CancellationToken cancellationToken)
    {
        var dog = await _dbContext.Dogs
            .FirstOrDefaultAsync(d => d.Id == dogId && d.OwnerId == userId, cancellationToken);

        if (dog is null)
            throw new UnauthorizedAccessException("Dog not found or does not belong to user.");

        dog.Name = request.Name.Trim();
        dog.Breed = request.Breed.Trim();
        dog.Bio = (request.Bio ?? string.Empty).Trim();
        dog.BirthDate = request.BirthDate;
        dog.Latitude = request.Latitude;
        dog.Longitude = request.Longitude;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var photo = await _dbContext.DogMedia
            .AsNoTracking()
            .Where(m => m.DogId == dogId)
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => m.MediaUrl)
            .FirstOrDefaultAsync(cancellationToken);

        return new DogSummary(dog.Id, dog.Name, dog.Breed, photo, dog.Bio, dog.IsActive, dog.Latitude, dog.Longitude);
    }

    public async Task SetDogStatusAsync(
        Guid userId,
        Guid dogId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var dog = await _dbContext.Dogs
            .FirstOrDefaultAsync(d => d.Id == dogId && d.OwnerId == userId, cancellationToken);

        if (dog is null)
            throw new UnauthorizedAccessException("Dog not found or does not belong to user.");

        dog.IsActive = isActive;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DogBelongsToUserAsync(
        Guid dogId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Dogs
            .AsNoTracking()
            .AnyAsync(d => d.Id == dogId && d.OwnerId == userId && d.IsActive, cancellationToken);
    }

    public async Task<string> ReplacePrimaryMediaAsync(
        Guid dogId,
        string mediaUrl,
        string mediaType,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.DogMedia
            .Where(m => m.DogId == dogId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _dbContext.DogMedia.RemoveRange(existing);
        }

        var media = new DogMedia
        {
            Id = Guid.NewGuid(),
            DogId = dogId,
            MediaUrl = mediaUrl,
            MediaType = mediaType,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _dbContext.DogMedia.Add(media);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return media.MediaUrl;
    }
}
