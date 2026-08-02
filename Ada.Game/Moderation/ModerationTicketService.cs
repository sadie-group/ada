using System.Collections.Concurrent;
using Ada.API.DTOs.Moderation;
using Ada.API.Interfaces.Game.Moderation;
using Ada.Core.Enums.Game.Moderation;
using Ada.Db;
using Ada.Db.Models.Moderation;
using Microsoft.EntityFrameworkCore;

namespace Ada.Game.Moderation;

public class ModerationTicketService(IDbContextFactory<AdaDbContext> dbContextFactory) : IModerationTicketService
{
    private readonly ConcurrentDictionary<int, ModerationTicketDto> _tickets = new();
    private readonly SemaphoreSlim _pickLock = new(1, 1);

    public async Task LoadAsync()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var open = await dbContext.ModerationTickets
            .AsNoTracking()
            .Where(x => x.State == ModerationTicketState.Open || x.State == ModerationTicketState.Picked)
            .Select(x => new ModerationTicketDto
            {
                Id = x.Id,
                ReporterPlayerId = x.ReporterPlayerId,
                ReporterUsername = x.ReporterPlayer!.Username,
                ReportedPlayerId = x.ReportedPlayerId,
                ReportedUsername = x.ReportedPlayer!.Username,
                RoomId = x.RoomId,
                CategoryId = x.CategoryId,
                Message = x.Message,
                State = x.State,
                Resolution = x.Resolution,
                PickedByPlayerId = x.PickedByPlayerId,
                PickedByUsername = x.PickedByPlayer!.Username,
                CreatedAt = x.CreatedAt,
                PickedAt = x.PickedAt,
                ClosedAt = x.ClosedAt
            })
            .ToListAsync();

        foreach (var ticket in open)
        {
            _tickets[ticket.Id] = ticket;
        }
    }

    public IReadOnlyList<ModerationTicketDto> GetActiveTickets() => _tickets.Values
        .OrderBy(x => x.CreatedAt)
        .ToList();

    public ModerationTicketDto? GetById(int ticketId) =>
        _tickets.TryGetValue(ticketId, out var ticket) ? ticket : null;

    public async Task<ModerationTicketDto?> CreateAsync(
        long reporterId,
        long? reportedId,
        int? roomId,
        int categoryId,
        string message)
    {
        if (_tickets.Values.Any(x =>
                x.ReporterPlayerId == reporterId && x.State != ModerationTicketState.Closed))
        {
            return null;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var entity = new ModerationTicket
        {
            ReporterPlayerId = reporterId,
            ReportedPlayerId = reportedId,
            RoomId = roomId,
            CategoryId = categoryId,
            Message = message,
            State = ModerationTicketState.Open,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.ModerationTickets.Add(entity);
        await dbContext.SaveChangesAsync();

        var reporter = await dbContext.Players
            .AsNoTracking()
            .Where(x => x.Id == reporterId)
            .Select(x => x.Username)
            .FirstOrDefaultAsync() ?? string.Empty;

        var reported = reportedId == null
            ? string.Empty
            : await dbContext.Players
                .AsNoTracking()
                .Where(x => x.Id == reportedId)
                .Select(x => x.Username)
                .FirstOrDefaultAsync() ?? string.Empty;

        var dto = new ModerationTicketDto
        {
            Id = entity.Id,
            ReporterPlayerId = reporterId,
            ReporterUsername = reporter,
            ReportedPlayerId = reportedId,
            ReportedUsername = reported,
            RoomId = roomId,
            CategoryId = categoryId,
            Message = message,
            State = ModerationTicketState.Open,
            Resolution = ModerationTicketResolution.None,
            CreatedAt = entity.CreatedAt
        };

        _tickets[dto.Id] = dto;

        return dto;
    }

    public async Task<ModerationTicketDto?> TryPickAsync(int ticketId, long moderatorId, string moderatorUsername)
    {
        await _pickLock.WaitAsync();

        try
        {
            if (!_tickets.TryGetValue(ticketId, out var ticket) || ticket.State != ModerationTicketState.Open)
            {
                return null;
            }

            var pickedAt = DateTimeOffset.UtcNow;

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var updated = await dbContext.ModerationTickets
                .Where(x => x.Id == ticketId && x.State == ModerationTicketState.Open)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.State, ModerationTicketState.Picked)
                    .SetProperty(x => x.PickedByPlayerId, moderatorId)
                    .SetProperty(x => x.PickedAt, pickedAt));

            if (updated == 0)
            {
                return null;
            }

            ticket.State = ModerationTicketState.Picked;
            ticket.PickedByPlayerId = moderatorId;
            ticket.PickedByUsername = moderatorUsername;
            ticket.PickedAt = pickedAt;

            return ticket;
        }
        finally
        {
            _pickLock.Release();
        }
    }

    public async Task<ModerationTicketDto?> TryReleaseAsync(int ticketId, long moderatorId)
    {
        if (!_tickets.TryGetValue(ticketId, out var ticket) ||
            ticket.State != ModerationTicketState.Picked ||
            ticket.PickedByPlayerId != moderatorId)
        {
            return null;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.ModerationTickets
            .Where(x => x.Id == ticketId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.State, ModerationTicketState.Open)
                .SetProperty(x => x.PickedByPlayerId, (long?) null)
                .SetProperty(x => x.PickedAt, (DateTimeOffset?) null));

        ticket.State = ModerationTicketState.Open;
        ticket.PickedByPlayerId = null;
        ticket.PickedByUsername = string.Empty;
        ticket.PickedAt = null;

        return ticket;
    }

    public async Task<ModerationTicketDto?> TryCloseAsync(
        int ticketId,
        long moderatorId,
        ModerationTicketResolution resolution)
    {
        if (!_tickets.TryGetValue(ticketId, out var ticket) || ticket.State == ModerationTicketState.Closed)
        {
            return null;
        }

        var closedAt = DateTimeOffset.UtcNow;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.ModerationTickets
            .Where(x => x.Id == ticketId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.State, ModerationTicketState.Closed)
                .SetProperty(x => x.Resolution, resolution)
                .SetProperty(x => x.PickedByPlayerId, moderatorId)
                .SetProperty(x => x.ClosedAt, closedAt));

        ticket.State = ModerationTicketState.Closed;
        ticket.Resolution = resolution;
        ticket.PickedByPlayerId = moderatorId;
        ticket.ClosedAt = closedAt;

        _tickets.TryRemove(ticketId, out _);

        return ticket;
    }

    private IReadOnlyList<ModerationCfhTopicDto>? _topics;

    public async Task<IReadOnlyList<ModerationCfhTopicDto>> GetTopicsAsync()
    {
        if (_topics != null)
        {
            return _topics;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        _topics = await dbContext.ModerationCfhTopics
            .AsNoTracking()
            .OrderBy(x => x.Order)
            .Select(x => new ModerationCfhTopicDto
            {
                Id = x.Id,
                CategoryName = x.CategoryName,
                Name = x.Name,
                Order = x.Order
            })
            .ToListAsync();

        return _topics;
    }
}
