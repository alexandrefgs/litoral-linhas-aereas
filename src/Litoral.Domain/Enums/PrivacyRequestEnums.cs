namespace Litoral.Domain.Enums;

public enum PrivacyRequestType
{
    Access,
    Correction,
    Deletion,
    Portability,
    ConsentWithdrawal,
    Other
}

public enum PrivacyRequestStatus
{
    Open,
    Completed,
    Rejected
}