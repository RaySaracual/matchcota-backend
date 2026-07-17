using System.ComponentModel.DataAnnotations;

namespace Matchcota.Api.Contracts.Chat;

public sealed record SendChatMessageRequestDto(
    [Required] Guid SenderDogId,
    [Required] string Content);
