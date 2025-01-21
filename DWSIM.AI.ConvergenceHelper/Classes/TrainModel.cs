using System;
using System.IO;
using Microsoft.ML;

namespace DWSIM.AI.ConvergenceHelper
{
    class Program
    {
        static readonly string _trainDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "taxi-fare-train.csv");
        static readonly string _testDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "taxi-fare-test.csv");
        static readonly string _modelPath = Path.Combine(Environment.CurrentDirectory, "Data", "Model.zip");
       
        static void Main(string[] args)
        {
            Console.WriteLine(Environment.CurrentDirectory);

            MLContext mlContext = new MLContext(seed: 0);

            var model = Train(mlContext, _trainDataPath);

            Evaluate(mlContext, model);

            TestSinglePrediction(mlContext, model);
        }

        public static ITransformer Train(MLContext mlContext, string dataPath)
        {
            IDataView dataView = mlContext.Data.LoadFromTextFile<ConvergenceHelperTrainingData>(dataPath, hasHeader: true, separatorChar: ',');
         
            var pipeline = mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: "FareAmount")
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "VendorIdEncoded", inputColumnName: "VendorId"))
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "RateCodeEncoded", inputColumnName: "RateCode"))
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "PaymentTypeEncoded", inputColumnName: "PaymentType"))
                    .Append(mlContext.Transforms.Concatenate("Features", "VendorIdEncoded", "RateCodeEncoded", "PassengerCount", "TripDistance", "PaymentTypeEncoded"))
                    .Append(mlContext.Regression.Trainers.OnlineGradientDescent());
     
            Console.WriteLine("=============== Create and Train the Model ===============");

            var model = pipeline.Fit(dataView);

            Console.WriteLine("=============== End of training ===============");
            Console.WriteLine();

            return model;
        }

        private static void Evaluate(MLContext mlContext, ITransformer model)
        {
           
            IDataView dataView = mlContext.Data.LoadFromTextFile<ConvergenceHelperTrainingData>(_testDataPath, hasHeader: true, separatorChar: ',');
          
            var predictions = model.Transform(dataView);
            var metrics = mlContext.Regression.Evaluate(predictions, "Label", "Score");
         
            Console.WriteLine();
            Console.WriteLine($"*************************************************");
            Console.WriteLine($"*       Model quality metrics evaluation         ");
            Console.WriteLine($"*------------------------------------------------");
            Console.WriteLine($"*       RSquared Score:      {metrics.RSquared:0.##}");
            Console.WriteLine($"*       Root Mean Squared Error:      {metrics.RootMeanSquaredError:#.##}");
            Console.WriteLine($"*************************************************");
        }

        private static void TestSinglePrediction(MLContext mlContext, ITransformer model)
        {
            //var predictionFunction = mlContext.Model.CreatePredictionEngine<ConvergenceHelperTrainingData, TaxiTripFarePrediction>(model);
                      
            //var prediction = predictionFunction.Predict(taxiTripSample);
           
            //Console.WriteLine($"**********************************************************************");
            //Console.WriteLine($"Predicted fare: {prediction.FareAmount:0.####}, actual fare: 15.5");
            //Console.WriteLine($"**********************************************************************");

        }
    }
}