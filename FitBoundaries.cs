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
            FeatureWidth = rightEdgePosition - leftEdgePosition; // sets the unit for the relative domain lengths.
            double wallWidth = rightWallPosition - leftWallPosition;
            double lengthE = DomainLengthE * FeatureWidth;
            double lengthA = DomainLengthA * FeatureWidth;
            double lengthC = DomainLengthC * FeatureWidth;
            double lengthEAll = lengthE + wallWidth - FeatureWidth;
            double lengthRef = (lengthE - FeatureWidth) / 2.0;

            X1 = leftWallPosition - lengthRef;
            X6 = rightWallPosition + lengthRef;
            X2 = X1 + lengthA;
            X5 = X6 - lengthA;
            X3 = leftEdgePosition + (FeatureWidth - lengthC) / 2.0;
            X4 = X3 + lengthC;
        }

        private void OrderPositions()
        { }
    
    }
}
