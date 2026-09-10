namespace RetailOS.Application.Products.DTOs;

public record ImportRowError(
    int Row,
    string? Field,
    string Message
);

public record ImportPreviewResponse(
    int TotalRows,
    int ValidRows,
    int ErrorRows,
    IReadOnlyList<ImportRowError> Errors
);

public record ImportCommitResponse(
    int TotalProcessed,
    int Created,
    int SkippedDuplicate,
    int SkippedInvalid
);
