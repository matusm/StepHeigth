using At.Matus.StatisticPod;
using Bev.SurfaceRasterData;

namespace StepHeight
{
    public class FitStatistics
    {

        private readonly FitVerticalStandard fvs;
        private readonly StatisticPod heightPod = new StatisticPod();
        private readonly StatisticPod ptPod = new StatisticPod();
        private readonly StatisticPod radiusPod = new StatisticPod();
        private readonly StatisticPod residualRangePod = new StatisticPod();
        private Point3D[] sumOfResidualPlots;
        private int numberOfResidualPlots;

        public FitStatistics(FitVerticalStandard fvs)
        {
            this.fvs = fvs; 
            Restart();
        }

        public int NumberOfSamples => (int)heightPod.SampleSize;
        public double AverageHeight => heightPod.AverageValue;
        public double HeightRange => heightPod.Range;
        public double HeightStdDev => heightPod.StandardDeviation;
        public double AveragePt => ptPod.AverageValue;
        public double PtRange => ptPod.Range;
        public double AverageA2Radius => radiusPod.AverageValue;
        public double A2RadiusRange => radiusPod.Range;
        public double AverageResiduals => residualRangePod.AverageValue;
        public Point3D[] AverageResidualPlot => GetAverageResidualPlot();

        public void Update()
        {
            heightPod.Update(fvs.Height);
            ptPod.Update(fvs.Pt);
            residualRangePod.Update(fvs.RangeOfResiduals);
            radiusPod.Update(fvs.A2Radius);
            UpdateResidualsPlot();
        }

        public void Restart()
        {
            heightPod.Restart();
            ptPod.Restart();
            radiusPod.Restart();
            residualRangePod.Restart();
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
