using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Helpdesk.Application.Audit;
using Helpdesk.Application.Assets;
using Helpdesk.Application.Auth;
using Helpdesk.Application.Dashboard;
using Helpdesk.Application.Tickets;
using Helpdesk.Application.Users;
using Helpdesk.Domain.Assets;
using Helpdesk.Domain.Tickets;
using Helpdesk.Domain.Users;

namespace Helpdesk.IntegrationTests;

public sealed class HelpdeskApiTests(HelpdeskApiFactory factory) : IClassFixture<HelpdeskApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ProtectedEndpointReturnsUnauthorizedWithoutToken()
    {
        var response = await _client.GetAsync("/api/tickets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginReturnsJwtAndCurrentUser()
    {
        var auth = await LoginAsync("admin@test.com");

        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
        Assert.Equal("admin@test.com", auth.User.Email);
        Assert.Equal("Admin", auth.User.Role.ToString());
    }

    [Fact]
    public async Task PostTicketsCreatesTicketInDatabase()
    {
        var auth = await LoginAsync("employee@test.com");
        var category = await FirstCategoryAsync(auth.Token);
        var title = $"Cannot access payroll app {Guid.NewGuid():N}";

        var created = await SendAsync<TicketDetailsResponse>(
            HttpMethod.Post,
            "/api/tickets",
            auth.Token,
            new CreateTicketRequest(title, "The payroll portal returns 403 after login.", category.Id, TicketPriority.Medium, null));

        var tickets = await GetAsync<List<TicketListItemResponse>>("/api/tickets", auth.Token);

        Assert.Equal(title, created.Title);
        Assert.Contains(tickets, ticket => ticket.Id == created.Id);
    }

    [Fact]
    public async Task TechnicianCannotCreateTicket()
    {
        var auth = await LoginAsync("technician@test.com");
        var category = await FirstCategoryAsync(auth.Token);
        using var request = AuthorizedRequest(
            HttpMethod.Post,
            "/api/tickets",
            auth.Token,
            new CreateTicketRequest(
                "Technician should not create tickets",
                "Technicians work the support queue instead of opening employee requests.",
                category.Id,
                TicketPriority.Low,
                null));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EmployeeGetsOnlyOwnTickets()
    {
        var auth = await LoginAsync("employee@test.com");

        var tickets = await GetAsync<List<TicketListItemResponse>>("/api/tickets", auth.Token);

        Assert.NotEmpty(tickets);
        Assert.All(tickets, ticket => Assert.Equal("Ewa Nowak", ticket.CreatedBy));
    }

    [Fact]
    public async Task TechnicianGetsQueueWithUnassignedTickets()
    {
        var auth = await LoginAsync("technician@test.com");

        var tickets = await GetAsync<List<TicketListItemResponse>>("/api/tickets", auth.Token);

        Assert.Contains(tickets, ticket => ticket.AssignedTo is null);
    }

    [Fact]
    public async Task EmployeeCannotAssignTicket()
    {
        var auth = await LoginAsync("employee@test.com");
        var ticket = (await GetAsync<List<TicketListItemResponse>>("/api/tickets", auth.Token)).First();
        using var request = AuthorizedRequest(HttpMethod.Patch, $"/api/tickets/{ticket.Id}/assign", auth.Token, new AssignTicketRequest(null));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TechnicianCanAssignTicketToHimself()
    {
        var employee = await LoginAsync("employee@test.com");
        var technician = await LoginAsync("technician@test.com");
        var ticket = await CreateTicketAsync(employee.Token);

        var assigned = await SendAsync<TicketDetailsResponse>(
            HttpMethod.Patch,
            $"/api/tickets/{ticket.Id}/assign",
            technician.Token,
            new AssignTicketRequest(null));

        Assert.Equal("Tomasz Zielinski", assigned.AssignedTo?.FullName);
        Assert.Equal(TicketStatus.Assigned, assigned.Status);
    }

    [Fact]
    public async Task TicketHistoryIsCreatedAfterStatusChange()
    {
        var employee = await LoginAsync("employee@test.com");
        var technician = await LoginAsync("technician@test.com");
        var ticket = await CreateTicketAsync(employee.Token);
        await SendAsync<TicketDetailsResponse>(HttpMethod.Patch, $"/api/tickets/{ticket.Id}/assign", technician.Token, new AssignTicketRequest(null));

        await SendAsync<TicketDetailsResponse>(
            HttpMethod.Patch,
            $"/api/tickets/{ticket.Id}/status",
            technician.Token,
            new ChangeTicketStatusRequest(TicketStatus.InProgress));
        var history = await GetAsync<List<TicketHistoryResponse>>($"/api/tickets/{ticket.Id}/history", technician.Token);

        Assert.Contains(history, item => item.Action == "Status changed" && item.NewValue == TicketStatus.InProgress.ToString());
    }

    [Fact]
    public async Task InternalCommentsAreHiddenFromEmployee()
    {
        var auth = await LoginAsync("employee@test.com");
        var tickets = await GetAsync<List<TicketListItemResponse>>("/api/tickets", auth.Token);
        var laptopTicket = tickets.Single(ticket => ticket.Title.Contains("Laptop overheats", StringComparison.OrdinalIgnoreCase));

        var details = await GetAsync<TicketDetailsResponse>($"/api/tickets/{laptopTicket.Id}", auth.Token);

        Assert.All(details.Comments, comment => Assert.False(comment.IsInternal));
        Assert.DoesNotContain(details.Comments, comment => comment.Body.Contains("Thermal diagnostics", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CommentIsSavedAndReturnedInTicketDetails()
    {
        var auth = await LoginAsync("employee@test.com");
        var ticket = await CreateTicketAsync(auth.Token);
        var body = $"Public comment {Guid.NewGuid():N}";

        var updated = await SendAsync<TicketDetailsResponse>(
            HttpMethod.Post,
            $"/api/tickets/{ticket.Id}/comments",
            auth.Token,
            new AddTicketCommentRequest(body, false));

        Assert.Contains(updated.Comments, comment => comment.Body == body && !comment.IsInternal);
    }

    [Fact]
    public async Task EmployeeCannotAddInternalComment()
    {
        var auth = await LoginAsync("employee@test.com");
        var ticket = await CreateTicketAsync(auth.Token);
        using var request = AuthorizedRequest(
            HttpMethod.Post,
            $"/api/tickets/{ticket.Id}/comments",
            auth.Token,
            new AddTicketCommentRequest("Internal escalation note.", true));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EmployeeCannotCommentClosedTicket()
    {
        var employee = await LoginAsync("employee@test.com");
        var technician = await LoginAsync("technician@test.com");
        var ticket = await CreateTicketAsync(employee.Token);
        await SendAsync<TicketDetailsResponse>(HttpMethod.Patch, $"/api/tickets/{ticket.Id}/assign", technician.Token, new AssignTicketRequest(null));
        await SendAsync<TicketDetailsResponse>(HttpMethod.Patch, $"/api/tickets/{ticket.Id}/status", technician.Token, new ChangeTicketStatusRequest(TicketStatus.InProgress));
        await SendAsync<TicketDetailsResponse>(HttpMethod.Patch, $"/api/tickets/{ticket.Id}/status", technician.Token, new ChangeTicketStatusRequest(TicketStatus.Resolved));
        await SendAsync<TicketDetailsResponse>(HttpMethod.Patch, $"/api/tickets/{ticket.Id}/status", employee.Token, new ChangeTicketStatusRequest(TicketStatus.Closed));
        using var request = AuthorizedRequest(
            HttpMethod.Post,
            $"/api/tickets/{ticket.Id}/comments",
            employee.Token,
            new AddTicketCommentRequest("One more note after closure.", false));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminCanCreateAssetAndAssetTagMustBeUnique()
    {
        var auth = await LoginAsync("admin@test.com");
        var assetTag = $"LTP-{Guid.NewGuid():N}"[..14].ToUpperInvariant();
        var request = new CreateAssetRequest(
            assetTag,
            AssetType.Laptop,
            "Framework",
            "Laptop 13",
            $"SN-{Guid.NewGuid():N}",
            DateOnly.FromDateTime(DateTime.UtcNow.Date),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(3).Date),
            "Integration test device");

        var created = await SendAsync<AssetResponse>(HttpMethod.Post, "/api/assets", auth.Token, request);
        using var duplicate = AuthorizedRequest(HttpMethod.Post, "/api/assets", auth.Token, request);
        var duplicateResponse = await _client.SendAsync(duplicate);

        Assert.Equal(assetTag, created.AssetTag);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task AssetDetailsReturnAssignmentHistory()
    {
        var auth = await LoginAsync("admin@test.com");
        var employee = (await GetAsync<List<UserResponse>>("/api/users", auth.Token))
            .Single(user => user.Email == "employee@test.com");
        var request = new CreateAssetRequest(
            $"HST-{Guid.NewGuid():N}"[..14].ToUpperInvariant(),
            AssetType.Phone,
            "Apple",
            "iPhone 16",
            $"SN-{Guid.NewGuid():N}",
            DateOnly.FromDateTime(DateTime.UtcNow.Date),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2).Date),
            "Assignment history integration test device");

        var created = await SendAsync<AssetResponse>(HttpMethod.Post, "/api/assets", auth.Token, request);
        await SendAsync<AssetResponse>(HttpMethod.Patch, $"/api/assets/{created.Id}/assign", auth.Token, new AssignAssetRequest(employee.Id));
        var details = await GetAsync<AssetDetailsResponse>($"/api/assets/{created.Id}", auth.Token);

        Assert.Equal(employee.FullName, details.AssignedTo);
        Assert.Contains(details.AssignmentHistory, assignment =>
            assignment.User == employee.FullName
            && assignment.AssignedBy == auth.User.FullName
            && assignment.ReturnedAt is null);
    }

    [Fact]
    public async Task EmployeeCannotReadUnassignedAssetDetails()
    {
        var admin = await LoginAsync("admin@test.com");
        var employee = await LoginAsync("employee@test.com");
        var asset = await SendAsync<AssetResponse>(
            HttpMethod.Post,
            "/api/assets",
            admin.Token,
            new CreateAssetRequest(
                $"UNA-{Guid.NewGuid():N}"[..14].ToUpperInvariant(),
                AssetType.Router,
                "Ubiquiti",
                "Gateway",
                $"SN-{Guid.NewGuid():N}",
                DateOnly.FromDateTime(DateTime.UtcNow.Date),
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(3).Date),
                "Unassigned permission test device"));
        using var request = AuthorizedRequest(HttpMethod.Get, $"/api/assets/{asset.Id}", employee.Token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AssetOperationsAreWrittenToGlobalAuditLog()
    {
        var auth = await LoginAsync("admin@test.com");
        var employee = (await GetAsync<List<UserResponse>>("/api/users", auth.Token))
            .Single(user => user.Email == "employee@test.com");
        var request = new CreateAssetRequest(
            $"AUD-{Guid.NewGuid():N}"[..14].ToUpperInvariant(),
            AssetType.Laptop,
            "Lenovo",
            "ThinkPad T14",
            $"SN-{Guid.NewGuid():N}",
            DateOnly.FromDateTime(DateTime.UtcNow.Date),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(3).Date),
            "Audit integration test device");

        var created = await SendAsync<AssetResponse>(HttpMethod.Post, "/api/assets", auth.Token, request);
        await SendAsync<AssetResponse>(HttpMethod.Patch, $"/api/assets/{created.Id}/assign", auth.Token, new AssignAssetRequest(employee.Id));
        var repaired = await SendAsync<AssetResponse>(HttpMethod.Patch, $"/api/assets/{created.Id}/status", auth.Token, new ChangeAssetStatusRequest(AssetStatus.InRepair));
        var logs = await GetAsync<List<AuditLogResponse>>("/api/audit?entityType=Asset&take=250", auth.Token);

        Assert.Equal(AssetStatus.InRepair, repaired.Status);
        Assert.Contains(logs, log =>
            log.EntityId == created.Id
            && log.Action == "Asset created"
            && log.NewValue == created.AssetTag);
        Assert.Contains(logs, log =>
            log.EntityId == created.Id
            && log.Action == "Asset assigned"
            && log.NewValue == employee.FullName);
        Assert.Contains(logs, log =>
            log.EntityId == created.Id
            && log.Action == "Asset status changed"
            && log.OldValue == AssetStatus.Assigned.ToString()
            && log.NewValue == AssetStatus.InRepair.ToString());
    }

    [Fact]
    public async Task AdminCannotUpdateAssetToDuplicateSerialNumber()
    {
        var auth = await LoginAsync("admin@test.com");
        var firstRequest = new CreateAssetRequest(
            $"DUP-{Guid.NewGuid():N}"[..14].ToUpperInvariant(),
            AssetType.Monitor,
            "Dell",
            "U2724",
            $"SN-{Guid.NewGuid():N}",
            DateOnly.FromDateTime(DateTime.UtcNow.Date),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(3).Date),
            "First duplicate serial test device");
        var secondRequest = new CreateAssetRequest(
            $"DUP-{Guid.NewGuid():N}"[..14].ToUpperInvariant(),
            AssetType.Monitor,
            "Dell",
            "U2424",
            $"SN-{Guid.NewGuid():N}",
            DateOnly.FromDateTime(DateTime.UtcNow.Date),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(3).Date),
            "Second duplicate serial test device");

        var first = await SendAsync<AssetResponse>(HttpMethod.Post, "/api/assets", auth.Token, firstRequest);
        var second = await SendAsync<AssetResponse>(HttpMethod.Post, "/api/assets", auth.Token, secondRequest);
        using var update = AuthorizedRequest(
            HttpMethod.Put,
            $"/api/assets/{second.Id}",
            auth.Token,
            new UpdateAssetRequest(
                second.Type,
                second.Brand,
                second.Model,
                first.SerialNumber,
                second.PurchaseDate,
                second.WarrantyUntil,
                second.Notes));

        var response = await _client.SendAsync(update);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AdminCanCreateUserAndChangeRole()
    {
        var auth = await LoginAsync("admin@test.com");
        var email = $"user-{Guid.NewGuid():N}@test.com";

        var created = await SendAsync<UserResponse>(
            HttpMethod.Post,
            "/api/users",
            auth.Token,
            new CreateUserRequest("Integration User", email, "Password123!", UserRole.Employee, "Operations"));
        var updated = await SendAsync<UserResponse>(
            HttpMethod.Patch,
            $"/api/users/{created.Id}/role",
            auth.Token,
            new UpdateUserRoleRequest(UserRole.Technician));

        Assert.Equal(email, created.Email);
        Assert.Equal(UserRole.Technician, updated.Role);
    }

    [Fact]
    public async Task AdminCanCreateAndDeactivateCategory()
    {
        var auth = await LoginAsync("admin@test.com");
        var name = $"Security {Guid.NewGuid():N}"[..18];

        var created = await SendAsync<TicketCategoryResponse>(
            HttpMethod.Post,
            "/api/tickets/categories",
            auth.Token,
            new CreateTicketCategoryRequest(name, "Security access and device hardening issues."));
        var updated = await SendAsync<TicketCategoryResponse>(
            HttpMethod.Put,
            $"/api/tickets/categories/{created.Id}",
            auth.Token,
            new UpdateTicketCategoryRequest(created.Name, created.Description, false));
        var activeCategories = await GetAsync<List<TicketCategoryResponse>>("/api/tickets/categories", auth.Token);

        Assert.True(created.IsActive);
        Assert.False(updated.IsActive);
        Assert.DoesNotContain(activeCategories, category => category.Id == created.Id);
    }

    [Fact]
    public async Task DashboardReturnsOpenTicketCount()
    {
        var auth = await LoginAsync("admin@test.com");

        var dashboard = await GetAsync<DashboardSummaryResponse>("/api/dashboard/summary", auth.Token);
        var tickets = await GetAsync<List<TicketListItemResponse>>("/api/tickets", auth.Token);
        var categories = await GetAsync<List<CountByCategoryResponse>>("/api/dashboard/tickets-by-category", auth.Token);
        var assetsByStatus = await GetAsync<List<CountByAssetStatusResponse>>("/api/dashboard/assets-by-status", auth.Token);
        var openStatuses = new[]
        {
            TicketStatus.New,
            TicketStatus.Assigned,
            TicketStatus.InProgress,
            TicketStatus.WaitingForUser
        };

        Assert.Equal(tickets.Count(ticket => openStatuses.Contains(ticket.Status)), dashboard.OpenTickets);
        Assert.NotEmpty(categories);
        Assert.NotEmpty(assetsByStatus);
    }

    [Fact]
    public async Task AdminCanReadGlobalAuditLog()
    {
        var auth = await LoginAsync("admin@test.com");

        var logs = await GetAsync<List<AuditLogResponse>>("/api/audit?entityType=Ticket&take=20", auth.Token);

        Assert.NotEmpty(logs);
        Assert.All(logs, log => Assert.Equal("Ticket", log.EntityType));
        Assert.Contains(logs, log => log.Action == "Ticket created");
    }

    [Fact]
    public async Task EmployeeCannotReadGlobalAuditLog()
    {
        var auth = await LoginAsync("employee@test.com");
        using var request = AuthorizedRequest(HttpMethod.Get, "/api/audit", auth.Token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<TicketDetailsResponse> CreateTicketAsync(string token)
    {
        var category = await FirstCategoryAsync(token);
        return await SendAsync<TicketDetailsResponse>(
            HttpMethod.Post,
            "/api/tickets",
            token,
            new CreateTicketRequest(
                $"Integration ticket {Guid.NewGuid():N}",
                "Created by an integration test.",
                category.Id,
                TicketPriority.High,
                null));
    }

    private async Task<TicketCategoryResponse> FirstCategoryAsync(string token)
    {
        var categories = await GetAsync<List<TicketCategoryResponse>>("/api/tickets/categories", token);
        return categories.First();
    }

    private async Task<AuthenticatedUserResponse> LoginAsync(string email)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, "Password123!"),
            JsonOptions);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<AuthenticatedUserResponse>(JsonOptions))!;
    }

    private async Task<T> GetAsync<T>(string path, string token)
    {
        using var request = AuthorizedRequest(HttpMethod.Get, path, token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, string token, object body)
    {
        using var request = AuthorizedRequest(method, path, token, body);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }

    private static HttpRequestMessage AuthorizedRequest(HttpMethod method, string path, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        return request;
    }
}
