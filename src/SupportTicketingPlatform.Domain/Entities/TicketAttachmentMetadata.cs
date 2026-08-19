namespace SupportTicketingPlatform.Domain.Entities;

public class TicketAttachmentMetadata
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public string UploadedByUserId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string StoragePath { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public string ContentType { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }
}
