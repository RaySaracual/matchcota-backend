using System.ComponentModel.DataAnnotations;

namespace Matchcota.Api.Contracts.Discovery;

public sealed record SwipeRequestDto(
    [Required] Guid SourceDogId,
    [Required] Guid TargetDogId,
    bool IsLike);
