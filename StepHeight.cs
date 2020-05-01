﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Bev.IO.BcrReader;
using Bev.SurfaceRasterData;

namespace StepHeight
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            // parse command line arguments
            var options = new Options();
            if (!CommandLine.Parser.Default.ParseArgumentsStrict(args, options))
                Console.WriteLine("*** ParseArgumentsStrict returned false");
            // consume the verbosity option
            if (options.BeQuiet == true)
                ConsoleUI.BeSilent();
            else
                ConsoleUI.BeVerbatim();
            ConsoleUI.Welcome();

            #region File name logic
            const string inputFileExtension = "sdf";
            const string outputFileExtension = "prn";
            const string residualsFileExtension = "txt";
            string inputFileName = "";
            string outputFileName = "";
            string residualsFileName = "";
            // get the filename(s) from command line
            string[] fileNames = options.ListOfFileNames.ToArray();
            if (fileNames.Length == 0)
            {
                //inputFileName = @"Halle1001_2015_05.sdf";
                //inputFileName = @"SHS80_2020_06.sdf";
                ConsoleUI.ErrorExit("!Missing file name", 1);
            }
            if (fileNames.Length == 1)
            { // one filename given
                //TODO check if extension present
                inputFileName = Path.ChangeExtension(fileNames[0], inputFileExtension);
                outputFileName = Path.ChangeExtension(fileNames[0], outputFileExtension);
                residualsFileName = Path.ChangeExtension(fileNames[0], residualsFileExtension); ;
            }
            if (fileNames.Length == 2)
            { // more than one filename
                inputFileName = Path.ChangeExtension(fileNames[0], inputFileExtension);
                outputFileName = Path.ChangeExtension(fileNames[1], outputFileExtension);
                residualsFileName = Path.ChangeExtension(fileNames[1], residualsFileExtension);
            }
            if (fileNames.Length >= 2)
            { // more than one filename
                inputFileName = Path.ChangeExtension(fileNames[0], inputFileExtension);
                outputFileName = Path.ChangeExtension(fileNames[1], outputFileExtension);
                residualsFileName = Path.ChangeExtension(fileNames[2], residualsFileExtension);
            }
            #endregion

            #region Read input file
            ConsoleUI.ReadingFile(inputFileName);
            BcrReader bcrReader = new BcrReader(inputFileName);
            ConsoleUI.Done();
            if (bcrReader.Status != ErrorCode.OK)
                ConsoleUI.ErrorExit($"!BcrReader ErrorCode: {bcrReader.Status}", 2);
            #endregion

            // now comes the real work

            Console.WriteLine($"X dimension: {bcrReader.RasterData.ScanFieldDimensionX * 1000:F3} mm");
            Console.WriteLine($"Y dimension: {bcrReader.RasterData.ScanFieldDimensionY * 1000:F3} mm");
            Console.WriteLine();

            FitVerticalStandard fitVerticalStandard = new FitVerticalStandard(GetFeatureTypeFor(options.TypeIndex), options.W1, options.W2, options.W3);
            //double left = 0.000179;
            //double right = 0.000340;
            //double left  = 0.001133;
            //double right = 0.001379;
            //double left = 0.000150;
            //double right = 0.000250;

            List<double> results = new List<double>();
            List<double> filteredResults = new List<double>();

            for (int i = 0; i < bcrReader.NumProfiles; i++)
            {

                fitVerticalStandard.FitProfile(bcrReader.GetPointsProfileFor(i), options.LeftX*1e6, options.RightX*1e6);
                double h = fitVerticalStandard.Height * 1e9;
                double pt = fitVerticalStandard.Pt * 1e9;
                double res = fitVerticalStandard.RangeOfResiduals * 1e9;
                double r = fitVerticalStandard.A2Radius * 1e6;

                Console.WriteLine($"profile {i,5} | hight: {h:F1} nm | Pt: {pt:F1} nm | res: {res,6:F1} nm");
                results.Add(h);
                if (res < 100.0)
                {
                    filteredResults.Add(h);
                }
                else
                {
                    Console.WriteLine("discarded!");
                }
            }
            Console.WriteLine();
            Console.WriteLine($"Average height: {results.Average():F1} nm");
            Console.WriteLine($"Average filtered height: {filteredResults.Average():F1} nm");
            Console.WriteLine();

            fitVerticalStandard.FitProfile(bcrReader.GetPointsProfileFor(275), left, right);

            foreach (var point in fitVerticalStandard.Residuals)
            {
                Console.WriteLine($"{point.X * 1e6,6:F1} µm {point.Z * 1e9,8:F1} nm");
            }


        }

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
    }
}
