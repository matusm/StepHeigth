using System.Collections.Generic;
using System.Linq;

namespace StepHeight
{
    public class FitStatistics
    {

        private FitVerticalStandard fvs;
        private List<double> h = new List<double>();
        private List<double> pt = new List<double>();
        private List<double> r = new List<double>();
        private List<double> res = new List<double>();

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

        public void Update()
        {
            h.Add(fvs.Height);
            pt.Add(fvs.Pt);
            res.Add(fvs.RangeOfResiduals);
            r.Add(fvs.A2Radius);
        }

        public void Restart()
        {
            h.Clear();
            pt.Clear();
            r.Clear();
            res.Clear();
        }
    }
}
