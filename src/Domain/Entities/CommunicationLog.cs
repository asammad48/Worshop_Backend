using Domain.Enums;

namespace Domain.Entities;

public sealed class CommunicationLog : BaseEntity
{
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }

    public CommunicationChannel Channel { get; set; }
    public CommunicationMessageType MessageType { get; set; }

    public DateTimeOffset SentAt { get; set; }
    public string? Notes { get; set; }

    public Guid SentByUserId { get; set; }
    public User? SentByUser { get; set; }
}
