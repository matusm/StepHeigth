namespace StepHeight
{
    public class FitBoundaries
    {
        public FitBoundaries(double relLengthE, double relLengthA, double relLengthC)
        {
            DomainLengthE = relLengthE;
            DomainLengthA = relLengthA;
            DomainLengthC = relLengthC;
        }

        private double DomainLengthA { get; }   // normalized reference evaluation length (2/3)
        private double DomainLengthC { get; }   // normalized feature evaluation length (1/3)
        private double DomainLengthE { get; }   // normalized overall evaluation length (3)

        public double FeatureWidth { get; private set; } = double.NaN; // absolute feature width (in m)
        public double WallWidth { get; private set; } = double.NaN; // absolute wall width (in m)
        // the six boundary points:
        public double X1 { get; private set; } = double.NaN; // start of the left reference domain
        public double X2 { get; private set; } = double.NaN; // end of the left reference domain
        public double X3 { get; private set; } = double.NaN; // start of feature domain
        public double X4 { get; private set; } = double.NaN; // end of the feature domain
        public double X5 { get; private set; } = double.NaN; // start of the right reference domain
        public double X6 { get; private set; } = double.NaN; // end of the right reference domain

        public void GenerateBoundaries(double leftEdgePosition, double rightEdgePosition) => GenerateBoundaries(leftEdgePosition, rightEdgePosition, leftEdgePosition, rightEdgePosition);  

        public void GenerateBoundaries(double leftEdgePosition, double rightEdgePosition, double leftWallPosition, double rightWallPosition)
        {
            (double El, double Er, double Wl, double Wr) t = SortPositions(leftEdgePosition, rightEdgePosition, leftWallPosition, rightWallPosition);
            FeatureWidth = t.Er - t.El; // sets the unit for the relative domain lengths.
            WallWidth = t.Wr - t.Wl;
            double lengthE = DomainLengthE * FeatureWidth;
            double lengthA = DomainLengthA * FeatureWidth;
            double lengthC = DomainLengthC * FeatureWidth;
            double lengthEAll = lengthE + WallWidth - FeatureWidth;
            double lengthRef = (lengthE - FeatureWidth) / 2.0;

            X1 = t.Wl - lengthRef;
            X6 = t.Wr + lengthRef;
            X2 = X1 + lengthA;
            X5 = X6 - lengthA;
            X3 = t.El + (FeatureWidth - lengthC) / 2.0;
            X4 = X3 + lengthC;
        }

        private (double, double, double, double) SortPositions(double e1, double e2, double w1, double w2)
        { 
            var t = (e1, e2, w1, w2);
            // sort the feature edge positions
            if (e1 > e2)
            {
                t = (e2, e1, w1, w2);
            }
            // sort the wall edge positions
            if (w1 > w2)
            {
                t = (t.Item1, t.Item2, w2, w1);
            }
            // confine the wall edge positions to the feature edge positions
            if (t.Item3 > t.Item1)
            {
                t = (t.Item1, t.Item2, t.Item1, t.Item4);
            }
            if (t.Item4 < t.Item2)
            {
                t = (t.Item1, t.Item2, t.Item3, t.Item2);
            }
            return t;
        }
    
    }
}
