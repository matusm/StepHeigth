using System;
using System.Collections.Generic;
using System.Linq;
using Bev.SurfaceRasterData;

namespace StepHeight
{
    public class FitVerticalStandard
    {
        public FitVerticalStandard(FeatureType featureType, double lengthE, double lengthA, double lengthC)
        {
            ResetProperties();
            FeatureType = featureType;
            featureFitSign = GetFeatureFitSignForFeatureType(FeatureType);
            DomainLengthE = lengthE; // overall length region to be used (3 W)
            DomainLengthA = lengthA; // single profile length on reference part (2/3 W) 
            DomainLengthC = lengthC; // length of evaluated profile at feature (1/3 W)
            fitBoundaries = new FitBoundaries(DomainLengthE, DomainLengthA, DomainLengthC);   
        }

        // immutable properties set by the constructor
        public FitStatus Status { get; private set; }
        public FeatureType FeatureType { get; }// type of feature to be fitted
        public string FeatureTypeDesignation => FeatureTypeUtils.TypeToString(FeatureType);
        public double DomainLengthA { get; }   // normalized reference evaluation length (2/3)
        public double DomainLengthC { get; }   // normalized feature evaluation length (1/3)
        public double DomainLengthE { get; }   // normalized overall evaluation length (3)
        // computed properties after calling FitProfile()
        public double FeatureWidth => fitBoundaries.FeatureWidth;   // feature width, W
        public double WallWidth => fitBoundaries.WallWidth;         // width of both inclined walls, 
        public double Yposition { get; private set; }               // position of profile in transverse direction
        public double Height { get; private set; }                  // height of the feature as obtained by fit (the measurand)
        public double Pt { get; private set; }                      // Pt as obtained by fit
        public double A2Radius { get; private set; }                // geometry parameter for A2 standards only
        public double A2CenterPosition { get; private set; }        // geometry parameter for A2 standards only
        public double A2Asymmetry { get; private set; }             // geometry parameter for A2 standards only
        public int NumberOfFitPoints { get; private set; }          // number of points used for fit
        public double RangeOfResiduals { get; private set; }        // range of residuals
        public bool ProfileTooShort { get; private set; }           // profile does not cover whole fit region
        public Point3D[] Residuals { get; private set; }            // residuals (in z direction) over the defined domain {A B C}
        public Point3D[] PredictedFunction { get; private set; }    // the predicted function over the defined domain {A B C}

        public void FitProfile(Point3D[] profile, double leftEdgePosition, double rightEdgePosition) => FitProfile(profile, leftEdgePosition, rightEdgePosition, leftEdgePosition, rightEdgePosition);

        // this overload is used for trapezoidal features
        public void FitProfile(Point3D[] profile, double leftEdgePosition, double rightEdgePosition, double leftWallPosition, double rightWallPosition)
        {
            ResetProperties();
            Point3D[] filteredProfile = RemoveBadPoints(profile);
            if (filteredProfile.Length == 0)
            {
                Status= FitStatus.NoData;
                return;
            }
            Yposition = filteredProfile[0].Y;
            // centering the profile to the feature avoids numerical instability with large X values
            double featureCenter = (leftEdgePosition + rightEdgePosition) / 2.0;
            Point3D[] shiftedProfile = ShiftProfile(filteredProfile, featureCenter);
            switch (FeatureType)
            {
                case FeatureType.A1Groove:
                case FeatureType.A1Ridge:
                    FitA1(shiftedProfile, leftEdgePosition - featureCenter, rightEdgePosition - featureCenter);
                    break;
                case FeatureType.A2Groove:
                case FeatureType.A2Ridge:
                    FitA2(shiftedProfile, leftEdgePosition - featureCenter, rightEdgePosition - featureCenter);
                    break;
                case FeatureType.FallingEdge:
                case FeatureType.RisingEdge:
                    FitEdge(shiftedProfile, leftEdgePosition - featureCenter);
                    break;
                case FeatureType.A1TrapezoidalGroove:
                case FeatureType.A1TrapezoidalRidge:
                    FitA1Trap(shiftedProfile, leftEdgePosition - featureCenter, rightEdgePosition - featureCenter, leftWallPosition - featureCenter, rightWallPosition - featureCenter);
                    break;
            }
        }

        // fit algorithms

        private void FitA1(Point3D[] profile, double leftEdgePosition, double rightEdgePosition) => FitA1Trap(profile, leftEdgePosition, rightEdgePosition, leftEdgePosition, rightEdgePosition);

        private void FitA1Trap(Point3D[] profile, double leftEdgePosition, double rightEdgePosition, double leftWallPosition, double rightWallPosition)
        {
            fitBoundaries.GenerateBoundaries(leftEdgePosition, rightEdgePosition, leftWallPosition, rightWallPosition);
            ProfileTooShort = (fitBoundaries.X1 < profile.First().X || fitBoundaries.X6 > profile.Last().X);
            if(ProfileTooShort)
            {
                Status = FitStatus.BadEdgePosition;
                return;
            }
            #region A1 fit algorithm (lengthy!)
            // populate the delta array
            int[] deltaData = new int[profile.Length];
            for (int i = 0; i < deltaData.Length; i++)
            {
                deltaData[i] = 0;
                if ((profile[i].X >= fitBoundaries.X1) && (profile[i].X <= fitBoundaries.X2))
                    deltaData[i] = featureFitSign; // region A
                if ((profile[i].X >= fitBoundaries.X5) && (profile[i].X <= fitBoundaries.X6))
                    deltaData[i] = featureFitSign; // region B
                if ((profile[i].X >= fitBoundaries.X3) && (profile[i].X <= fitBoundaries.X4))
                    deltaData[i] = -featureFitSign; // region C
            }

            // build the summs
            double SigmaX = 0, SigmaY = 0, SigmaXX = 0, SigmaXY = 0, deltaSigmaX = 0, deltaSigmaY = 0;
            int Num = 0, deltaNum = 0;
            for (int i = 0; i < deltaData.Length; i++)
            {
                int delta = deltaData[i];
                SigmaX += Math.Abs(delta) * profile[i].X;
                SigmaY += Math.Abs(delta) * profile[i].Z;
                SigmaXX += Math.Abs(delta) * profile[i].X * profile[i].X;
                SigmaXY += Math.Abs(delta) * profile[i].X * profile[i].Z;
                deltaSigmaX += delta * profile[i].X;
                deltaSigmaY += delta * profile[i].Z;
                Num += Math.Abs(delta);
                deltaNum += delta;
            }

            // variables with prefix fit_ are specifically for the fit
            // coefficients
            double fit_N = Num * deltaSigmaX * deltaSigmaX - 2 * deltaNum * deltaSigmaX * SigmaX + Num * SigmaX * SigmaX + deltaNum * deltaNum * SigmaXX - Num * Num * SigmaXX;
            double fit_a = (Num * deltaSigmaX * deltaSigmaY - deltaNum * deltaSigmaY * SigmaX + deltaNum * deltaNum * SigmaXY - Num * Num * SigmaXY - deltaNum * deltaSigmaX * SigmaY + Num * SigmaX * SigmaY) / fit_N;
            double fit_b = (deltaNum * deltaSigmaY * SigmaXX - deltaSigmaX * deltaSigmaY * SigmaX - deltaNum * deltaSigmaX * SigmaXY + Num * SigmaX * SigmaXY + deltaSigmaX * deltaSigmaX * SigmaY - Num * SigmaXX * SigmaY) / fit_N;
            double fit_c = (deltaSigmaY * SigmaX * SigmaX - deltaSigmaX * SigmaX * SigmaY + Num * deltaSigmaX * SigmaXY - Num * deltaSigmaY * SigmaXX + deltaNum * SigmaY * SigmaXX - deltaNum * SigmaX * SigmaXY) / fit_N;
            double fit_d = 2 * fit_c;
            Height = fit_d;

            // generate the fitted function, the residual function and some parameters
            List<Point3D> predictedFunction = new List<Point3D>();
            List<Point3D> residuals = new List<Point3D>();
            List<double> residualsDelta = new List<double>(); // for Pt calculation only

            for (int i = 0; i < profile.Length; i++)
            {
                if (deltaData[i] != 0)
                {
                    double fittedX = profile[i].X;
                    double fittedY = profile[i].Y;
                    double fittedZ = profile[i].X * fit_a + fit_b + deltaData[i] * fit_c;
                    double residualZ = profile[i].Z - fittedZ;
                    double residualDeltaZ = residualZ * deltaData[i];
                    predictedFunction.Add(new Point3D(fittedX, fittedY, fittedZ));
                    residuals.Add(new Point3D(fittedX, fittedY, residualZ));
                    residualsDelta.Add(residualDeltaZ);
                }
            }

            Pt = fit_d + residualsDelta.Max();
            RangeOfResiduals = RangeZfor(residuals);
            residuals.Sort();
            Residuals = residuals.ToArray();
            NumberOfFitPoints = Residuals.Length;
            predictedFunction.Sort();
            PredictedFunction = predictedFunction.ToArray();
            #endregion
            Status = FitStatus.Success;
        }

        private void FitA2(Point3D[] profile, double leftEdgePosition, double rightEdgePosition)
        {
            fitBoundaries.GenerateBoundaries(leftEdgePosition, rightEdgePosition);
            ProfileTooShort = (fitBoundaries.X1 < profile.First().X || fitBoundaries.X6 > profile.Last().X);
            if (ProfileTooShort)
            {
                Status = FitStatus.BadEdgePosition;
                return;
            }
            #region A2 fit algorithm (lengthy!)
            // generate array for the reference part (A, B)
            List<Point3D> domainAB = new List<Point3D>();
            foreach (Point3D point in profile)
            {
                if (((fitBoundaries.X1 <= point.X) && (fitBoundaries.X2 >= point.X)) || ((fitBoundaries.X5 <= point.X) && (fitBoundaries.X6 >= point.X)))
                {
                    domainAB.Add(new Point3D(point));
                }
            }

            // generate array for the central part (C)
            List<Point3D> domainC = new List<Point3D>();
            foreach (Point3D point in profile)
            {
                if ((fitBoundaries.X3 <= point.X) && (fitBoundaries.X4 >= point.X))
                {
                    domainC.Add(new Point3D(point));
                }
            }

            // fit a linear line in regions A and B
            double SigmaX_AB = 0;
            double SigmaY_AB = 0;
            double SigmaXY_AB = 0;
            double SigmaXX_AB = 0;
            for (int i = 0; i < domainAB.Count; i++)
            {
                SigmaX_AB += domainAB[i].X;
                SigmaY_AB += domainAB[i].Z;
                SigmaXX_AB += domainAB[i].X * domainAB[i].X;
                SigmaXY_AB += domainAB[i].X * domainAB[i].Z;
            }
            int Num_AB = domainAB.Count;

            // coefficients for the linear reference section
            double fit_k = (Num_AB * SigmaXY_AB - SigmaX_AB * SigmaY_AB) / (Num_AB * SigmaXX_AB - SigmaX_AB * SigmaX_AB);
            double fit_d = (SigmaY_AB - fit_k * SigmaX_AB) / Num_AB;

            // level A, B region
            foreach (var point in domainAB)
                point.Z -= (fit_k * point.X + fit_d); // actually the residua

            // level the central part C 
            foreach (var point in domainC)
                point.Z -= (fit_k * point.X + fit_d);

            // fit circular region (C) with a parabola
            double SigmaX_C = 0;
            double SigmaY_C = 0;
            double SigmaXY_C = 0;
            double SigmaXXY_C = 0;
            double SigmaXX_C = 0;
            double SigmaXXX_C = 0;
            double SigmaXXXX_C = 0;
            for (int i = 0; i < domainC.Count; i++)
            {
                SigmaX_C += domainC[i].X;
                SigmaY_C += domainC[i].Z;
                SigmaXX_C += domainC[i].X * domainC[i].X;
                SigmaXXX_C += domainC[i].X * domainC[i].X * domainC[i].X;
                SigmaXXXX_C += domainC[i].X * domainC[i].X * domainC[i].X * domainC[i].X;
                SigmaXY_C += domainC[i].X * domainC[i].Z;
                SigmaXXY_C += domainC[i].X * domainC[i].X * domainC[i].Z;
            }
            int Num_C = domainC.Count;
            double fiN = Num_C * SigmaXX_C * SigmaXXXX_C - SigmaX_C * SigmaX_C * SigmaXXXX_C - Num_C * SigmaXXX_C * SigmaXXX_C + 2.0 * SigmaX_C * SigmaXX_C * SigmaXXX_C - SigmaXX_C * SigmaXX_C * SigmaXX_C;
            double fiA = SigmaX_C * SigmaY_C * SigmaXXX_C - SigmaY_C * SigmaXX_C * SigmaXX_C + SigmaX_C * SigmaXX_C * SigmaXY_C - Num_C * SigmaXXX_C * SigmaXY_C + Num_C * SigmaXX_C * SigmaXXY_C - SigmaX_C * SigmaX_C * SigmaXXY_C;
            double fiB = SigmaY_C * SigmaXX_C * SigmaXXX_C - SigmaX_C * SigmaY_C * SigmaXXXX_C + Num_C * SigmaXXXX_C * SigmaXY_C;
            fiB += SigmaX_C * SigmaXX_C * SigmaXXY_C - Num_C * SigmaXXX_C * SigmaXXY_C - SigmaXX_C * SigmaXX_C * SigmaXY_C;
            double fiC = SigmaY_C * SigmaXX_C * SigmaXXXX_C - SigmaY_C * SigmaXXX_C * SigmaXXX_C;
            fiC += SigmaXX_C * SigmaXXX_C * SigmaXY_C - SigmaX_C * SigmaXXXX_C * SigmaXY_C;
            fiC += SigmaX_C * SigmaXXX_C * SigmaXXY_C - SigmaXX_C * SigmaXX_C * SigmaXXY_C;

            // coefficients for the circular section
            double fit_a = fiA / fiN;
            double fit_b = fiB / fiN;
            double fit_c = fiC / fiN;

            // analytical way
            double A2CenterYvalue = fit_c - fit_b * fit_b / (4.0 * fit_a);  // value of extremum
            A2CenterPosition = -fit_b / (2.0 * fit_a);  // position of extremum
            A2Radius = 1.0 / Math.Abs(2.0 * fit_a);     // radius
            A2Asymmetry = (2.0 * A2CenterPosition - (rightEdgePosition + leftEdgePosition)) / FeatureWidth; // asymmetry value +/-1

            if (featureFitSign > 0)
            {
                Height = -A2CenterYvalue;
                Pt = MaxZfor(domainAB) - MinZfor(domainC);
            }
            else
            {
                Height = A2CenterYvalue;
                Pt = MaxZfor(domainC) - MinZfor(domainAB);
            }

            List<Point3D> predictedFunction = new List<Point3D>();
            List<Point3D> residuals = new List<Point3D>();

            foreach (var point in domainAB)
            {
                double x = point.X;
                double y = point.Y;
                double z = fit_k * x + fit_d;
                predictedFunction.Add(new Point3D(x, y, z));
                residuals.Add(new Point3D(x, y, point.Z)); // since AB is leveled, Z is the residual
            }

            foreach (var point in domainC)
            {
                double x = point.X;
                double y = point.Y;
                double z = fit_a * x * x + fit_b * x + fit_c ; 
                predictedFunction.Add(new Point3D(x, y, z + (fit_k * x + fit_d))); // de-level
                residuals.Add(new Point3D(x, y, point.Z - z));
            }

            RangeOfResiduals = RangeZfor(residuals);
            residuals.Sort();
            Residuals = residuals.ToArray();
            predictedFunction.Sort();
            PredictedFunction = predictedFunction.ToArray();
            NumberOfFitPoints = residuals.Count;
            #endregion
            Status = FitStatus.Success;
        }

        private void FitEdge(Point3D[] profile, double edgePosition)
        {
            // TODO: implement edge fit
            fitBoundaries.GenerateBoundaries(edgePosition, edgePosition);
            ProfileTooShort = (fitBoundaries.X1 < profile.First().X || fitBoundaries.X6 > profile.Last().X);
            if (ProfileTooShort)
            {
                Status = FitStatus.BadEdgePosition;
                return;
            }
            throw new NotImplementedException();
        }

        // helper functions

        private Point3D[] ShiftProfile(Point3D[] profile, double center)
        {
            List<Point3D> points = new List<Point3D>();
            foreach (var p in profile)
            {
                points.Add(new Point3D(p.X - center, p.Y, p.Z));
            }
            return points.ToArray();
        }

        private double MaxZfor(List<Point3D> profile)
        {
            double max = double.MinValue;
            foreach (var point in profile)
            {
                double z = point.Z;
                if (z > max) max = z;
            }
            return max;
        }

        private double MinZfor(List<Point3D> profile)
        {
            double min = double.MaxValue;
            foreach (var point in profile)
            {
                double z = point.Z;
                if (z < min) min = z;
            }
            return min;
        }

        private double RangeZfor(List<Point3D> profile) => MaxZfor(profile) - MinZfor(profile);

        private int GetFeatureFitSignForFeatureType(FeatureType featureType)
        {
            switch (featureType)
            {
                case FeatureType.A1Groove:
                    return +1; 
                case FeatureType.A1Ridge:
                    return -1;
                case FeatureType.A2Groove:
                    return +1;
                case FeatureType.A2Ridge:
                    return -1;
                case FeatureType.A1TrapezoidalGroove:
                    return +1;
                case FeatureType.A1TrapezoidalRidge:
                    return -1;
                case FeatureType.RisingEdge:
                    return -1;
                case FeatureType.FallingEdge:
                    return +1;
                case FeatureType.None:
                    return 0;
                default:
                    return 0;
            }
        }

        private Point3D[] RemoveBadPoints(Point3D[] pointProfile)
        {
            List<Point3D> filteredProfile = new List<Point3D>();
            foreach (var point in pointProfile)
            {
                bool isValid = !double.IsNaN(point.Z) && !double.IsNaN(point.X);
                if (isValid)
                    filteredProfile.Add(point);
            }
            return filteredProfile.ToArray();
        }

        private void ResetProperties()
        {
            NumberOfFitPoints = 0;
            ProfileTooShort = false;
            RangeOfResiduals = double.NaN;
            Yposition = double.NaN;
            Height = double.NaN;
            Pt = double.NaN;
            A2Asymmetry = double.NaN;
            A2Radius = double.NaN;
            A2CenterPosition = double.NaN;
            Residuals = null;
            PredictedFunction = null;
            Status = FitStatus.Unknown;
        }

        private readonly int featureFitSign; // necessary for groove/ridge handling
        private readonly FitBoundaries fitBoundaries;
    }


}
