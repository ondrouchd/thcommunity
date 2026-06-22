using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using THcommunity.Configuration;
using THcommunity.Models;
using THcommunity.Services;
using Xunit;

namespace THcommunity.Tests;

public class EventServicePricingTests
{
    private readonly Mock<ISupabaseClient> _db = new();
    private readonly TeamSettings _settings = new();

    private EventService CreateService()
    {
        return new EventService(
            _db.Object,
            Mock.Of<ITeamService>(),
            Mock.Of<IPushNotificationService>(),
            Options.Create(_settings),
            NullLogger<EventService>.Instance);
    }

    private void SetupUser(Guid userId, PlayerPosition position)
    {
        _db.Setup(x => x.GetSingleAsync<User>("users", It.Is<string>(q => q.Contains(userId.ToString()))))
            .ReturnsAsync(new User { Id = userId, Position = position });
    }

    private void SetupResponses(params EventResponse[] responses)
    {
        _db.Setup(x => x.GetListAsync<EventResponse>("event_responses", It.IsAny<string>()))
            .ReturnsAsync(responses.ToList());
    }

    private static EventResponse PlayerResponse(Guid userId, int minutesOffset) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Response = ResponseType.Player,
        RespondedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(minutesOffset)
    };

    [Fact]
    public async Task CalculatePrice_ReturnsZero_WhenUserNotFound()
    {
        _db.Setup(x => x.GetSingleAsync<User>("users", It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var price = await CreateService().CalculatePriceAsync(Guid.NewGuid(), Guid.NewGuid());

        price.Should().Be(0m);
    }

    [Fact]
    public async Task CalculatePrice_ReturnsGoaliePrice_ForGoalie()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId, PlayerPosition.Goalie);

        var price = await CreateService().CalculatePriceAsync(Guid.NewGuid(), userId);

        price.Should().Be(_settings.Pricing.GoaliePrice);
    }

    [Fact]
    public async Task CalculatePrice_ReturnsTier1_ForEarlyPlayers()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        SetupUser(userId, PlayerPosition.Player);
        // Target is the 1st player to respond -> within Tier1 (positions 1..Tier1Count)
        SetupResponses(
            PlayerResponse(userId, 0),
            PlayerResponse(Guid.NewGuid(), 1),
            PlayerResponse(Guid.NewGuid(), 2));

        var price = await CreateService().CalculatePriceAsync(eventId, userId);

        price.Should().Be(_settings.Pricing.Tier1Price);
    }

    [Fact]
    public async Task CalculatePrice_ReturnsTier2_ForMiddlePlayers()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        SetupUser(userId, PlayerPosition.Player);

        // Build Tier1Count earlier players, then the target at position Tier1Count+1 (Tier2 range).
        var responses = new List<EventResponse>();
        for (int i = 0; i < _settings.Pricing.Tier1Count; i++)
            responses.Add(PlayerResponse(Guid.NewGuid(), i));
        responses.Add(PlayerResponse(userId, _settings.Pricing.Tier1Count));
        SetupResponses(responses.ToArray());

        var price = await CreateService().CalculatePriceAsync(eventId, userId);

        price.Should().Be(_settings.Pricing.Tier2Price);
    }

    [Fact]
    public async Task CalculatePrice_ReturnsTier3_ForLatePlayers()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        SetupUser(userId, PlayerPosition.Player);

        // Place the target just beyond Tier2Count -> Tier3 range.
        var responses = new List<EventResponse>();
        for (int i = 0; i < _settings.Pricing.Tier2Count; i++)
            responses.Add(PlayerResponse(Guid.NewGuid(), i));
        responses.Add(PlayerResponse(userId, _settings.Pricing.Tier2Count));
        SetupResponses(responses.ToArray());

        var price = await CreateService().CalculatePriceAsync(eventId, userId);

        price.Should().Be(_settings.Pricing.Tier3Price);
    }

    [Fact]
    public async Task CalculatePrice_ReturnsTier3_WhenPlayerHasNoCountedResponse()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        SetupUser(userId, PlayerPosition.Player);
        // Only other users have Player responses; target user isn't in the list (position 0).
        SetupResponses(
            PlayerResponse(Guid.NewGuid(), 0),
            PlayerResponse(Guid.NewGuid(), 1));

        var price = await CreateService().CalculatePriceAsync(eventId, userId);

        price.Should().Be(_settings.Pricing.Tier3Price);
    }
}
