﻿using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace StepHeight
{
    public class Options
    {
        [Option("type", DefaultValue = 1, HelpText = "feature type to be fitted")]
        public int TypeIndex { get; set; }

        [Option("X1", DefaultValue = 0.0, HelpText = "x-value of first edge of feature.")]
        public double LeftX { get; set; }

        [Option("X2", DefaultValue = 0.0, HelpText = "x-value of second edge of feature.")]
        public double RightX { get; set; }

        [Option("W1", DefaultValue = 3.0, HelpText = "Parameter W1 of evaluation region.")]
        public double W1 { get; set; }

        [Option("W2", DefaultValue = (2.0 / 3.0), HelpText = "Parameter W2 of evaluation region.")]
        public double W2 { get; set; }

        [Option("W3", DefaultValue = (1.0 / 3.0), HelpText = "Parameter W3 of evaluation region.")]
        public double W3 { get; set; }

        [Option('q', "quiet", HelpText = "Quiet mode. No screen output (except for errors).")]
        public bool BeQuiet { get; set; }

        [Option("comment", DefaultValue = "", HelpText = "User supplied comment string.")]
        public string UserComment { get; set; }

        [Option("maxspan", DefaultValue = 1.0, HelpText = "Discard fit if residuals are larger, in um")]
        public double MaxSpan { get; set; }

        [Option("precision", DefaultValue = 4, HelpText = "Decimal digits for results")]
        public int Precision { get; set; }

        [ValueList(typeof(List<string>), MaximumElements = 3)]
        public IList<string> ListOfFileNames { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            string AppName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            string AppVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            HelpText help = new HelpText
            {
                Heading = new HeadingInfo(AppName, "version " + AppVer),
                Copyright = new CopyrightInfo("Michael Matus", 2020),
                AdditionalNewLineAfterOption = false,
                AddDashesToOption = true
            };
            string sPre = "Program to evaluate BCR raster data files for step heights, groove depths, or edge heights. " +
            "The step heighs per profile are separatly outputed together with the average value. " +
            "The type of feature must be provided via the --type option. " +
            " ";
            help.AddPreOptionsLine(sPre);
            help.AddPreOptionsLine("");
            help.AddPreOptionsLine("Usage: " + AppName + " filename1 [filename2] [filename3] [options]");
            help.AddPostOptionsLine("");
            help.AddPostOptionsLine("Supported values for --type:");
            help.AddPostOptionsLine("   1: ISO A1 (rectangular ridge)");
            help.AddPostOptionsLine("   2: ISO A2 (cylindrical groove)");
            help.AddPostOptionsLine("   3: ISO A1 (rectangular groove)");
            help.AddPostOptionsLine("   4: ISO A2 (cylindrical ridge)");
            help.AddPostOptionsLine("   5: rising step");
            help.AddPostOptionsLine("   6: falling step");

            help.AddOptions(this);

            return help;
        }


    }
}