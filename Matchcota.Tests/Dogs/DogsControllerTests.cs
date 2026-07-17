using System.Security.Claims;
using Matchcota.Api.Contracts.Dogs;
using Matchcota.Api.Controllers;
using Matchcota.Services.Dogs;
using Matchcota.Services.Dogs.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Matchcota.Tests.Dogs;

public sealed class DogsControllerTests
{
    [Fact]
    public async Task UpdateDog_ReturnsUnauthorized_WhenUserClaimMissing()
    {
        var dogsService = new FakeDogsService();
        var controller = BuildController(dogsService, hasUserId: false);

        var result = await controller.UpdateDog(
            Guid.NewGuid(),
            new UpdateDogRequestDto("Max", "Poodle", "Bio", null, 18.5, -69.9),
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task UpdateDog_ReturnsOkAndMappedDto_WhenRequestIsValid()
    {
        var dogsService = new FakeDogsService
        {
            UpdateDogResult = new DogSummary(
                Guid.NewGuid(),
                "Max",
                "Poodle",
                "https://cdn/mx.jpg",
                "Bio actualizado",
                true,
                18.5,
                -69.9),
        };
        var controller = BuildController(dogsService, hasUserId: true);

        var dogId = Guid.NewGuid();
        var result = await controller.UpdateDog(
            dogId,
            new UpdateDogRequestDto(" Max ", " Poodle ", "Bio actualizado", null, 18.5, -69.9),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<DogSummaryDto>(ok.Value);

        Assert.Equal("Max", dto.Name);
        Assert.Equal("Poodle", dto.Breed);
        Assert.Equal("Bio actualizado", dto.Bio);
        Assert.Equal(18.5, dto.Latitude);
        Assert.Equal(-69.9, dto.Longitude);

        Assert.Equal(dogId, dogsService.LastUpdateDogId);
        Assert.Equal(" Max ", dogsService.LastUpdateRequest!.Name);
        Assert.Equal(" Poodle ", dogsService.LastUpdateRequest.Breed);
    }

    [Fact]
    public async Task SetDogStatus_ReturnsUnauthorized_WhenUserClaimMissing()
    {
        var dogsService = new FakeDogsService();
        var controller = BuildController(dogsService, hasUserId: false);

        var result = await controller.SetDogStatus(
            Guid.NewGuid(),
            new PatchDogStatusRequestDto(false),
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task SetDogStatus_ReturnsNoContent_AndCallsService()
    {
        var dogsService = new FakeDogsService();
        var controller = BuildController(dogsService, hasUserId: true);

        var dogId = Guid.NewGuid();
        var result = await controller.SetDogStatus(
            dogId,
            new PatchDogStatusRequestDto(false),
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(dogId, dogsService.LastStatusDogId);
        Assert.False(dogsService.LastStatusIsActive);
    }

    private static DogsController BuildController(FakeDogsService dogsService, bool hasUserId)
    {
        var controller = new DogsController(dogsService, new FakeStorageService());

        var claims = new List<Claim>();
        if (hasUserId)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        var user = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user },
        };

        return controller;
    }

    private sealed class FakeStorageService : IStorageService
    {
        public Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken)
            => Task.FromResult($"/uploads/test.{extension}");
    }

    private sealed class FakeDogsService : IDogsService
    {
        public DogSummary UpdateDogResult { get; set; } = new(
            Guid.NewGuid(),
            "Dog",
            "Breed",
            null,
            "",
            true,
            null,
            null);

        public Guid LastUpdateDogId { get; private set; }
        public UpdateDogRequest? LastUpdateRequest { get; private set; }
        public Guid LastStatusDogId { get; private set; }
        public bool LastStatusIsActive { get; private set; }

        public Task<IReadOnlyList<DogSummary>> GetMyDogsAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<DogSummary>>(Array.Empty<DogSummary>());

        public Task<DogSummary> CreateDogAsync(Guid userId, CreateDogRequest request, CancellationToken cancellationToken)
            => Task.FromResult(UpdateDogResult);

        public Task<DogSummary> UpdateDogAsync(Guid userId, Guid dogId, UpdateDogRequest request, CancellationToken cancellationToken)
        {
            LastUpdateDogId = dogId;
            LastUpdateRequest = request;
            return Task.FromResult(UpdateDogResult);
        }

        public Task SetDogStatusAsync(Guid userId, Guid dogId, bool isActive, CancellationToken cancellationToken)
        {
            LastStatusDogId = dogId;
            LastStatusIsActive = isActive;
            return Task.CompletedTask;
        }

        public Task<bool> DogBelongsToUserAsync(Guid dogId, Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task<string> ReplacePrimaryMediaAsync(Guid dogId, string mediaUrl, string mediaType, CancellationToken cancellationToken)
            => Task.FromResult(mediaUrl);
    }
}
