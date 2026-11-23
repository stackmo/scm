namespace SCM.Domain.Shared.Enums;

public enum TubeType
{
    EDTA = 1,
    Serum = 2,
    Heparin = 3,
    NaF = 4,
    Urine = 5
}

public enum TubeStatus
{
    Prepared = 0,
    Collected = 1,
    Transporting = 2,
    Received = 3,
    Rejected = 4
}

public enum BatchStatus
{
    Draft = 0,
    Scheduled = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum ManifestStatus
{
    InTransit = 0,
    Received = 1,
    TemperatureBreach = 2
}
