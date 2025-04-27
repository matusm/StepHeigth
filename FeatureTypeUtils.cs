namespace StepHeight
{
    internal static class FeatureTypeUtils
    {
        internal static string TypeToString(FeatureType featureType)
        {
            switch (featureType)
            {
                case FeatureType.A1Groove:
                    return "ISO 5436-1 Type A1 (rectangular groove)"; //+
                case FeatureType.A1Ridge:
                    return "Inverted ISO 5436-1 Type A1 (rectangular ridge)"; //-
                case FeatureType.A2Groove:
                    return "ISO 5436-1 Type A2 (cylindrical groove)"; //+
                case FeatureType.A2Ridge:
                    return "Inverted ISO 5436-1 Type A2 (cylindrical ridge)"; //-
                case FeatureType.A1TrapezoidalGroove:
                    return "ISO 5436-1 Type A1 (trapezoidal groove)"; //+
                case FeatureType.A1TrapezoidalRidge:
                    return "Inverted ISO 5436-1 Type A1 (trapezoidal ridge)"; //-
                case FeatureType.RisingEdge:
                    return "Single edge (low->high)"; //-
                case FeatureType.FallingEdge:
                    return "Single edge (high->low)"; //+
                case FeatureType.None:
                    return "undefined feature";
                default:
                    return "undefined feature";
            }
        }
    }
}
