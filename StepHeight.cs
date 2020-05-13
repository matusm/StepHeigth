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
                        string resultLine = fitVerticalStandard.ToFormattedString(i);
                        ConsoleUI.WriteLine($" > {resultLine}");
                        fittedProfilsResult.AppendLine(resultLine);
                        fitStatistics.Update();
                    }
                }
            }
            if (fitStatistics.NumberOfSamples == 0)
                ConsoleUI.ErrorExit("!No valid profile fit found", 3);
            if (fitStatistics.NumberOfSamples == 1)
                ConsoleUI.WriteLine("1 profile fitted.");
            if (fitStatistics.NumberOfSamples > 1)
                ConsoleUI.WriteLine($"{fitStatistics.NumberOfSamples} profiles fitted.");
            #endregion

            #region Generate calibration (output) file
            StringBuilder reportStringBuilder = new StringBuilder();
            reportStringBuilder.AppendLine($"# Output of {ConsoleUI.Title}, version {ConsoleUI.FullVersion}");
            reportStringBuilder.AppendLine($"# Input data ========================================");
            reportStringBuilder.AppendLine($"# Input file summary ================================");
            reportStringBuilder.AppendLine($"InputFile                = {inputFileName}");
            reportStringBuilder.AppendLine($"ManufacID                = {bcrReader.ManufacID}");
            reportStringBuilder.AppendLine($"NumberOfPointsPerProfile = {bcrReader.NumPoints}");
            reportStringBuilder.AppendLine($"NumberOfProfiles         = {bcrReader.NumProfiles}");
            reportStringBuilder.AppendLine($"XScale                   = {bcrReader.XScale * 1e6} {microMeter}");
            reportStringBuilder.AppendLine($"YScale                   = {bcrReader.YScale * 1e6} {microMeter}");
            reportStringBuilder.AppendLine($"ZScale                   = {bcrReader.ZScale * 1e6} {microMeter}");
            reportStringBuilder.AppendLine($"ScanFieldWidth           = {bcrReader.RasterData.ScanFieldDimensionX*1e6} {microMeter}");
            reportStringBuilder.AppendLine($"ScanFieldHeight          = {bcrReader.RasterData.ScanFieldDimensionY * 1e6} {microMeter}");
            reportStringBuilder.AppendLine($"# Fit parameters ====================================");
            reportStringBuilder.AppendLine($"FeatureType              = {GetFeatureTypeFor(options.TypeIndex)}");
            reportStringBuilder.AppendLine($"W1                       = {options.W1}");
            reportStringBuilder.AppendLine($"W2                       = {options.W2}");
            reportStringBuilder.AppendLine($"W3                       = {options.W3}");
            reportStringBuilder.AppendLine($"FirstFeatureEdge         = {options.LeftX} {microMeter}");
            reportStringBuilder.AppendLine($"SecondFeatureEdge        = {options.RightX} {microMeter}");
            reportStringBuilder.AppendLine($"FeatureWidth             = {featureWidth*1e6} {microMeter}");
            reportStringBuilder.AppendLine($"FirstProfilePosition     = {options.Y0} {microMeter}");
            if (options.DeltaY > bcrReader.RasterData.ScanFieldDimensionY * 1e6)
                reportStringBuilder.AppendLine($"EvaluationWidth          = infinity");
            else
                reportStringBuilder.AppendLine($"EvaluationWidth          = {options.DeltaY} {microMeter}");
            reportStringBuilder.AppendLine($"ThresholdResiduals       = {options.MaxSpan} {microMeter}");
            reportStringBuilder.AppendLine($"# Fit results =======================================");
            reportStringBuilder.AppendLine($"NumberOfValidProfiles    = {fitStatistics.NumberOfSamples}");
            reportStringBuilder.AppendLine($"AverageHeigth            = {fitStatistics.AverageHeight * 1e6,6:F5} {microMeter}");
            reportStringBuilder.AppendLine($"RangeOfHeights           = {fitStatistics.HeightRange * 1e6:F5} {microMeter}");
            reportStringBuilder.AppendLine($"# Columns ===========================================");
            reportStringBuilder.AppendLine($"# 1 : Profile index");
            reportStringBuilder.AppendLine($"# 2 : Transvers position / {microMeter}");
            reportStringBuilder.AppendLine($"# 3 : Feature height/depth / {microMeter}");
            reportStringBuilder.AppendLine($"# 4 : Pt / {microMeter}");
            reportStringBuilder.AppendLine($"# 5 : Range of residuals / {microMeter}");
            if (GetFeatureTypeFor(options.TypeIndex) == FeatureType.A2Groove ||
                GetFeatureTypeFor(options.TypeIndex) == FeatureType.A2Ridge)
            {
                reportStringBuilder.AppendLine($"# 6 : Radius / {microMeter}");
                reportStringBuilder.AppendLine($"# 7 : Asymmetry index");
            }
            reportStringBuilder.AppendLine($"#====================================================");
            reportStringBuilder.Append(fittedProfilsResult);
            #endregion


            Console.WriteLine(reportStringBuilder.ToString());
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
