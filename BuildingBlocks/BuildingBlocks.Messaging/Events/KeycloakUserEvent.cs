using System.Text.Json.Serialization;

namespace BuildingBlocks.Messaging.Events;

public record KeycloakUserEvent
{
    public string Type { get; set; }
    public Guid UserId { get; set; }
    public Details Details { get; set; }
}

public record Details
{
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; }
    [JsonPropertyName("last_name")]
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
}