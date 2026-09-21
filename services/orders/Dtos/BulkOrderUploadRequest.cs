namespace Orders.Api.Dtos;

public class BulkOrderUploadRequest
{
    public IFormFile? File { get; set; }

    public int BatchSize { get; set; } = 100;
}
