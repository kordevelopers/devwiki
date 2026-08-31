using System;
using System.Linq;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Chart.ManualPcaScatter
{
    /// <summary>
    /// 외부 수학 라이브러리 없이 exact t-SNE를 계산한다.
    /// 고차원 유사도는 perplexity 기반 Gaussian 확률로, 2차원 유사도는 Student-t 분포로 계산한다.
    /// </summary>
    public sealed class TsneProjectionModel
    {
        private readonly double[][] coordinates;

        private TsneProjectionModel(double[][] coordinates, double effectivePerplexity, int iterations, double learningRate, int randomSeed, double divergence)
        {
            this.coordinates = CloneMatrix(coordinates);
            EffectivePerplexity = effectivePerplexity;
            Iterations = iterations;
            LearningRate = learningRate;
            RandomSeed = randomSeed;
            KullbackLeiblerDivergence = divergence;
        }

        public double[][] Coordinates { get { return CloneMatrix(coordinates); } }
        public double EffectivePerplexity { get; private set; }
        public int Iterations { get; private set; }
        public double LearningRate { get; private set; }
        public int RandomSeed { get; private set; }
        public double KullbackLeiblerDivergence { get; private set; }

        public static TsneProjectionModel FitTransform(double[][] standardizedMatrix, double perplexity, int iterations, double learningRate, int randomSeed)
        {
            ValidateMatrix(standardizedMatrix);
            int rowCount = standardizedMatrix.Length;
            double effectivePerplexity = Math.Max(1d, Math.Min(perplexity, Math.Max(1d, (rowCount - 1d) / 3d)));
            int safeIterations = Math.Max(250, iterations);
            double safeLearningRate = double.IsNaN(learningRate) || double.IsInfinity(learningRate) || learningRate <= 0d ? 200d : learningRate;

            double[,] probabilities = BuildJointProbabilities(standardizedMatrix, effectivePerplexity);
            double[][] embedding = CreateInitialEmbedding(rowCount, randomSeed);
            double[][] velocity = CreateMatrix(rowCount, 2);
            double[][] gains = CreateMatrix(rowCount, 2);
            double[][] previousGradient = CreateMatrix(rowCount, 2);
            for (int row = 0; row < rowCount; row++)
            {
                gains[row][0] = 1d;
                gains[row][1] = 1d;
            }

            for (int iteration = 0; iteration < safeIterations; iteration++)
            {
                double exaggeration = iteration < 250 ? 12d : 1d;
                double momentum = iteration < 250 ? 0.5d : 0.8d;
                double[][] gradient = CalculateGradient(embedding, probabilities, exaggeration);
                for (int row = 0; row < rowCount; row++)
                {
                    for (int axis = 0; axis < 2; axis++)
                    {
                        bool changedDirection = Math.Sign(gradient[row][axis]) != Math.Sign(previousGradient[row][axis]);
                        gains[row][axis] = changedDirection ? gains[row][axis] + 0.2d : gains[row][axis] * 0.8d;
                        gains[row][axis] = Math.Max(0.01d, gains[row][axis]);
                        velocity[row][axis] = (momentum * velocity[row][axis])
                            - (safeLearningRate * gains[row][axis] * gradient[row][axis]);
                        embedding[row][axis] += velocity[row][axis];
                        previousGradient[row][axis] = gradient[row][axis];
                    }
                }

                Recenter(embedding);
            }

            return new TsneProjectionModel(
                embedding,
                effectivePerplexity,
                safeIterations,
                safeLearningRate,
                randomSeed,
                CalculateDivergence(embedding, probabilities));
        }

        private static double[,] BuildJointProbabilities(double[][] matrix, double perplexity)
        {
            int rowCount = matrix.Length;
            double[,] squaredDistances = new double[rowCount, rowCount];
            for (int left = 0; left < rowCount; left++)
            {
                for (int right = left + 1; right < rowCount; right++)
                {
                    double distance = 0d;
                    for (int feature = 0; feature < matrix[left].Length; feature++)
                    {
                        double difference = matrix[left][feature] - matrix[right][feature];
                        distance += difference * difference;
                    }

                    squaredDistances[left, right] = distance;
                    squaredDistances[right, left] = distance;
                }
            }

            double[,] conditional = new double[rowCount, rowCount];
            double targetEntropy = Math.Log(perplexity);
            for (int row = 0; row < rowCount; row++)
            {
                double beta = 1d;
                double betaMinimum = double.NegativeInfinity;
                double betaMaximum = double.PositiveInfinity;
                for (int search = 0; search < 60; search++)
                {
                    double sum = 0d;
                    double weightedDistance = 0d;
                    for (int other = 0; other < rowCount; other++)
                    {
                        if (other == row)
                        {
                            conditional[row, other] = 0d;
                            continue;
                        }

                        double value = Math.Exp(-squaredDistances[row, other] * beta);
                        conditional[row, other] = value;
                        sum += value;
                        weightedDistance += squaredDistances[row, other] * value;
                    }

                    sum = Math.Max(sum, 1e-300d);
                    double difference = Math.Log(sum) + (beta * weightedDistance / sum) - targetEntropy;
                    if (Math.Abs(difference) <= 1e-5d)
                    {
                        break;
                    }

                    if (difference > 0d)
                    {
                        betaMinimum = beta;
                        beta = double.IsPositiveInfinity(betaMaximum) ? beta * 2d : (beta + betaMaximum) / 2d;
                    }
                    else
                    {
                        betaMaximum = beta;
                        beta = double.IsNegativeInfinity(betaMinimum) ? beta / 2d : (beta + betaMinimum) / 2d;
                    }
                }

                double rowSum = 0d;
                for (int other = 0; other < rowCount; other++)
                {
                    rowSum += conditional[row, other];
                }

                rowSum = Math.Max(rowSum, 1e-300d);
                for (int other = 0; other < rowCount; other++)
                {
                    conditional[row, other] /= rowSum;
                }
            }

            double[,] joint = new double[rowCount, rowCount];
            double divisor = 2d * rowCount;
            for (int left = 0; left < rowCount; left++)
            {
                for (int right = left + 1; right < rowCount; right++)
                {
                    double probability = Math.Max((conditional[left, right] + conditional[right, left]) / divisor, 1e-12d);
                    joint[left, right] = probability;
                    joint[right, left] = probability;
                }
            }

            return joint;
        }

        private static double[][] CalculateGradient(double[][] embedding, double[,] probabilities, double exaggeration)
        {
            int rowCount = embedding.Length;
            double[,] numerators = BuildStudentTNumerators(embedding, out double denominator);
            double[][] gradient = CreateMatrix(rowCount, 2);
            for (int left = 0; left < rowCount; left++)
            {
                for (int right = 0; right < rowCount; right++)
                {
                    if (left == right)
                    {
                        continue;
                    }

                    double numerator = numerators[left, right];
                    double q = Math.Max(numerator / denominator, 1e-12d);
                    double multiplier = 4d * ((exaggeration * probabilities[left, right]) - q) * numerator;
                    gradient[left][0] += multiplier * (embedding[left][0] - embedding[right][0]);
                    gradient[left][1] += multiplier * (embedding[left][1] - embedding[right][1]);
                }
            }

            return gradient;
        }

        private static double CalculateDivergence(double[][] embedding, double[,] probabilities)
        {
            int rowCount = embedding.Length;
            double[,] numerators = BuildStudentTNumerators(embedding, out double denominator);
            double divergence = 0d;
            for (int left = 0; left < rowCount; left++)
            {
                for (int right = 0; right < rowCount; right++)
                {
                    if (left == right)
                    {
                        continue;
                    }

                    double p = Math.Max(probabilities[left, right], 1e-12d);
                    double q = Math.Max(numerators[left, right] / denominator, 1e-12d);
                    divergence += p * Math.Log(p / q);
                }
            }

            return divergence;
        }

        private static double[,] BuildStudentTNumerators(double[][] embedding, out double denominator)
        {
            int rowCount = embedding.Length;
            double[,] numerators = new double[rowCount, rowCount];
            denominator = 0d;
            for (int left = 0; left < rowCount; left++)
            {
                for (int right = left + 1; right < rowCount; right++)
                {
                    double dx = embedding[left][0] - embedding[right][0];
                    double dy = embedding[left][1] - embedding[right][1];
                    double numerator = 1d / (1d + (dx * dx) + (dy * dy));
                    numerators[left, right] = numerator;
                    numerators[right, left] = numerator;
                    denominator += 2d * numerator;
                }
            }

            denominator = Math.Max(denominator, 1e-12d);
            return numerators;
        }

        private static double[][] CreateInitialEmbedding(int rowCount, int randomSeed)
        {
            var random = new Random(randomSeed);
            double[][] result = CreateMatrix(rowCount, 2);
            for (int row = 0; row < rowCount; row++)
            {
                double first = Math.Max(1e-12d, 1d - random.NextDouble());
                double second = 1d - random.NextDouble();
                double radius = Math.Sqrt(-2d * Math.Log(first)) * 1e-4d;
                result[row][0] = radius * Math.Cos(2d * Math.PI * second);
                result[row][1] = radius * Math.Sin(2d * Math.PI * second);
            }

            return result;
        }

        private static void Recenter(double[][] matrix)
        {
            double meanX = matrix.Average(row => row[0]);
            double meanY = matrix.Average(row => row[1]);
            foreach (double[] row in matrix)
            {
                row[0] -= meanX;
                row[1] -= meanY;
            }
        }

        private static double[][] CreateMatrix(int rowCount, int columnCount)
        {
            var result = new double[rowCount][];
            for (int row = 0; row < rowCount; row++)
            {
                result[row] = new double[columnCount];
            }

            return result;
        }

        private static double[][] CloneMatrix(double[][] matrix)
        {
            return matrix == null
                ? new double[0][]
                : matrix.Select(row => row == null ? new double[0] : (double[])row.Clone()).ToArray();
        }

        private static void ValidateMatrix(double[][] matrix)
        {
            if (matrix == null || matrix.Length < 3 || matrix[0] == null || matrix[0].Length < 2)
            {
                throw new ArgumentException("t-SNE requires at least three rows and two numeric features.", "standardizedMatrix");
            }

            int columnCount = matrix[0].Length;
            if (matrix.Any(row => row == null
                || row.Length != columnCount
                || row.Any(value => double.IsNaN(value) || double.IsInfinity(value))))
            {
                throw new ArgumentException("t-SNE input must be a rectangular matrix containing finite values.", "standardizedMatrix");
            }
        }
    }
}
