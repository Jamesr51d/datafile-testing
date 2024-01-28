using System.Diagnostics;
using OfficeOpenXml;
using FiftyOne.Pipeline.Core.FlowElements;
using FiftyOne.Pipeline.Engines;
using System.Collections;
using System;
using FiftyOne.DeviceDetection;
using System.Text;
using Newtonsoft.Json;

namespace LocalDetection
{
    class Program
    {
        #region setup
        static readonly string _json;
        static Config _config;
        static DateTime _dt = DateTime.Now;

        public class Config
        {
            public string Hash1FilePath;
            public string Hash2FilePath;
            public string InputFilePath;
            public string OutPutFolder;
        }

        static Program()
        {
            _json = File.ReadAllText(".\\config.json");
            _config = JsonConvert.DeserializeObject<Config>(_json);
        }
        public enum Choices
        {
            CompareDetectionBetweenFiles = 1,
            CompareDetectionBetweenFilesFailed = 2,
            RunPerformanceTest = 3,
            RunPerformanceTestPerfVsPred = 4,
            OutputDetectionOnConsoleForOneUserAgent = 5,
            OutputProfileIDsExcel = 6
        }

        #endregion

        public static void Main()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // I/O
            int whichTest = ReadChoiceFromConsole();

            ChoiceOfTest(whichTest);
        }

        #region methods
        private static void ChoiceOfTest(
            int whichTest)
        {
            switch (whichTest)
            {
                case (int)Choices.CompareDetectionBetweenFiles:
                    Console.WriteLine("CompareDatafileDetection");
                    CompareDetectionBetweenFiles();
                    break;

                case (int)Choices.CompareDetectionBetweenFilesFailed:

                    Console.WriteLine("CompareDateFileDetectionDifference");
                        CompareDetectionBetweenFilesFailed();
                    break;

                case (int)Choices.RunPerformanceTest:
                    Console.WriteLine("PerformanceTest");
                        RunPerformanceTest();
                    break;

                case (int)Choices.RunPerformanceTestPerfVsPred:
                    Console.WriteLine("PerformanceTestPerfVsPred");
                        RunPerformanceTestPerfVsPred();
                    break;

                case (int)Choices.OutputDetectionOnConsoleForOneUserAgent:
                    Console.WriteLine("OutputDetectionOnConsole");
                        OutputDetectionOnConsoleForOneUserAgent();
                    break;

                case (int)Choices.OutputProfileIDsExcel:
                    Console.WriteLine("CompareDataFileDetectionToDataBase");
                    OutputProfileIDsExcel();
                    break;

                default:
                    Console.WriteLine("Invalid input.");
                    break;
            
            }
        }
    
        private static int ReadChoiceFromConsole()
        {
            Console.WriteLine("Which test to run:\n" +
                "1. Output detection in Excel file for Useragents\n" +
                "2. Output difference in detection in Excel file\n" +
                "3. Performance Test : Old Datafile Vs. New Datafile\n" +
                "4. Performance Test: Predictive Vs. Performance\n" +
                "5. Input/Output Useragent on Console:\n" +
                "6. Independent Accuracy Check. This requires a files with useragents like\n" +
                "'UseragentDIVIDER123456-123456-123456-123456'\n" +
                "Input number of test to run (1|2|3|4|5|6):");
            return Convert.ToInt32(Console.ReadLine());
        }

        private static IPipeline InstantiatePipeline(
            PerformanceProfiles performanceProfile,
            string Datafile,
            bool setPerf = false,
            bool setPred = true
            )
        {
            // Build Pipelines for different settings


            Console.WriteLine("Building a pipeline");
            return BuildPipeline.CreatePipeline(performanceProfile, Datafile, setPerf, setPred);
        }
        
        public static string FileFriendlyDatetime(DateTime dt)
        {
            return dt.ToString().Replace("/", "").Replace(" ", "").Replace(":", "");
        }

        private static Dictionary<string, string> UAwithProfileID(string[] UAs)
        {
            // userAgents is UA + DIVIDER + ProfileIDs
            List<string> cleanUserAgentsList = new();

            // ua key = ProfileIDs 
            Dictionary<string, string> uaToProfileID = new();

            int millions = 0;

            foreach (string value in UAs)
            {

                // UA + DIVIDER + ProfileID ->
                // [UA, ProfileID] -> 0 index = UA
                string[] uaSplit = value.Split("DIVIDER");

                if (uaSplit.Count() == 2)
                {

                    string ua = uaSplit[0];


                    string uaProfileID = uaSplit[1];

                    uaToProfileID[ua] = uaProfileID;

                    cleanUserAgentsList.Add(ua);
                    millions++;
                }

            }

            return uaToProfileID;
        }
        #endregion

        #region Actions

        private static void CompareDetectionBetweenFiles()
        {
            var test = _config.InputFilePath;
            var userAgentProcessor = new UserAgentProcessor();
            var pipelines = new List<IPipeline>()
                    {
                        InstantiatePipeline(PerformanceProfiles.LowMemory, _config.Hash1FilePath),
                        InstantiatePipeline(PerformanceProfiles.LowMemory, _config.Hash2FilePath),
                        InstantiatePipeline(PerformanceProfiles.Balanced, _config.Hash1FilePath),
                        InstantiatePipeline(PerformanceProfiles.Balanced, _config.Hash2FilePath),
                        InstantiatePipeline(PerformanceProfiles.HighPerformance, _config.Hash1FilePath),
                        InstantiatePipeline(PerformanceProfiles.HighPerformance, _config.Hash2FilePath),
                        InstantiatePipeline(PerformanceProfiles.MaxPerformance, _config.Hash1FilePath),
                        InstantiatePipeline(PerformanceProfiles.MaxPerformance, _config.Hash2FilePath)
                    };
            Console.WriteLine("Built pipelines");
            string[] Uas = File.ReadAllLines(_config.InputFilePath);
            Console.WriteLine("Done Reading Input file");

            using (ExcelPackage package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Properties");

                int row = 1; // Start from the first row
                int col = 1; // Start from the first column

                // Headers
                worksheet.Cells[row, col].Value = "Property Name";
                worksheet.Cells[row, col + 1].Value = $"Live Datafile Value: {_config.Hash1FilePath}";
                worksheet.Cells[row, col + 2].Value = $"Pre-Prod Datafile Value: {_config.Hash2FilePath}";
                worksheet.Cells[row, col + 3].Value = "Are Returned Values the Same?";

                worksheet.Column(1).Width = 20;
                worksheet.Column(2).Width = 80;
                worksheet.Column(3).Width = 80;
                worksheet.Column(4).Width = 10;
                row++;

                // Loop over Useragents 
                foreach (string userAgent in Uas)
                {
                    // process for each pipeline 
                    for (int i = 0; i < pipelines.Count - 1; i += 2)
                    {
                        Dictionary<string, string> properties1 =
                            userAgentProcessor.ProcessUserAgent(pipelines[i],
                            userAgent);
                        Dictionary<string, string> properties2 =
                           userAgentProcessor.ProcessUserAgent(
                               pipelines[i + 1], userAgent);


                        foreach (var key in properties1.Keys)
                        {
                            worksheet.Cells[row, col].Value = key;
                            worksheet.Cells[row, col + 1].Value = properties1[key];
                            worksheet.Cells[row, col + 2].Value = properties2.ContainsKey(key) ? properties2[key] : "N/A";
                            if (key != "Evidence")
                            {
                                var same = (properties1[key] == properties2[key]);
                                string comparison = same ? "Same" : "NotSame";
                                worksheet.Cells[row, col + 3].Value = comparison;
                            }
                            row++;
                        }
                    }
                }
                string OutputName = $"{Choices.CompareDetectionBetweenFiles}-{FileFriendlyDatetime(_dt)}.xlsx";
                var outputDest = new FileInfo(_config.OutPutFolder + OutputName);
                package.Workbook.Calculate();

                package.SaveAs(outputDest);
                Console.WriteLine($"Saved file to {outputDest.FullName}");
            }
        }

        private static void CompareDetectionBetweenFilesFailed()
        {
            var userAgentProcessor = new UserAgentProcessor();
            var pipelines = new List<IPipeline>()
                    {
                        InstantiatePipeline(PerformanceProfiles.LowMemory, _config.Hash1FilePath),
                        InstantiatePipeline(PerformanceProfiles.LowMemory, _config.Hash2FilePath),
                        InstantiatePipeline(PerformanceProfiles.Balanced, _config.Hash1FilePath),
                        InstantiatePipeline(PerformanceProfiles.Balanced, _config.Hash2FilePath),
                        InstantiatePipeline(PerformanceProfiles.HighPerformance, _config.Hash1FilePath),
                        InstantiatePipeline(PerformanceProfiles.HighPerformance, _config.Hash2FilePath),
                        InstantiatePipeline(PerformanceProfiles.MaxPerformance, _config.Hash1FilePath),
                        InstantiatePipeline(PerformanceProfiles.MaxPerformance, _config.Hash2FilePath)
                    };
            Console.WriteLine("Built pipelines");
            string[] Uas = File.ReadAllLines(_config.InputFilePath);
            Console.WriteLine("Done Reading Input File");

            using (ExcelPackage package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Properties");

                int row = 1; // Start from the first row
                int col = 1; // Start from the first column

                // Headers
                worksheet.Cells[row, col].Value = "Property Name";
                worksheet.Cells[row, col + 1].Value = $"Live Datafile Value: {_config.Hash1FilePath}";
                worksheet.Cells[row, col + 2].Value = $"Pre-Prod Datafile Value: {_config.Hash2FilePath}";
                worksheet.Cells[row, col + 3].Value = "Are Returned Values the Same?";

                worksheet.Column(1).Width = 20;
                worksheet.Column(2).Width = 80;
                worksheet.Column(3).Width = 80;
                worksheet.Column(4).Width = 10;
                row++;
                int UAsDetected = 0;
                int differentDetection = 0;
                int totalUAS = Uas.Count();


                foreach (string userAgent in Uas)
                {
                    // process for each pipeline 
                    for (int i = 0; i < pipelines.Count - 1; i += 2)
                    {
                        Dictionary<string, string> properties1 =
                            userAgentProcessor.ProcessUserAgent(pipelines[i],
                            userAgent);
                        Dictionary<string, string> properties2 =
                           userAgentProcessor.ProcessUserAgent(
                               pipelines[i + 1], userAgent);



                        StringBuilder sb1 = new StringBuilder();
                        StringBuilder sb2 = new StringBuilder();

                        foreach (var key in properties1.Keys)
                        {
                            if (key == "UserAgent" || key == "ProfileIDs")
                            {
                                continue;
                            }
                            sb1.Append(properties1[key]).Append("-");
                            sb2.Append(properties2.ContainsKey(key) ? properties2[key] : "N/A").Append("-");
                        }

                        string detection1 = sb1.ToString();
                        string detection2 = sb2.ToString();

                        var same = (detection1 == detection2);

                        string comparison = same ? "Same" : "NotSame";
                        if (!same)
                        {
                            worksheet.Cells[row, col].Value = userAgent;
                            worksheet.Cells[row, col + 1].Value = detection1;
                            worksheet.Cells[row, col + 2].Value = detection2;
                            worksheet.Cells[row, col + 3].Value = comparison;

                            differentDetection++;
                            row++;
                        }

                        if (UAsDetected % 10000 == 0)
                        {
                            Console.WriteLine(UAsDetected + " of " + totalUAS);
                        }
                        UAsDetected++;
                    }
                }
                float percentageDetectionDifferent = (float)differentDetection / (float)totalUAS * 100;
                worksheet.Cells[row, col + 2].Value = "Percentage Different:";
                worksheet.Cells[row, col + 3].Value = percentageDetectionDifferent;
                worksheet.Cells[row, col + 4].Value = "Different:";
                worksheet.Cells[row, col + 5].Value = differentDetection;
                worksheet.Cells[row, col + 6].Value = "Total:";
                worksheet.Cells[row, col + 7].Value = totalUAS;
                string OutputName = $"{Choices.CompareDetectionBetweenFilesFailed}-{FileFriendlyDatetime(_dt)}.xlsx";
                package.Workbook.Calculate();
                var outputDest = new FileInfo(_config.OutPutFolder + OutputName);
                package.SaveAs(outputDest);
                Console.WriteLine($"Saved file to {outputDest}");
            }
        }

        private static void RunPerformanceTestPerfVsPred()
        {
            var userAgentProcessor = new UserAgentProcessor();
            var pipelines = new List<IPipeline>()
                    {
                        InstantiatePipeline(PerformanceProfiles.LowMemory, _config.Hash1FilePath),
                        InstantiatePipeline(PerformanceProfiles.LowMemory, _config.Hash2FilePath),
                        InstantiatePipeline(PerformanceProfiles.Balanced, _config.Hash1FilePath),
                        InstantiatePipeline(PerformanceProfiles.Balanced, _config.Hash2FilePath),
                        InstantiatePipeline(PerformanceProfiles.HighPerformance, _config.Hash1FilePath),
                        InstantiatePipeline(PerformanceProfiles.HighPerformance, _config.Hash2FilePath),
                        InstantiatePipeline(PerformanceProfiles.MaxPerformance, _config.Hash1FilePath),
                        InstantiatePipeline(PerformanceProfiles.MaxPerformance, _config.Hash2FilePath)
                    };
            Console.WriteLine("Built pipelines");
            string[] Uas = File.ReadAllLines(_config.InputFilePath);
            Console.WriteLine("Done Reading Input File");

            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Performance Results");

                int row = 1; // Start from the first row
                int col = 1; // Start from the first column

                // Headers
                worksheet.Cells[row, col].Value = "Performance Profile";
                worksheet.Cells[row, col + 1].Value = "Perf Time (s)";
                worksheet.Cells[row, col + 2].Value = "Pred Time (s)";
                row++;
                worksheet.Column(1).Width = 18.57;
                worksheet.Column(1).Style.Font.Bold = true;
                worksheet.Column(1).Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Column(1).Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);


                worksheet.Row(1).Style.Font.Bold = true;
                worksheet.Row(1).Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Row(1).Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                double totalPercentageChange = 0;
                int totalMeasurements = 0;

            

                    for (int i = 0; i < 30; i++)
                    {
                        // Measure speed for Performance
                        Stopwatch stopwatch1 = Stopwatch.StartNew();

                        foreach (string userAgent in Uas)
                        {
                        userAgentProcessor.ProcessUserAgent(pipelines[0], userAgent);
                        }

                        stopwatch1.Stop();

                        // Measure speed for Predictive
                        Stopwatch stopwatch2 = Stopwatch.StartNew();
                        foreach (string userAgent in Uas)
                        {
                        userAgentProcessor.ProcessUserAgent(pipelines[1], userAgent);
                        }
                        stopwatch2.Stop();
                        double oldValue = stopwatch1.Elapsed.TotalSeconds;
                        double newValue = stopwatch2.Elapsed.TotalSeconds;
                        double percentageChange = ((newValue - oldValue) / oldValue) * 100;
                        worksheet.Cells[row, col + 1].Value = $"{stopwatch1.Elapsed.TotalSeconds:F2}";
                        worksheet.Cells[row, col + 2].Value = $"{stopwatch2.Elapsed.TotalSeconds:F2}";
                        worksheet.Cells[row, col + 3].Value = $"{percentageChange:F2}";
                        totalPercentageChange += percentageChange;
                        totalMeasurements++;
                        row++;
                    }


                double overallAverage = totalPercentageChange / totalMeasurements;
                worksheet.Cells[row + 1, col + 3].Value = $"Overall Average: {overallAverage:F2}%";

                // Save the Excel file
                string OutputName = $"{Choices.RunPerformanceTestPerfVsPred}-{FileFriendlyDatetime(_dt)}.xlsx";
                var outputDest = new FileInfo(_config.OutPutFolder + OutputName);
                package.SaveAs(outputDest);
                Console.WriteLine($"Saved file to {outputDest}");
            }
        }

        private static void RunPerformanceTest()
        {
            var userAgentProcessor = new UserAgentProcessor();
            string[] Uas = File.ReadAllLines(_config.InputFilePath);
            var pipeline1 = InstantiatePipeline(
                PerformanceProfiles.HighPerformance,
                _config.Hash2FilePath);

            var pipeline2 = InstantiatePipeline(
                PerformanceProfiles.HighPerformance,
                _config.Hash2FilePath);
            Console.WriteLine("Built pipelines");
            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Performance Results");

                int row = 1; // Start from the first row
                int col = 1; // Start from the first column

                // Headers
                worksheet.Cells[row, col].Value = "Performance Profile";
                worksheet.Cells[row, col + 1].Value = "Datafile1 Time (s)";
                worksheet.Cells[row, col + 2].Value = "Datafile2 Time (s)";
                row++;
                worksheet.Column(1).Width = 18.57;
                worksheet.Column(1).Style.Font.Bold = true;
                worksheet.Column(1).Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Column(1).Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);


                worksheet.Row(1).Style.Font.Bold = true;
                worksheet.Row(1).Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Row(1).Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);


                double totalPercentageChange = 0;
                int totalMeasurements = 0;

                foreach (PerformanceProfiles profile in Enum.GetValues(typeof(PerformanceProfiles)))
                {
                    Console.WriteLine($"Starting: {profile}");

                    if (profile == PerformanceProfiles.BalancedTemp)
                    {
                        continue;
                    }
                    worksheet.Cells[row, col].Value = $"{profile}";
                    row++;

                    for (int i = 0; i < 30; i++)
                    {
                        // Measure performance for Datafile1
                        Stopwatch stopwatch1 = Stopwatch.StartNew();
                        foreach (string userAgent in Uas)
                        {
                            userAgentProcessor.ProcessUserAgent(pipeline1, userAgent);
                        }
                        stopwatch1.Stop();

                        // Measure performance for Datafile2
                        Stopwatch stopwatch2 = Stopwatch.StartNew();
                        foreach (string userAgent in Uas)
                        {
                            userAgentProcessor.ProcessUserAgent(pipeline2, userAgent);
                        }
                        stopwatch2.Stop();
                        double oldValue = stopwatch1.Elapsed.TotalSeconds;
                        double newValue = stopwatch2.Elapsed.TotalSeconds;
                        double percentageChange = ((newValue - oldValue) / oldValue) * 100;
                        worksheet.Cells[row, col + 1].Value = $"{stopwatch1.Elapsed.TotalSeconds:F2}";
                        worksheet.Cells[row, col + 2].Value = $"{stopwatch2.Elapsed.TotalSeconds:F2}";
                        worksheet.Cells[row, col + 3].Value = $"{percentageChange:F2}";
                        totalPercentageChange += percentageChange;
                        totalMeasurements++;
                        row++;
                    }
                    Console.WriteLine($"Completed: {profile}");
                }
                double overallAverage = totalPercentageChange / totalMeasurements;
                worksheet.Cells[row + 1, col + 3].Value = $"Overall Average: {overallAverage:F2}%";

                // Save the Excel file
                string OutputName = $"{Choices.RunPerformanceTest}-{FileFriendlyDatetime(_dt)}.xlsx";
                var outputDest = new FileInfo(_config.OutPutFolder + OutputName);
                package.SaveAs(outputDest);
                Console.WriteLine($"Saved file to {outputDest}");
            }
        }

        private static void OutputDetectionOnConsoleForOneUserAgent()
        {
            var userAgentProcessor = new UserAgentProcessor();
            var pipeline = InstantiatePipeline(
                PerformanceProfiles.HighPerformance,
                _config.Hash2FilePath);
            Console.WriteLine("Input UserAgent(s)");
            List<string> inputUserAgents = new List<string>();
            while (true)
            {
                string input = Console.ReadLine();
                if (string.IsNullOrEmpty(input)) break;
                inputUserAgents.Add(input);
            }

            // Create FlowData and Get Properties 
            foreach (string userAgent in inputUserAgents)
            {
                Dictionary<string, string> properties = userAgentProcessor.ProcessUserAgent(pipeline, userAgent);
                Console.WriteLine("\nResults:\n");
                // Print properties to the console
                foreach (KeyValuePair<string, string> property in properties)
                {
                    Console.WriteLine($"{property.Key}: {property.Value}");
                }
            }
        }
       
        private static void OutputProfileIDsExcel()
        {
            var userAgentProcessor = new UserAgentProcessor();
            string[] Uas = File.ReadAllLines(_config.InputFilePath);
            var pipeline = InstantiatePipeline(
                PerformanceProfiles.HighPerformance,
                _config.Hash2FilePath);

            Dictionary<string, string> uaToProfileID = UAwithProfileID(Uas);

            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Properties");

                int row = 1; // Start from the first row
                int col = 1; // Start from the first column

                // Headers
                worksheet.Cells[row, col].Value = "User-Agent";
                worksheet.Cells[row, col + 1].Value = "Detect Profile ID";
                worksheet.Cells[row, col + 2].Value = "Data Profile ID";
                worksheet.Cells[row, col + 3].Value = "IsSame";

                worksheet.Column(1).Width = 80.57;
                worksheet.Column(2).Width = 50.57;
                worksheet.Column(3).Width = 50.57;
                worksheet.Column(4).Width = 50.57;

                row++;

                int failures = 0;
                int processed = 0;

                Console.WriteLine("\nProcessing:\n");

                foreach (string userAgent in uaToProfileID.Keys)
                {
                    Dictionary<string, string> properties =
                    userAgentProcessor.ProcessUserAgent(pipeline, userAgent);

                    var same = (properties["ProfileIDs"] == uaToProfileID[userAgent]);
                    string comparison = same ? "Same" : "NotSame";

                    if (!same)
                    { 
                        worksheet.Cells[row, col].Value = userAgent;
                        worksheet.Cells[row, col+1].Value = properties["ProfileIDs"];
                        worksheet.Cells[row, col + 2].Value = uaToProfileID[userAgent];

                        worksheet.Cells[row, col + 3].Value = comparison;

                        row++;
                        failures++;
                    }

                    processed++;

                    if(processed % 10000 == 0)
                    {
                        Console.WriteLine("Processed: " + processed);
                    }
                }

                worksheet.Cells[row, col].Value = "Pass Rate";
                worksheet.Cells[row, col+1].Value = failures;
                worksheet.Cells[row, col + 2].Value = Uas.Count();
                worksheet.Cells[row, col + 3].Value = (1 - failures / (float) Uas.Count())  * 100;

                string OutputName = $"{Choices.OutputProfileIDsExcel}-{FileFriendlyDatetime(_dt)}.xlsx";
                var outputDest = new FileInfo(_config.OutPutFolder + OutputName);
                package.Workbook.Calculate();

                Console.WriteLine("\nSaving:\n");

                package.SaveAs(outputDest);
                Console.WriteLine($"Saved file to {outputDest}");
            }
        }
        #endregion
    }
}