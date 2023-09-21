using System.Diagnostics;
using OfficeOpenXml;
using FiftyOne.Pipeline.Core.FlowElements;
using FiftyOne.Pipeline.Engines;

// feed in list of profileIDS dfrom database 
// feed in Useragents 
// compare the two outputs on an excel sheet


namespace LocalDetection
{
    class Program
    {
        public static void Main()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // I/O
            string DatafileType = "Enterprise-HashV41.hash";
            string Datafile1 = @$"\\dpnas1\Production\daphne\hash\v4\2023\09\19\{DatafileType}";
            string Datafile2 = @$"\\dpnas1\Production\daphne\hash\v4\2023\09\21\{DatafileType}";

            string InputData = "Top1500Crawlers2";

            string OutputDestination = $@"C:\Users\jamesr\Documents\DatafileTesting\Output";

            // Filepath for Useragent Text file 
            string filePath = $"C:\\Users\\jamesr\\Documents\\DatafileTesting\\{InputData}.txt";
            string[] userAgents = File.ReadAllLines(filePath);

            //ask which test to run 
            Console.WriteLine("Which test to run:\n1. Output detection in text file for Useragents\n2. Performance Tests\n 3.Input/Output Useragent on Console:\nInput (1|2|3)");
            string whichTest = Console.ReadLine();

            string CompareDateFileDetection = "1";
            string PerformanceTest = "2";
            string OutputDetectionOnConsole = "3";
            string CompareDataFileDetectionToDataBase = "4";

            // Build Pipelines for different settings
            Dictionary<(PerformanceProfiles, string), IPipeline> pipelines = new Dictionary<(PerformanceProfiles, string), IPipeline>
                {
                    { (PerformanceProfiles.LowMemory, Datafile1), BuildPipeline.CreatePipeline(PerformanceProfiles.LowMemory, Datafile1) },
                    { (PerformanceProfiles.LowMemory, Datafile2), BuildPipeline.CreatePipeline(PerformanceProfiles.LowMemory, Datafile2) },
                    { (PerformanceProfiles.Balanced, Datafile1), BuildPipeline.CreatePipeline(PerformanceProfiles.Balanced, Datafile1) },
                    { (PerformanceProfiles.Balanced, Datafile2), BuildPipeline.CreatePipeline(PerformanceProfiles.Balanced, Datafile2) },
                    { (PerformanceProfiles.HighPerformance, Datafile1), BuildPipeline.CreatePipeline(PerformanceProfiles.HighPerformance, Datafile1) },
                    { (PerformanceProfiles.HighPerformance, Datafile2), BuildPipeline.CreatePipeline(PerformanceProfiles.HighPerformance, Datafile2) },
                    { (PerformanceProfiles.MaxPerformance, Datafile1), BuildPipeline.CreatePipeline(PerformanceProfiles.MaxPerformance, Datafile1) },
                    { (PerformanceProfiles.MaxPerformance, Datafile2), BuildPipeline.CreatePipeline(PerformanceProfiles.MaxPerformance, Datafile2) },
            };

            if (whichTest == CompareDateFileDetection)
            {
                BuildParameters parameters = new BuildParameters
                {
                    Datafile1 = Datafile1,
                    Datafile2 = Datafile2,
                    InputData = InputData,
                    UserAgents = userAgents,
                    OutputDest = OutputDestination,
                    Pipelines = pipelines
                };
                UserAgentProcessor userAgentProcessor = new UserAgentProcessor();
                CompareDetectionBetweenFiles(parameters, userAgentProcessor);
            }
            else if (whichTest == PerformanceTest)
            {
                BuildParameters parameters = new BuildParameters
                {
                    Datafile1 = Datafile1,
                    Datafile2 = Datafile2,
                    InputData = InputData,
                    UserAgents = userAgents,
                    OutputDest = OutputDestination,
                    Pipelines = pipelines
                };
                UserAgentProcessor userAgentProcessor = new UserAgentProcessor();
                RunPerformanceTest(parameters, userAgentProcessor);
            }
            else if (whichTest == OutputDetectionOnConsole)
            {
                BuildParameters parameters = new BuildParameters
                {
                    Datafile1 = Datafile1,
                    OutputDest = OutputDestination,
                    Pipelines = pipelines
                };
                UserAgentProcessor userAgentProcessor = new UserAgentProcessor();
                OutputDetectionOnConsoleForOneUserAgent(parameters, userAgentProcessor);
            }
            else if (whichTest == CompareDataFileDetectionToDataBase)
            {
                Console.WriteLine("Invalid input.");
            }
            else
            {
                Console.WriteLine("Invalid input.");
            }
        }

        public class BuildParameters
        {
            public string Datafile1 { get; set; }
            public string Datafile2 { get; set; }
            public string InputData { get; set; }
            public string[] UserAgents { get; set; }
            public string OutputDest { get; set; }
            public Dictionary<(PerformanceProfiles, string), IPipeline> Pipelines { get; set; }
        }



        private static void CompareDetectionBetweenFiles(BuildParameters parameters, UserAgentProcessor userAgentProcessor)
        {
            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Properties");

                int row = 1; // Start from the first row
                int col = 1; // Start from the first column

                // Headers
                worksheet.Cells[row, col].Value = "Property Name";
                worksheet.Cells[row, col + 1].Value = $"Live Datafile Value: {parameters.Datafile1}";
                worksheet.Cells[row, col + 2].Value = $"Pre-Prod Datafile Value: {parameters.Datafile2}";
                worksheet.Cells[row, col + 3].Value = "Are Returned Values the Same?";

                worksheet.Column(1).Width = 50.57;
                worksheet.Column(2).Width = 80.57;
                worksheet.Column(3).Width = 80.57;
                worksheet.Column(4).Width = 20.57;
                row++;

                foreach (string userAgent in parameters.UserAgents)
                {
                    // Process for Datafile1
                    Dictionary<string, string> properties1 = userAgentProcessor.ProcessUserAgent(parameters.Pipelines[(PerformanceProfiles.Balanced, parameters.Datafile1)], userAgent);
                    Dictionary<string, string> properties2 = userAgentProcessor.ProcessUserAgent(parameters.Pipelines[(PerformanceProfiles.Balanced, parameters.Datafile2)], userAgent);

                    foreach (var key in properties1.Keys)
                    {
                        worksheet.Cells[row, col].Value = key;
                        worksheet.Cells[row, col + 1].Value = properties1[key];
                        worksheet.Cells[row, col + 2].Value = properties2.ContainsKey(key) ? properties2[key] : "N/A";
                        if (key != "Evidence")
                        {
                            var same = (properties1[key] == properties2[key]);
                            if (same == false)
                            {
                                int t = 0;
                            }
                            string comparison = same ? "Same" : "NotSame";
                            worksheet.Cells[row, col + 3].Value = comparison;
                        }


                        row++;
                    }

                    row++; // Add an empty row between different user agents


                }
                string OutputDest1 = $"{parameters.OutputDest}-Detection-{parameters.InputData}.xlsx";
                package.Workbook.Calculate();

                package.SaveAs(new FileInfo(OutputDest1));
            }
        }

        private static void RunPerformanceTest(BuildParameters parameters, UserAgentProcessor userAgentProcessor)
        {
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
                        foreach (string userAgent in parameters.UserAgents)
                        {
                            userAgentProcessor.ProcessUserAgent(parameters.Pipelines[(profile, parameters.Datafile1)], userAgent);
                        }
                        stopwatch1.Stop();

                        // Measure performance for Datafile2
                        Stopwatch stopwatch2 = Stopwatch.StartNew();
                        foreach (string userAgent in parameters.UserAgents)
                        {
                            userAgentProcessor.ProcessUserAgent(parameters.Pipelines[(profile, parameters.Datafile2)], userAgent);
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
                }
                double overallAverage = totalPercentageChange / totalMeasurements;
                worksheet.Cells[row + 1, col + 3].Value = $"Overall Average: {overallAverage:F2}%";

                // Save the Excel file
                string OutputDest2 = $"{parameters.OutputDest}-Performance.xlsx";
                package.SaveAs(new FileInfo(OutputDest2));
            }
        }

        private static void OutputDetectionOnConsoleForOneUserAgent(BuildParameters parameters, UserAgentProcessor userAgentProcessor)
        {
            Console.WriteLine("Input UserAgent(s)");
            string input = Console.ReadLine();
            List<string> inputUserAgents = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            // Create FlowData and Get Properties 
            foreach (string userAgent in inputUserAgents)
            {
                Dictionary<string, string> properties = userAgentProcessor.ProcessUserAgent(parameters.Pipelines[(PerformanceProfiles.Balanced, parameters.Datafile1)], userAgent);
                Console.WriteLine("\nResults:\n");
                // Print properties to the console
                foreach (KeyValuePair<string, string> property in properties)
                {
                    Console.WriteLine($"{property.Key}: {property.Value}");
                }

            }
        }
    }
}