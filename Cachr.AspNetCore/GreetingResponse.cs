namespace Cachr.AspNetCore;

public record GreetingResponse(Guid Id, string Name, string? DetectedAddress, bool IsSecure);