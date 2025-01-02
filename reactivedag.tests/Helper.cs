using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveDAG.tests
{
    public static class Helper
    {
        public static double CalculateCovariance(List<double> x, List<double> y)
        {
            if (x.Count != y.Count || x.Count == 0)
                throw new ArgumentException("Datasets must not be empty and should be equal in length.");

            var xMean = x.Average();
            var yMean = y.Average();
            return x.Zip(y, (xi, yi) => (xi - xMean) * (yi - yMean)).Sum() / (x.Count - 1);
        }

        public static double CalculateVolatility(List<double> returns)
        {
            double averageReturn = returns.Average();
            double variance = returns.Sum(r => Math.Pow(r - averageReturn, 2)) / (returns.Count - 1);
            return Math.Sqrt(variance);
        }

        public static double GetZScoreForConfidenceLevel(double confidenceLevel, List<double> dataSet)
        {
            double mean = dataSet.Average();
            double sumOfSquares = dataSet.Sum(value => Math.Pow(value - mean, 2));
            double standardDeviation = Math.Sqrt(sumOfSquares / dataSet.Count);
            double zScore = (confidenceLevel - mean) / standardDeviation;
            return zScore;
        }
    }
}
