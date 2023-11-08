namespace Tsw.EventBus.Common.Abstractions;

public sealed record class PublishContent(Guid EventId, string JsonContent, string IntegrationEventTypeName);
