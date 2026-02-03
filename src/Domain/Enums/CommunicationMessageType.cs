namespace Domain.Enums;

public enum CommunicationMessageType : short
{
    Diagnosis = 1,
    Estimate = 2,
    Update = 3,
    ReadyForPickup = 4,
    PaymentReminder = 5,
    Other = 6
}
