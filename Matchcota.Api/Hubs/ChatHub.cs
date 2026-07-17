using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Matchcota.Api.Contracts.Chat;
using Matchcota.Services.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Matchcota.Api.Hubs;

[Authorize]
public sealed class ChatHub(IChatService chatService, ILogger<ChatHub> logger) : Hub
{
    private readonly IChatService _chatService = chatService;
    private readonly ILogger<ChatHub> _logger = logger;

    public async Task JoinMatch(Guid matchId)
    {
        var userId = GetUserId();
        await _chatService.EnsureUserBelongsToMatchAsync(userId, matchId, Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(matchId));
        _logger.LogInformation(
            "Chat event={ChatEvent} matchId={MatchId} connectionId={ConnectionId}",
            "join",
            matchId,
            Context.ConnectionId);
    }

    public async Task LeaveMatch(Guid matchId)
    {
        _ = GetUserId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(matchId));
        _logger.LogInformation(
            "Chat event={ChatEvent} matchId={MatchId} connectionId={ConnectionId}",
            "leave",
            matchId,
            Context.ConnectionId);
    }

    public async Task<ChatMessageDto> SendMessage(Guid matchId, Guid senderDogId, string content)
    {
        var userId = GetUserId();

        ChatMessageDto dto;
        try
        {
            var message = await _chatService.SendMessageAsync(
                userId,
                matchId,
                senderDogId,
                content,
                Context.ConnectionAborted);

            dto = new ChatMessageDto(
                message.MessageId,
                message.MatchId,
                message.SenderDogId,
                message.Content,
                message.SentAtUtc);

            _logger.LogInformation(
                "Chat event={ChatEvent} messageId={MessageId} matchId={MatchId} senderDogId={SenderDogId} contentLength={ContentLength}",
                "send",
                dto.MessageId,
                matchId,
                senderDogId,
                content.Length);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Chat event={ChatEvent} matchId={MatchId} senderDogId={SenderDogId} connectionId={ConnectionId}",
                "error",
                matchId,
                senderDogId,
                Context.ConnectionId);
            throw;
        }

        await Clients.Group(GroupName(matchId)).SendAsync("message_received", dto, Context.ConnectionAborted);
        return dto;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Chat event={ChatEvent} connectionId={ConnectionId}", "connected", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is null)
            _logger.LogInformation("Chat event={ChatEvent} connectionId={ConnectionId}", "disconnected", Context.ConnectionId);
        else
            _logger.LogWarning(exception,
                "Chat event={ChatEvent} connectionId={ConnectionId}", "error", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    private Guid GetUserId()
    {
        var claim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? Context.User?.FindFirstValue("sub");

        if (!Guid.TryParse(claim, out var userId) || userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Invalid user identity.");
        }

        return userId;
    }

    private static string GroupName(Guid matchId) => $"match:{matchId:D}";
}
