using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveDAG.tests.TestHelpers
{
    public static class Helpers
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

        public static async Task<List<Post>> ProcessUserPosts(UserPosts userPosts)
        {
            var result = await Task.Run(() =>
            {
                var posts = userPosts.Posts
                    .Select(p => new Post { Title = p.Title.ToUpper(), Content = p.Content })
                    .ToList();
                return posts;
            });

            return result;
        }

        public static async Task<AdditionalData> FetchAdditionalDataAsync(List<Post> processedPosts)
        {
            return await Task.Run(() =>
            {
                return new AdditionalData { Processed = true, PostCount = processedPosts.Count };
            });
        }
        public static FinalResult AggregateResults(UserDetails userDetails, UserPosts userPosts, List<Post> processedPosts, AdditionalData additionalData)
        {
            return new FinalResult
            {
                UserId = userDetails.UserId,
                PostCount = processedPosts.Count,
                AdditionalDataProcessed = additionalData.Processed
            };
        }
    }
}
