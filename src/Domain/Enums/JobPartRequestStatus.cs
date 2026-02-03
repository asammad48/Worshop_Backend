namespace Domain.Enums;

public enum JobPartRequestStatus : short
{
    Requested = 0,
    Ordered = 1,
    Arrived = 2,
    IssuedToJob = 3,
    Cancelled = 4
}
