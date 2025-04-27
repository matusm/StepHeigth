namespace StepHeight
{
    public enum ScanFieldTopology
    {
        Unknown,
        Single,
        Two,
        Three
    }

    public enum FeatureType
    {
        None,
        A1Groove,               // ISO 5436-1 Type A1 (rectangular groove)
        A2Groove,               // ISO 5436-1 Type A2 (cylindrical groove)
        A1Ridge,                // ISO 5436-1 Type A1 (rectangular step)
        A2Ridge,                // ISO 5436-1 Type A2 (cylindrical ridge), uncommon
        A1TrapezoidalGroove,    // ISO 5436-1 Type A1 (trapezoidal groove)
        A1TrapezoidalRidge,     // ISO 5436-1 Type A1 (trapezoidal ridge)
        RisingEdge,             // single edge low->high
        FallingEdge             // single edge high->low
    }

    public enum FitStatus
    {
        Unknown,
        Success,
        BadEdgePosition,
        NoData
    }
}
