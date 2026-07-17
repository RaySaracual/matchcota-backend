using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Matchcota.Api.Contracts.Chat;
using Matchcota.Services.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matchcota.Api.Controllers;

[ApiController]
[Route("api/v1/chat")]
[Authorize]
public sealed class ChatController(IChatService chatService) : ControllerBase
{
    private readonly IChatService _chatService = chatService;

    [HttpGet("{matchId:guid}/messages")]
    public async Task<ActionResult<ChatMessagesPageDto>> GetMessages(
        Guid matchId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var result = await _chatService.GetMessagesAsync(userId, matchId, page, pageSize, cancellationToken);

        var dto = new ChatMessagesPageDto(
            result.Items.Select(m => new ChatMessageDto(m.MessageId, m.MatchId, m.SenderDogId, m.Content, m.SentAtUtc)).ToList(),
            result.Page,
            result.PageSize,
            result.HasMore);

        return Ok(dto);
    }

    [HttpPost("{matchId:guid}/messages")]
    public async Task<ActionResult<ChatMessageDto>> SendMessage(
        Guid matchId,
        [FromBody] SendChatMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var message = await _chatService.SendMessageAsync(
            userId,
            matchId,
            request.SenderDogId,
            request.Content,
            cancellationToken);

        return Ok(new ChatMessageDto(
            message.MessageId,
            message.MatchId,
            message.SenderDogId,
            message.Content,
            message.SentAtUtc));
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
