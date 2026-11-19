namespace Litoral.Domain.Enums;

public enum MelKind
{
    Mel,
    Cdl
}

public enum MelCategory
{
    A,
    B,
    C,
    D
}

public enum MelVerification
{
    Acars,
    Safety,
    Info
}

public enum MelAcarsCheck
{
    None,
    MaxAltitude,
    RemarksContain,
    ApuNotUsed,
    AutobrakeOff,
    SpeedbrakeNotArmed,
    AutothrottleOff,
    CenterTankEmpty,
    TaxiLightOff,
    LogoLightOff
}

public enum MelSource
{
    System,
    Staff
}

public enum MelCheckResult
{
    Pending,
    Complied,
    Violated
}