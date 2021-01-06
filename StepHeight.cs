using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Bev.IO.BcrReader;
using Bev.SurfaceRasterData;

namespace StepHeight
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            const string microMeter = "µm"; // or "um"
            ScanFieldTopology scanFieldTopology = ScanFieldTopology.Unknown;
            // not clear what happens in multithread environments
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            // parse command line arguments
            var options = new Options();
            if (!CommandLine.Parser.Default.ParseArgumentsStrict(args, options))
                ConsoleUI.WriteLine("*** ParseArgumentsStrict returned false");
            if (options.BeQuiet == true)
                ConsoleUI.BeSilent();
            else
                ConsoleUI.BeVerbatim();
            ConsoleUI.Welcome();

            #region File name logic
            const string inputFileExtension = "sdf";
            string inputFileName;
            string outputFileName = "";
            string residualsFileName = "";
            // get the filename(s) from command line
            string[] fileNames = options.ListOfFileNames.ToArray();
            if (fileNames.Length == 0)
                ConsoleUI.ErrorExit("!Missing file name", 1);
            if (Path.GetExtension(fileNames[0]) == string.Empty)
                inputFileName = Path.ChangeExtension(fileNames[0], inputFileExtension);
            else
                inputFileName = fileNames[0];
            if (fileNames.Length == 1)
            { // one filename given
                outputFileName = Path.ChangeExtension(fileNames[0], options.OutFileExt);
                residualsFileName = Path.ChangeExtension(fileNames[0], options.ResFileExt); ;
            }
            if (fileNames.Length == 2)
            { // two filenames given
                outputFileName = Path.ChangeExtension(fileNames[1], options.OutFileExt);
                residualsFileName = Path.ChangeExtension(fileNames[1], options.ResFileExt);
            }
            if (fileNames.Length > 2)
            { // three filenames given
                outputFileName = Path.ChangeExtension(fileNames[1], options.OutFileExt);
                residualsFileName = Path.ChangeExtension(fileNames[2], options.ResFileExt);
            }
            #endregion

            // check if multipatch
            // TODO provide means to use two scan fields
            if (options.Multifile)
                scanFieldTopology = ScanFieldTopology.Three;
            else
                scanFieldTopology = ScanFieldTopology.Single;

            //prepare BcrReader objects
            BcrReader bcrReaderA = null; // used also for a single input file
            BcrReader bcrReaderB = null;
            BcrReader bcrReaderC = null;

            #region Read input file(s)
            if (scanFieldTopology == ScanFieldTopology.Single)
            {
                ConsoleUI.ReadingFile(inputFileName);
                bcrReaderA = new BcrReader(inputFileName);
                ConsoleUI.Done();
                if (bcrReaderA.Status != ErrorCode.OK)
                    ConsoleUI.ErrorExit($"!BcrReader ErrorCode: {bcrReaderA.Status}", 2);
            }
            if (scanFieldTopology == ScanFieldTopology.Two)
            {
                // left patch
                string fileA = Path.ChangeExtension(Path.GetFileNameWithoutExtension(inputFileName) + "A", inputFileExtension);
                ConsoleUI.ReadingFile(fileA);
                bcrReaderA = new BcrReader(fileA);
                ConsoleUI.Done();
                if (bcrReaderA.Status != ErrorCode.OK)
                    ConsoleUI.ErrorExit($"!BcrReader ErrorCode: {bcrReaderA.Status}", 10);
                // center patch
                string fileB = Path.ChangeExtension(Path.GetFileNameWithoutExtension(inputFileName) + "B", inputFileExtension);
                ConsoleUI.ReadingFile(fileB);
                bcrReaderB = new BcrReader(fileB);
                ConsoleUI.Done();
                if (bcrReaderB.Status != ErrorCode.OK)
                    ConsoleUI.ErrorExit($"!BcrReader ErrorCode: {bcrReaderB.Status}", 10);
                // check if the three files are compatible
                if (bcrReaderA.NumProfiles != bcrReaderB.NumProfiles)
                    ConsoleUI.ErrorExit($"!Geometry of input files incompatible", 11);
            }
            if (scanFieldTopology == ScanFieldTopology.Three)
            {
                // left patch
                string fileA = Path.ChangeExtension(Path.GetFileNameWithoutExtension(inputFileName) + "A", inputFileExtension);
                ConsoleUI.ReadingFile(fileA);
                bcrReaderA = new BcrReader(fileA);
                ConsoleUI.Done();
                if (bcrReaderA.Status != ErrorCode.OK)
                    ConsoleUI.ErrorExit($"!BcrReader ErrorCode: {bcrReaderA.Status}", 10);
                // center patch
                string fileB = Path.ChangeExtension(Path.GetFileNameWithoutExtension(inputFileName) + "B", inputFileExtension);
                ConsoleUI.ReadingFile(fileB);
                bcrReaderB = new BcrReader(fileB);
                ConsoleUI.Done();
                if (bcrReaderB.Status != ErrorCode.OK)
                    ConsoleUI.ErrorExit($"!BcrReader ErrorCode: {bcrReaderB.Status}", 10);
                // right patch
                string fileC = Path.ChangeExtension(Path.GetFileNameWithoutExtension(inputFileName) + "C", inputFileExtension);
                ConsoleUI.ReadingFile(fileC);
                bcrReaderC = new BcrReader(fileC);
                ConsoleUI.Done();
                if (bcrReaderC.Status != ErrorCode.OK)
                    ConsoleUI.ErrorExit($"!BcrReader ErrorCode: {bcrReaderC.Status}", 10);
                // check if the three files are compatible
                if (bcrReaderA.NumProfiles != bcrReaderB.NumProfiles || bcrReaderB.NumProfiles != bcrReaderC.NumProfiles)
                    ConsoleUI.ErrorExit($"!Geometry of input files incompatible", 11);
            }
            #endregion

            #region Set offset for the scan field
            if (scanFieldTopology == ScanFieldTopology.Single)
            {
                bcrReaderA.SetXOffset(0.0);
                bcrReaderA.SetYOffset(0.0);
                bcrReaderA.SetZOffset(0.0);
            }
            if (scanFieldTopology == ScanFieldTopology.Two)
            {
                double x0A = bcrReaderA.XOffset;
                double x0B = bcrReaderB.XOffset;
                bcrReaderA.SetXOffset(0.0);
                bcrReaderB.SetXOffset(x0B - x0A);
                bcrReaderA.SetYOffset(0.0);
                bcrReaderA.SetZOffset(0.0);
                bcrReaderB.SetYOffset(0.0);
                bcrReaderB.SetZOffset(0.0);
            }
            if (scanFieldTopology == ScanFieldTopology.Three)
            {
                double x0A = bcrReaderA.XOffset;
                double x0B = bcrReaderB.XOffset;
                double x0C = bcrReaderC.XOffset;
                bcrReaderA.SetXOffset(0.0);
                bcrReaderB.SetXOffset(x0B - x0A);
                bcrReaderC.SetXOffset(x0C - x0A);
                bcrReaderA.SetYOffset(0.0);
                bcrReaderA.SetZOffset(0.0);
                bcrReaderB.SetYOffset(0.0);
                bcrReaderB.SetZOffset(0.0);
                bcrReaderC.SetYOffset(0.0);
                bcrReaderC.SetZOffset(0.0);
            }
            #endregion

            #region Y-band logic
            double yStart = options.Y0 * 1e-6;              // in m, ignore profiles with y less than this value
            double yEnd = yStart + (options.DeltaY * 1e-6); // in m, ignore profiles with y greater than this value
            if (yStart > yEnd)
            {
                double yTemp = yEnd;
                yEnd = yStart;
                yStart = yTemp;
            }
            #endregion

            #region Diagnostic console output
            int pointsPerProfile = 0;
            int numberPatches = 0;
            Point3D[] firstProfile = ExtractProfile(0, bcrReaderA, bcrReaderB, bcrReaderC);
            double scanFieldWidth = firstProfile.Max().X - firstProfile.Min().X;
            if (scanFieldTopology == ScanFieldTopology.Single)
            {
                numberPatches = 1;
                pointsPerProfile = bcrReaderA.NumPoints;
            }
            if (scanFieldTopology == ScanFieldTopology.Two)
            {
                numberPatches = 2;
                pointsPerProfile = bcrReaderA.NumPoints + bcrReaderB.NumPoints;
            }
            if (scanFieldTopology == ScanFieldTopology.Three)
            {
                numberPatches = 3;
                pointsPerProfile = bcrReaderA.NumPoints + bcrReaderB.NumPoints + bcrReaderC.NumPoints;
            }
            // FitVerticalStandard must be called here for getting the feature type designation
            FitVerticalStandard fitVerticalStandard = new FitVerticalStandard(GetFeatureTypeFor(options.TypeIndex), options.W1, options.W2, options.W3);
            ConsoleUI.WriteLine($"Disjoined scan fields: {numberPatches}");
            ConsoleUI.WriteLine($"Number of points per profile: {pointsPerProfile}");
            ConsoleUI.WriteLine($"Number of profiles: {bcrReaderA.NumProfiles}");
            ConsoleUI.WriteLine($"Feature type: {fitVerticalStandard.FeatureTypeDesignation}");
            ConsoleUI.WriteLine($"W1: {options.W1}");
            ConsoleUI.WriteLine($"W2: {options.W2}");
            ConsoleUI.WriteLine($"W3: {options.W3}");
            ConsoleUI.WriteLine($"Position of left feature edge: {options.LeftX} {microMeter}");
            ConsoleUI.WriteLine($"Position of right feature edge: {options.RightX} {microMeter}");
            ConsoleUI.WriteLine($"y-value of first profile {options.Y0} {microMeter}");
            if (options.DeltaY > bcrReaderA.RasterData.ScanFieldDimensionY * 1e6)
                ConsoleUI.WriteLine($"Width of y-band to evaluate: infinity");
            else
                ConsoleUI.WriteLine($"Width of y-band to evaluate: {options.DeltaY} {microMeter}");
            ConsoleUI.WriteLine($"Threshold for residuals: {options.MaxSpan} {microMeter}");
            #endregion

            #region Fit requested profiles
            ConsoleUI.WriteLine("Start fitting profiles.");
            int numberDiscardedProfiles = 0;
            FitStatistics fitStatistics = new FitStatistics(fitVerticalStandard);
            StringBuilder fittedProfilsResult = new StringBuilder();
            double featureWidth = double.NaN;
            for (int profileIndex = 0; profileIndex < bcrReaderA.NumProfiles; profileIndex++)
            {
                double y = bcrReaderA.GetPointFor(0, profileIndex).Y;
                if (y >= yStart && y <= yEnd)
                {
                    Point3D[] currentProfile = ExtractProfile(profileIndex, bcrReaderA, bcrReaderB, bcrReaderC);
                    fitVerticalStandard.FitProfile(currentProfile, options.LeftX * 1e-6, options.RightX * 1e-6);
                    if (fitVerticalStandard.Status == FitStatus.BadEdgePosition)
                        ConsoleUI.ErrorExit("!Feature edge location outside of profile", 30);
                    if(fitVerticalStandard.Status!=FitStatus.Success)
                    {
                        ConsoleUI.WriteLine($" > {profileIndex,5} profile discarded ({fitVerticalStandard.Status})");
                        numberDiscardedProfiles++;
                        break;
                    }
                    featureWidth = fitVerticalStandard.FeatureWidth; // for later use
                    if (fitVerticalStandard.RangeOfResiduals < options.MaxSpan * 1e-6)
                    {
                        string resultLine = FormattedStringForFitResult(profileIndex, fitVerticalStandard);
                        ConsoleUI.WriteLine($" > {resultLine}");
                        fittedProfilsResult.AppendLine(resultLine);
                        fitStatistics.Update();
                    }
                    else
                    {
                        ConsoleUI.WriteLine($" > {profileIndex,5} profile discarded ({fitVerticalStandard.RangeOfResiduals*1e6:F4} {microMeter})");
                        numberDiscardedProfiles++;
                    }
                }
            }
            if (fitStatistics.NumberOfSamples == 0)
                ConsoleUI.ErrorExit("!No valid profile fit found", 3);
            if (fitStatistics.NumberOfSamples == 1)
                ConsoleUI.WriteLine($"1 profile fitted, {numberDiscardedProfiles} discarded.");
            if (fitStatistics.NumberOfSamples > 1)
                ConsoleUI.WriteLine($"{fitStatistics.NumberOfSamples} profiles fitted, {numberDiscardedProfiles} discarded.");
            if (fitStatistics.AverageHeight < 1e-7)
                ConsoleUI.WriteLine($"Average feature heigth/depth {fitStatistics.AverageHeight * 1e9:F2} nm");
            else
                ConsoleUI.WriteLine($"Average feature heigth/depth {fitStatistics.AverageHeight * 1e6:F3} {microMeter}");

            #endregion

            #region Collate calibration (output) data
            StringBuilder reportStringBuilder = new StringBuilder();
            reportStringBuilder.AppendLine($"# Output of {ConsoleUI.Title}, version {ConsoleUI.Version}");
            reportStringBuilder.AppendLine($"InputFile                 = {inputFileName}");
            reportStringBuilder.AppendLine($"DisjointScanFields        = {numberPatches}");
            reportStringBuilder.AppendLine($"ManufacID                 = {bcrReaderA.ManufacID}");
            reportStringBuilder.AppendLine($"UserComment               = {options.UserComment}");
            reportStringBuilder.AppendLine($"NumberOfPointsPerProfile  = {pointsPerProfile}");
            reportStringBuilder.AppendLine($"NumberOfProfiles          = {bcrReaderA.NumProfiles}");
            reportStringBuilder.AppendLine($"XScale                    = {bcrReaderA.XScale * 1e6} {microMeter}");
            reportStringBuilder.AppendLine($"YScale                    = {bcrReaderA.YScale * 1e6} {microMeter}");
            reportStringBuilder.AppendLine($"ZScale                    = {bcrReaderA.ZScale * 1e6} {microMeter}");
            reportStringBuilder.AppendLine($"ScanFieldWidth            = {scanFieldWidth * 1e6:F2} {microMeter}");
            reportStringBuilder.AppendLine($"ScanFieldHeight           = {bcrReaderA.RasterData.ScanFieldDimensionY * 1e6} {microMeter}");
            reportStringBuilder.AppendLine($"# Fit parameters =====================================");
            reportStringBuilder.AppendLine($"FeatureType               = {GetFeatureTypeFor(options.TypeIndex)}");
            reportStringBuilder.AppendLine($"W1                        = {options.W1}");
            reportStringBuilder.AppendLine($"W2                        = {options.W2}");
            reportStringBuilder.AppendLine($"W3                        = {options.W3}");
            reportStringBuilder.AppendLine($"FirstFeatureEdge          = {options.LeftX} {microMeter}");
            reportStringBuilder.AppendLine($"SecondFeatureEdge         = {options.RightX} {microMeter}");
            reportStringBuilder.AppendLine($"FeatureWidth              = {featureWidth * 1e6} {microMeter}");
            reportStringBuilder.AppendLine($"FirstProfilePosition      = {options.Y0} {microMeter}");
            if (options.DeltaY > bcrReaderA.RasterData.ScanFieldDimensionY * 1e6)
                reportStringBuilder.AppendLine($"EvaluationWidth           = infinity");
            else
                reportStringBuilder.AppendLine($"EvaluationWidth           = {options.DeltaY} {microMeter}");
            reportStringBuilder.AppendLine($"ThresholdResiduals        = {options.MaxSpan} {microMeter}");
            reportStringBuilder.AppendLine($"# Fit results =======================================");
            reportStringBuilder.AppendLine($"NumberOfValidProfiles     = {fitStatistics.NumberOfSamples}");
            reportStringBuilder.AppendLine($"NumberOfDiscardedProfiles = {numberDiscardedProfiles}");
            reportStringBuilder.AppendLine($"AverageHeight             = {fitStatistics.AverageHeight * 1e6:F5} {microMeter}");
            reportStringBuilder.AppendLine($"RangeOfHeights            = {fitStatistics.HeightRange * 1e6:F5} {microMeter}");
            reportStringBuilder.AppendLine($"AveragePt                 = {fitStatistics.AveragePt * 1e6:F5} {microMeter}");
            reportStringBuilder.AppendLine($"RangeOfPt                 = {fitStatistics.PtRange * 1e6:F5} {microMeter}");
            if (GetFeatureTypeFor(options.TypeIndex) == FeatureType.A2Groove ||
                GetFeatureTypeFor(options.TypeIndex) == FeatureType.A2Ridge)
            {
                reportStringBuilder.AppendLine($"AverageRadius             = {fitStatistics.AverageA2Radius * 1e6:F1} {microMeter}");
                reportStringBuilder.AppendLine($"RangeOfRadii              = {fitStatistics.A2RadiusRange * 1e6:F1} {microMeter}");
            }
            reportStringBuilder.AppendLine($"# Columns ============================================");
            reportStringBuilder.AppendLine($"# 1 : Profile index");
            reportStringBuilder.AppendLine($"# 2 : Profile position / {microMeter}");
            reportStringBuilder.AppendLine($"# 3 : Feature height/depth / {microMeter}");
            reportStringBuilder.AppendLine($"# 4 : Pt / {microMeter}");
            reportStringBuilder.AppendLine($"# 5 : Range of residuals / {microMeter}");
            if (GetFeatureTypeFor(options.TypeIndex) == FeatureType.A2Groove ||
                GetFeatureTypeFor(options.TypeIndex) == FeatureType.A2Ridge)
            {
                reportStringBuilder.AppendLine($"# 6 : Radius / {microMeter}");
                reportStringBuilder.AppendLine($"# 7 : Asymmetry index");
            }
            reportStringBuilder.AppendLine($"#=====================================================");
            reportStringBuilder.Append(fittedProfilsResult);
            #endregion

            #region Write output file
            ConsoleUI.WritingFile(outputFileName);
            try
            {
                File.WriteAllText(outputFileName, reportStringBuilder.ToString());
            }
            catch
            {
                ConsoleUI.ErrorExit("!Error writing file", 2);
            }
            ConsoleUI.Done();
            #endregion

            #region Prepare and write average residual plot
            if (fitStatistics.AverageResidualPlot != null)
            {
                StringBuilder plot = new StringBuilder();
                plot.AppendLine($"x coordinate in {microMeter}{options.Separator}average fit residuals in nm");

                foreach (var point in fitStatistics.AverageResidualPlot)
                {
                    double x = point.X * 1e6;
                    double z = point.Z * 1e9;
                    plot.AppendLine($"{x}{options.Separator}{z:F3}");
                }
                // and now write to file
                ConsoleUI.WritingFile(residualsFileName);
                try
                {
                    File.WriteAllText(residualsFileName, plot.ToString());
                }
                catch
                {
                    ConsoleUI.ErrorExit("!Error writing file", 2);
                }
                ConsoleUI.Done();
            }
            #endregion

        }

        //=====================================================================

        private static Point3D[] ExtractProfile(int profileIndex, BcrReader bcrReaderA, BcrReader bcrReaderB, BcrReader bcrReaderC)
        {
            if (bcrReaderA == null)
                ConsoleUI.ErrorExit("!This should not happen", 20);
            if (bcrReaderB == null && bcrReaderC == null)
                return bcrReaderA.GetPointsProfileFor(profileIndex);
            List<Point3D> points = new List<Point3D>();
            foreach (var point in bcrReaderA.GetPointsProfileFor(profileIndex))
                points.Add(point);
            if (bcrReaderB != null)
                foreach (var point in bcrReaderB.GetPointsProfileFor(profileIndex))
                    points.Add(point);
            if (bcrReaderC != null)
                foreach (var point in bcrReaderC.GetPointsProfileFor(profileIndex))
                    points.Add(point);
            return points.ToArray();
        }

        //=====================================================================

        private static string FormattedStringForFitResult(int profileIndex, FitVerticalStandard fvs)
        {
            string retString = "";
            double h = fvs.Height * 1e6;    // nm
            double pt = fvs.Pt * 1e6;       // nm
            double res = fvs.RangeOfResiduals * 1e6;    // nm
            double r = fvs.A2Radius * 1e6;  // µm
            double y = fvs.Yposition * 1e6; // µm
            double asy = fvs.A2Asymmetry;
            switch (fvs.FeatureType)
            {
                case FeatureType.A1Groove:
                case FeatureType.A1Ridge:
                case FeatureType.FallingEdge:
                case FeatureType.RisingEdge:
                    // output for rectangular (flat toped) features
                    retString = $"{profileIndex,5} {y,7:F1} {h,10:F4} {pt,10:F4} {res,10:F4}";
                    break;
                case FeatureType.A2Groove:
                case FeatureType.A2Ridge:
                    // output for cylindrical features
                    retString = $"{profileIndex,5} {y,7:F1} {h,10:F4} {pt,10:F4} {res,10:F4} {r,8:F1} {asy,6:F3}";
                    break;
            }
            return retString;
        }

        //=====================================================================

        private static FeatureType GetFeatureTypeFor(int index)
        {
            switch (index)
            {
                case 1:
                    return FeatureType.A1Ridge;
                case 2:
                    return FeatureType.A2Groove;
                case 3:
                    return FeatureType.A1Groove;
                case 4:
                    return FeatureType.A2Ridge;
                case 5:
                    return FeatureType.RisingEdge;
                case 6:
                    return FeatureType.FallingEdge;
                default:
                    return FeatureType.None;
            }
        }

        //=====================================================================
    }

    public enum ScanFieldTopology
    {
        Unknown,
        Single,
        Two,
        Three
    }
}
