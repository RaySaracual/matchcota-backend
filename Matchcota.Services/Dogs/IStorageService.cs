namespace Matchcota.Services.Dogs;

public interface IStorageService
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken);
}
