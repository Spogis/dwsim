using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.ML;

namespace DWSIM.AI.ConvergenceHelper
{
    public static class ModelTrainer
    {

        private static MLContext mlContext;
       
        public static void Initialize()
        {

            mlContext = new MLContext(seed: 0);

        }

        public static ITransformer Train(MLContext mlContext, List<ConvergenceHelperTrainingData> data)
        {

            IDataView dataView = mlContext.Data.LoadFromEnumerable(data);
         
            var pipeline = mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: "FareAmount")
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "VendorIdEncoded", inputColumnName: "VendorId"))
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "RateCodeEncoded", inputColumnName: "RateCode"))
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "PaymentTypeEncoded", inputColumnName: "PaymentType"))
                    .Append(mlContext.Transforms.Concatenate("Features", "VendorIdEncoded", "RateCodeEncoded", "PassengerCount", "TripDistance", "PaymentTypeEncoded"))
                    .Append(mlContext.Regression.Trainers.OnlineGradientDescent());
     
            var model = pipeline.Fit(dataView);

            return model;
        }

        public static void Evaluate(MLContext mlContext, ITransformer model, List<ConvergenceHelperTrainingData> data)
        {

            IDataView dataView = mlContext.Data.LoadFromEnumerable(data);

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

        public static void TestSinglePrediction(MLContext mlContext, ITransformer model)
        {
            //var predictionFunction = mlContext.Model.CreatePredictionEngine<ConvergenceHelperTrainingData, TaxiTripFarePrediction>(model);
                      
            //var prediction = predictionFunction.Predict(taxiTripSample);
           
            //Console.WriteLine($"**********************************************************************");
            //Console.WriteLine($"Predicted fare: {prediction.FareAmount:0.####}, actual fare: 15.5");
            //Console.WriteLine($"**********************************************************************");

        }
    }
}