using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportTicketingPlatform.Application.Commands.Tickets.SetTicketPriority;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Domain.Enums;
using SupportTicketingPlatform.Application.Tests.Helpers;
using Xunit;

namespace SupportTicketingPlatform.Application.Tests.Commands
{
    public class SetTicketPriorityCommandTests
    {
        [Fact]
        public async Task Handle_ShouldChangePriority_WhenValid()
        {
            var dbContext = TestDbContextFactory.Create();
            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(u => u.UserId).Returns("admin1");

            var ticket = new Ticket { Title = "Test", Description = "Test", CustomerId = "cust1", CategoryId = 1, Priority = TicketPriority.Low, Status = TicketStatus.New };
            dbContext.Tickets.Add(ticket);
            await dbContext.SaveChangesAsync();

            var handler = new SetTicketPriorityCommandHandler(dbContext, mockUserService.Object);
            var command = new SetTicketPriorityCommand(ticket.Id, TicketPriority.Critical);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var updatedTicket = await dbContext.Tickets.FirstAsync();
            updatedTicket.Priority.Should().Be(TicketPriority.Critical);
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenTicketIsClosed()
        {
            var dbContext = TestDbContextFactory.Create();
            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(u => u.UserId).Returns("admin1");

            var ticket = new Ticket { Title = "Test", Description = "Test", CustomerId = "cust1", CategoryId = 1, Priority = TicketPriority.Low, Status = TicketStatus.Closed };
            dbContext.Tickets.Add(ticket);
            await dbContext.SaveChangesAsync();

            var handler = new SetTicketPriorityCommandHandler(dbContext, mockUserService.Object);
            var command = new SetTicketPriorityCommand(ticket.Id, TicketPriority.High);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Type.Should().Be(ErrorType.Conflict);
        }
    }
}
