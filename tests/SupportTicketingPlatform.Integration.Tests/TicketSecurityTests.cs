using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using System.Threading.Tasks;

namespace SupportTicketingPlatform.Integration.Tests
{
    public class TicketSecurityTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public TicketSecurityTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Post_Tickets_WithoutToken_ShouldReturnUnauthorized()
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/tickets", new 
            { 
                Title = "Integration Test", 
                Description = "Testing Auth", 
                Priority = 1, 
                CategoryId = 1 
            });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        
        [Fact]
        public async Task Get_AdminEndpoint_WithoutToken_ShouldReturnUnauthorized()
        {
            var response = await _client.GetAsync("/api/admin/tickets/unassigned");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
