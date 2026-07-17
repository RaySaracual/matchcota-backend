using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Matchcota.Api.Contracts.Chat;
using Matchcota.Services.Chat;
using Matchcota.Services.Safety;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matchcota.Api.Controllers;

[ApiController]
[Route("api/v1/chat")]
[Authorize]
public sealed class ChatController(IChatService chatService, ISafetyService safetyService) : ControllerBase
{
    private readonly IChatService _chatService = chatService;
    private readonly ISafetyService _safetyService = safetyService;

    [HttpGet("conversations")]
    public async Task<ActionResult<IReadOnlyList<ConversationDto>>> GetConversations(
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var blockedDogIds = await _safetyService.GetBlockedDogIdsAsync(userId, cancellationToken);
        var conversations = await _chatService.GetConversationsAsync(userId, cancellationToken, blockedDogIds);

        var dtos = conversations.Select(c => new ConversationDto(
            c.MatchId,
            c.OtherDogId,
            c.OtherDogName,
            c.OtherDogBreed,
            c.OtherDogPhotoUrl,
            c.LastMessageContent,
            c.LastMessageSentAtUtc,
            c.MyDogId,
            c.UnreadCount)).ToList();

        return Ok(dtos);
    }

    [HttpPost("{matchId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        try
        {
            await _chatService.MarkAsReadAsync(userId, matchId, cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

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
