﻿using System;
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
            string inputFileName = "";
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
            if (fileNames.Length >= 2)
            { // three filenames given
                outputFileName = Path.ChangeExtension(fileNames[1], options.OutFileExt);
                residualsFileName = Path.ChangeExtension(fileNames[2], options.ResFileExt);
            }
            #endregion

            #region Read input file
            ConsoleUI.ReadingFile(inputFileName);
            BcrReader bcrReader = new BcrReader(inputFileName);
            ConsoleUI.Done();
            if (bcrReader.Status != ErrorCode.OK)
                ConsoleUI.ErrorExit($"!BcrReader ErrorCode: {bcrReader.Status}", 2);
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

            #region Diagnostic output
            // this is needed for getting the feature type designation
            FitVerticalStandard fitVerticalStandard = new FitVerticalStandard(GetFeatureTypeFor(options.TypeIndex), options.W1, options.W2, options.W3);
            ConsoleUI.WriteLine($"Number of profiles in scan: {bcrReader.NumProfiles}");
            ConsoleUI.WriteLine($"Feature type: {fitVerticalStandard.FeatureTypeDesignation}");
            ConsoleUI.WriteLine($"W1: {options.W1}");
            ConsoleUI.WriteLine($"W2: {options.W2}");
            ConsoleUI.WriteLine($"W3: {options.W3}");
            ConsoleUI.WriteLine($"Position of left edge: {options.LeftX} {microMeter}");
            ConsoleUI.WriteLine($"Position of right edge: {options.RightX} {microMeter}");
            ConsoleUI.WriteLine($"y-value of first profile {options.Y0} {microMeter}");
            ConsoleUI.WriteLine($"Width of y-band to evaluate: {options.DeltaY} {microMeter}");
            ConsoleUI.WriteLine($"Residual threshold for discarding: {options.MaxSpan} {microMeter}");
            #endregion

            #region Fit requested profiles
            int numberDiscardedProfiles = 0;
            FitStatistics fitStatistics = new FitStatistics(fitVerticalStandard);
            StringBuilder fittedProfilsResult = new StringBuilder();
            double featureWidth = double.NaN;
            for (int i = 0; i < bcrReader.NumProfiles; i++)
            {
                double y = bcrReader.GetPointFor(0, i).Y;
                if (y >= yStart && y <= yEnd)
                {
                    fitVerticalStandard.FitProfile(bcrReader.GetPointsProfileFor(i), options.LeftX * 1e-6, options.RightX * 1e-6);
                    featureWidth = fitVerticalStandard.FeatureWidth; // for later use
                    if (fitVerticalStandard.RangeOfResiduals < options.MaxSpan * 1e-6)
                    {
                        string resultLine = ToFormattedString(i, fitVerticalStandard);
                        ConsoleUI.WriteLine($" > {resultLine}");
                        fittedProfilsResult.AppendLine(resultLine);
                        fitStatistics.Update();
                    }
                    else
                    {
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

            #region Collate calibration (output) file data
            StringBuilder reportStringBuilder = new StringBuilder();
            reportStringBuilder.AppendLine($"# Output of {ConsoleUI.Title}, version {ConsoleUI.Version}");
            reportStringBuilder.AppendLine($"InputFile                 = {inputFileName}");
            reportStringBuilder.AppendLine($"ManufacID                 = {bcrReader.ManufacID}");
            reportStringBuilder.AppendLine($"UserComment               = {options.UserComment}");
            reportStringBuilder.AppendLine($"NumberOfPointsPerProfile  = {bcrReader.NumPoints}");
            reportStringBuilder.AppendLine($"NumberOfProfiles          = {bcrReader.NumProfiles}");
            reportStringBuilder.AppendLine($"XScale                    = {bcrReader.XScale * 1e6} {microMeter}");
            reportStringBuilder.AppendLine($"YScale                    = {bcrReader.YScale * 1e6} {microMeter}");
            reportStringBuilder.AppendLine($"ZScale                    = {bcrReader.ZScale * 1e6} {microMeter}");
            reportStringBuilder.AppendLine($"ScanFieldWidth            = {bcrReader.RasterData.ScanFieldDimensionX * 1e6} {microMeter}");
            reportStringBuilder.AppendLine($"ScanFieldHeight           = {bcrReader.RasterData.ScanFieldDimensionY * 1e6} {microMeter}");
            reportStringBuilder.AppendLine($"# Fit parameters =====================================");
            reportStringBuilder.AppendLine($"FeatureType               = {GetFeatureTypeFor(options.TypeIndex)}");
            reportStringBuilder.AppendLine($"W1                        = {options.W1}");
            reportStringBuilder.AppendLine($"W2                        = {options.W2}");
            reportStringBuilder.AppendLine($"W3                        = {options.W3}");
            reportStringBuilder.AppendLine($"FirstFeatureEdge          = {options.LeftX} {microMeter}");
            reportStringBuilder.AppendLine($"SecondFeatureEdge         = {options.RightX} {microMeter}");
            reportStringBuilder.AppendLine($"FeatureWidth              = {featureWidth * 1e6} {microMeter}");
            reportStringBuilder.AppendLine($"FirstProfilePosition      = {options.Y0} {microMeter}");
            if (options.DeltaY > bcrReader.RasterData.ScanFieldDimensionY * 1e6)
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
                plot.AppendLine($"1 : x coordinate in {microMeter}");
                plot.AppendLine($"2 : average fit residuals in nm");
                plot.AppendLine($"@@@@");
                foreach (var point in fitStatistics.AverageResidualPlot)
                {
                    double x = point.X * 1e6;
                    double z = point.Z * 1e9;
                    plot.AppendLine($"{x,10:F3} {z,10:F3}");
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

        static string ToFormattedString(int profileIndex, FitVerticalStandard fvs)
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

        static FeatureType GetFeatureTypeFor(int index)
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
}
