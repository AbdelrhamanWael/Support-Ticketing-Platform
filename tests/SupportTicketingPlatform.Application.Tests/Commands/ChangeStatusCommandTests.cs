using FluentAssertions;
using SupportTicketingPlatform.Application.Tests.Helpers;
using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Tests.Commands;

public class ChangeStatusCommandTests
{
    [Fact]
    public async Task Handle_WhenNewToClosed_ThrowsInvalidOperation()
    {
        await using var context = TestDbContextFactory.Create();

        context.Tickets.Add(new Ticket
        {
            Id = 1,
            Title = "Test",
            Description = "Test ticket",
            CustomerId = "customer-1",
            CategoryId = 1,
            Status = TicketStatus.New,
            Priority = TicketPriority.Medium,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var handler = new ChangeTicketStatusCommandHandler(context);
        var command = new ChangeTicketStatusCommand
        {
            TicketId = 1,
            NewStatus = TicketStatus.Closed,
            UserId = "agent-1",
            Reason = "Invalid transition"
        };

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TICKET-R09*");
    }

    [Fact]
    public async Task Handle_WhenNewToAssigned_UpdatesStatus()
    {
        await using var context = TestDbContextFactory.Create();

        context.Tickets.Add(new Ticket
        {
            Id = 1,
            Title = "Test",
            Description = "Test ticket",
            CustomerId = "customer-1",
            CategoryId = 1,
            Status = TicketStatus.New,
            Priority = TicketPriority.Medium,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var handler = new ChangeTicketStatusCommandHandler(context);
        var command = new ChangeTicketStatusCommand
        {
            TicketId = 1,
            NewStatus = TicketStatus.Assigned,
            UserId = "lead-1",
            Reason = "Assigning agent"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        context.Tickets.Single().Status.Should().Be(TicketStatus.Assigned);
        context.TicketStatusHistories.Should().ContainSingle();
    }
}
