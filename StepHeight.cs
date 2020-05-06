using System;
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

            #region Y-band logic
            double yStart = options.Y0 * 1e-6;              // in m, ignore profiles with y less than this value
            double yEnd = yStart + options.DeltaY * 1e-6;  // in m, ignore profiles with y greater than this value
            if (yStart > yEnd)
            {
                double yTemp = yEnd;
                yEnd = yStart;
                yStart = yTemp;
            }
            #endregion

            FitVerticalStandard fitVerticalStandard = new FitVerticalStandard(GetFeatureTypeFor(options.TypeIndex), options.W1, options.W2, options.W3);
            FitStatistics fitStatistics = new FitStatistics(fitVerticalStandard);

            for (int i = 0; i < bcrReader.NumProfiles; i++)
            {
                double y = bcrReader.GetPointFor(0, i).Y;
                if (y >= yStart && y <= yEnd)
                {
                    fitVerticalStandard.FitProfile(bcrReader.GetPointsProfileFor(i), options.LeftX * 1e-6, options.RightX * 1e-6);
                    if (fitVerticalStandard.RangeOfResiduals < options.MaxSpan * 1e-6)
                    {
                        Console.WriteLine(fitVerticalStandard.ToFormattedString(i));
                        fitStatistics.Update();
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Heigth/nm: {fitStatistics.AverageHeight * 1e9,6:F2} ± {fitStatistics.HeightRange * 0.5e9:F2}");
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
