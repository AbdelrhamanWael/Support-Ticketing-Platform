using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportTicketingPlatform.Application.Commands.Tickets.ReopenTicket;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Domain.Enums;
using SupportTicketingPlatform.Application.Tests.Helpers;
using Xunit;

namespace SupportTicketingPlatform.Application.Tests.Commands
{
    public class ReopenTicketCommandTests
    {
        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenTicketIsClosed()
        {
            // Arrange (التجهيز)
            var dbContext = TestDbContextFactory.Create();
            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(u => u.UserId).Returns("admin1");

            var ticket = new Ticket { Title = "Test", Description = "Test", CustomerId = "cust1", CategoryId = 1, Status = TicketStatus.Closed };
            dbContext.Tickets.Add(ticket);
            await dbContext.SaveChangesAsync();

            var handler = new ReopenTicketCommandHandler(dbContext, mockUserService.Object);
            var command = new ReopenTicketCommand(ticket.Id, "Need more info");

            // Act (التنفيذ)
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert (التأكد من النتيجة)
            result.IsSuccess.Should().BeTrue();
            var updatedTicket = await dbContext.Tickets.FirstAsync();
            updatedTicket.Status.Should().Be(TicketStatus.Reopened);
        }

        [Fact]
        public async Task Handle_ShouldFail_WhenTicketIsNotClosed()
        {
            // Arrange
            var dbContext = TestDbContextFactory.Create();
            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(u => u.UserId).Returns("admin1");

            // تذكرة جديدة مش مقفولة
            var ticket = new Ticket { Title = "Test", Description = "Test", CustomerId = "cust1", CategoryId = 1, Status = TicketStatus.New };
            dbContext.Tickets.Add(ticket);
            await dbContext.SaveChangesAsync();

            var handler = new ReopenTicketCommandHandler(dbContext, mockUserService.Object);
            var command = new ReopenTicketCommand(ticket.Id, "Try reopen");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Type.Should().Be(ErrorType.Conflict);
        }
    }
}
