using System;
using System.Collections.Generic;
using System.Linq;
using Bev.SurfaceRasterData;

namespace StepHeight
{
    public class FitStatistics
    {

        private FitVerticalStandard fvs;
        private List<double> h = new List<double>();
        private List<double> pt = new List<double>();
        private List<double> r = new List<double>();
        private List<double> res = new List<double>();
        private Point3D[] sumOfResidualPlots;
        private int numberOfResidualPlots;

        public FitStatistics(FitVerticalStandard fvs)
        {
            this.fvs = fvs; 
            Restart();
        }

        public int NumberOfSamples => h.Count;
        public double AverageHeight => h.Average();
        public double HeightRange => h.Max() - h.Min();
        public double AveragePt => pt.Average();
        public double PtRange => pt.Max() - pt.Min();
        public double AverageA2Radius => r.Average();
        public double A2RadiusRange => r.Max() - r.Min();
        public double AverageResiduals => res.Average();
        public Point3D[] AverageResidualPlot => GetAverageResidualPlot();

        public void Update()
        {
            h.Add(fvs.Height);
            pt.Add(fvs.Pt);
            res.Add(fvs.RangeOfResiduals);
            r.Add(fvs.A2Radius);
            UpdateResidualsPlot();
        }

        public void Restart()
        {
            h.Clear();
            pt.Clear();
            r.Clear();
            res.Clear();
            sumOfResidualPlots = null;
            numberOfResidualPlots = 0;
        }

        private void UpdateResidualsPlot()
        {
            InitializeResidualsField();
            if (sumOfResidualPlots.Length != fvs.Residuals.Length) return;
            for (int i = 0; i < sumOfResidualPlots.Length; i++)
            {
                sumOfResidualPlots[i].Z += fvs.Residuals[i].Z;
            }
            numberOfResidualPlots++;
        }

        private Point3D[] GetAverageResidualPlot()
        {
            if (numberOfResidualPlots == 0) return null;
            Point3D[] average = new Point3D[sumOfResidualPlots.Length];
            for (int i = 0; i < average.Length; i++)
            {
                average[i] = new Point3D(sumOfResidualPlots[i].X, sumOfResidualPlots[i].Y, sumOfResidualPlots[i].Z / numberOfResidualPlots);
            }
            return average;
        }

        private void InitializeResidualsField()
        {
            if (sumOfResidualPlots==null)
            {
                sumOfResidualPlots = new Point3D[fvs.Residuals.Length];
                for (int i = 0; i < fvs.Residuals.Length; i++)
                {
                    sumOfResidualPlots[i] = new Point3D(fvs.Residuals[i].X, 0, 0);
                }
                numberOfResidualPlots = 0;
            }
        }
    }
}
