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
        
        public static double[,] MatrixMultiply(double[,] A, double[,] B)
        {
            int aRows = A.GetLength(0);
            int aCols = A.GetLength(1);
            int bRows = B.GetLength(0);
            int bCols = B.GetLength(1);
            
            if (aCols != bRows)
                throw new ArgumentException("Matrix dimensions do not match for multiplication");
                
            var result = new double[aRows, bCols];
            
            for (int i = 0; i < aRows; i++)
            {
                for (int j = 0; j < bCols; j++)
                {
                    result[i, j] = 0;
                    for (int k = 0; k < aCols; k++)
                    {
                        result[i, j] += A[i, k] * B[k, j];
                    }
                }
            }
            
            return result;
        }

        public static double[] MatrixVectorMultiply(double[,] A, double[] v)
        {
            int aRows = A.GetLength(0);
            int aCols = A.GetLength(1);
            
            if (aCols != v.Length)
                throw new ArgumentException("Matrix and vector dimensions do not match for multiplication");
                
            var result = new double[aRows];
            
            for (int i = 0; i < aRows; i++)
            {
                result[i] = 0;
                for (int j = 0; j < aCols; j++)
                {
                    result[i] += A[i, j] * v[j];
                }
            }            
            return result;
        }
        

        public static double DotProduct(double[] a, double[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vector dimensions do not match for dot product");
                
            double sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                sum += a[i] * b[i];
            }
            
            return sum;
        }        
     

        public static double Determinant3x3(double[,] A)
        {
            if (A.GetLength(0) < 3 || A.GetLength(1) < 3)
                throw new ArgumentException("Matrix must be at least 3x3");
                
            double a = A[0, 0];
            double b = A[0, 1];
            double c = A[0, 2];
            double d = A[1, 0];
            double e = A[1, 1];
            double f = A[1, 2];
            double g = A[2, 0];
            double h = A[2, 1];
            double i = A[2, 2];
            
            return a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g);
        }
    }
}
