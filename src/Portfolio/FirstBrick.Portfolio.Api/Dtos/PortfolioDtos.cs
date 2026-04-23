namespace FirstBrick.Portfolio.Api.Dtos;

public record PortfolioEntryDto(Guid ProjectId, string ProjectTitle, decimal TotalInvested, DateTime LastUpdatedUtc);
