using System;
using Accord.MachineLearning.Clustering;

namespace SKhynix.TAS.UI.Report.Pccb.ReportMaker.Control.Chart.TSNEChart
{
    /// <summary>Accord.NET 3.8 Barnes-Hut t-SNE adapter.</summary>
    public sealed class TSNEProjectionModel
    {
        private readonly double[][] coordinates;

        private TSNEProjectionModel(double[][] coordinates, double effectivePerplexity)
        {
            this.coordinates = CloneMatrix(coordinates);
            EffectivePerplexity = effectivePerplexity;
        }

        public double[][] Coordinates { get { return CloneMatrix(coordinates); } }
        public double EffectivePerplexity { get; private set; }
        public int Iterations { get { return 0; } }
        public double LearningRate { get { return 0d; } }
        public int RandomSeed { get { return 0; } }
        public double KullbackLeiblerDivergence { get { return double.NaN; } }
        public string EngineName { get { return "Accord.NET TSNE (Barnes-Hut)"; } }

        public static TSNEProjectionModel FitTransform(double[][] standardizedMatrix, double perplexity, int iterations, double learningRate, int randomSeed)
        {
            ValidateMatrix(standardizedMatrix);
            int rowCount = standardizedMatrix.Length;
            double effectivePerplexity = Math.Max(1d, Math.Min(perplexity, Math.Max(1d, (rowCount - 1d) / 3d - 1e-6d)));
            var model = new TSNE
            {
                Perplexity = effectivePerplexity,
                Theta = 0.5d,
                NumberOfOutputs = 2
            };
            double[][] transformed = model.Transform(standardizedMatrix, CreateMatrix(rowCount, 2));
            return new TSNEProjectionModel(transformed, effectivePerplexity);
        }

        private static double[][] CreateMatrix(int rowCount, int columnCount)
        {
            var result = new double[rowCount][];
            for (int row = 0; row < rowCount; row++) result[row] = new double[columnCount];
            return result;
        }

        private static double[][] CloneMatrix(double[][] matrix)
        {
            if (matrix == null) return new double[0][];
            var result = new double[matrix.Length][];
            for (int row = 0; row < matrix.Length; row++) result[row] = matrix[row] == null ? new double[0] : (double[])matrix[row].Clone();
            return result;
        }

        private static void ValidateMatrix(double[][] matrix)
        {
            if (matrix == null || matrix.Length < 3 || matrix[0] == null || matrix[0].Length < 2)
                throw new ArgumentException("Accord.NET t-SNE requires at least three rows and two numeric features.", "standardizedMatrix");
            int columnCount = matrix[0].Length;
            for (int row = 0; row < matrix.Length; row++)
            {
                if (matrix[row] == null || matrix[row].Length != columnCount)
                    throw new ArgumentException("t-SNE input must be a rectangular matrix.", "standardizedMatrix");
                for (int column = 0; column < columnCount; column++)
                    if (double.IsNaN(matrix[row][column]) || double.IsInfinity(matrix[row][column]))
                        throw new ArgumentException("t-SNE input must contain finite values.", "standardizedMatrix");
            }
        }
    }
}





