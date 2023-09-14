using System;
using System.Collections.Generic;
using System.Diagnostics;
using OfficeOpenXml;
using System.IO;
using FiftyOne.DeviceDetection;
using FiftyOne.DeviceDetection.Cloud.FlowElements;
using FiftyOne.DeviceDetection.Hash.Engine.OnPremise.FlowElements;
using FiftyOne.Pipeline.Core.Data;
using FiftyOne.Pipeline.Core.Exceptions;
using FiftyOne.Pipeline.Core.FlowElements;
using FiftyOne.Pipeline.Engines;
using FiftyOne.Pipeline.Engines.Data;
using Microsoft.Extensions.Logging;

namespace DeviceDetection
{
    class Program
    {
        public static void Main()
        {
            // DATAFILES
            string Datafile1 = @"\\dpnas1\Production\daphne\hash\v4\2023\09\14\Enterprise-HashV41.hash";
            string Datafile2 = @"\\xnas1\Test\james\pearl.daphne\pre-prod\daphne\hash\v4\2023\09\14\Enterprise-HashV41.hash";
            // string Datafile2 = @"\\XNAS1\Test\james\pearl.daphne\20230904b\daphne\hash\v4\2023\09\03\Enterprise-HashV41.hash";
            // string Datafile = @"\\dpnas1\Production\daphne\hash\v4\2023\09\04\Enterprise-HashV41.hash";

            string filenameNew = "Top1500Crawlers";

            // Filepath for Useragent Text file 
            string filePath = $"C:\\Users\\jamesr\\Documents\\DatafileTesting\\{filenameNew}.txt";
 
            string[] userAgents = File.ReadAllLines(filePath);

            // Filepath for Outputted Text file
            string OutputDest = $@"C:\Users\jamesr\Documents\DatafileTesting\Output";
           // string OutputDest2 = $@"C:\Users\jamesr\Documents\DatafileTesting\Detection-{filenameNew}.csv";

            //ask which test to run 
            Console.WriteLine("Which test to run:\n1. Output detection in text file for Useragents\n2. Performance Tests\n 3.Input/Output Useragent on Console:\nInput (1|2|3)");
            string whichTest = Console.ReadLine();

            // Build Pipelines for different settings
            Dictionary<(PerformanceProfiles, string), IPipeline> pipelines = new Dictionary<(PerformanceProfiles, string), IPipeline>
                {
                    { (PerformanceProfiles.LowMemory, Datafile1), CreatePipeline(PerformanceProfiles.LowMemory, Datafile1) },
                    { (PerformanceProfiles.LowMemory, Datafile2), CreatePipeline(PerformanceProfiles.LowMemory, Datafile2) },
                    { (PerformanceProfiles.Balanced, Datafile1), CreatePipeline(PerformanceProfiles.Balanced, Datafile1) },
                    { (PerformanceProfiles.Balanced, Datafile2), CreatePipeline(PerformanceProfiles.Balanced, Datafile2) },
                    { (PerformanceProfiles.HighPerformance, Datafile1), CreatePipeline(PerformanceProfiles.HighPerformance, Datafile1) },
                    { (PerformanceProfiles.HighPerformance, Datafile2), CreatePipeline(PerformanceProfiles.HighPerformance, Datafile2) },
                    { (PerformanceProfiles.MaxPerformance, Datafile1), CreatePipeline(PerformanceProfiles.MaxPerformance, Datafile1) },
                    { (PerformanceProfiles.MaxPerformance, Datafile2), CreatePipeline(PerformanceProfiles.MaxPerformance, Datafile2) },
            };

            if (whichTest == "1")
            {
                using (ExcelPackage package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Properties");

                    int row = 1; // Start from the first row
                    int col = 1; // Start from the first column

                    // Headers
                    worksheet.Cells[row, col].Value = "Property Name";
                    worksheet.Cells[row, col + 1].Value = "Live Datafile Value";
                    worksheet.Cells[row, col + 2].Value = "Pre-Prod Datafile Value";
                    worksheet.Cells[row, col + 3].Value = "Are Returned Values the Same?";

                    worksheet.Column(2).Width = 30.57;
                    worksheet.Column(2).Width = 40.57;
                    worksheet.Column(3).Width = 40.57;
                    worksheet.Column(4).Width = 20.57;
                    row++;

                    foreach (string userAgent in userAgents)
                    {
                        // Process for Datafile1
                        Dictionary<string, string> properties1 = ProcessUserAgent(pipelines[(PerformanceProfiles.Balanced, Datafile1)], userAgent);

                        // Process for Datafile2
                        Dictionary<string, string> properties2 = ProcessUserAgent(pipelines[(PerformanceProfiles.Balanced, Datafile2)], userAgent);

                        foreach (var key in properties1.Keys)
                        {
                            worksheet.Cells[row, col].Value = key;
                            worksheet.Cells[row, col + 1].Value = properties1[key];
                            worksheet.Cells[row, col + 2].Value = properties2.ContainsKey(key) ? properties2[key] : "N/A";
                            if(key != "Evidence")
                            {
                               string comparison = (properties1[key] == properties2[key]) ? "Same" : "NotSame";
                               worksheet.Cells[row, col + 3].Value = comparison;
                            }
                           
                            
                            row++;
                        }

                        row++; // Add an empty row between different user agents


                    }
                    string OutputDest1 = $"{OutputDest}-Detection-{filenameNew}.xlsx";
                    package.Workbook.Calculate();

                    package.SaveAs(new FileInfo(OutputDest1));
                }
            }


            else if (whichTest == "2")
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
                            foreach (string userAgent in userAgents)
                            {
                                ProcessUserAgent(pipelines[(profile, Datafile1)], userAgent);
                            }
                            stopwatch1.Stop();

                            // Measure performance for Datafile2
                            Stopwatch stopwatch2 = Stopwatch.StartNew();
                            foreach (string userAgent in userAgents)
                            {
                                ProcessUserAgent(pipelines[(profile, Datafile2)], userAgent);
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

                       // Add an empty row between different profiles
                    }
                    double overallAverage = totalPercentageChange / totalMeasurements;
                    worksheet.Cells[row + 1, col + 3].Value = $"Overall Average: {overallAverage:F2}%";

                    // Save the Excel file
                    string OutputDest2 = $"{OutputDest}-Performance.xlsx";
                    package.SaveAs(new FileInfo(OutputDest2));
                }
            }

            else if (whichTest == "3")
            {
                Console.WriteLine("Input UserAgent(s)");
                string input = Console.ReadLine();
                List<string> inputUserAgents = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                // Create FlowData and Get Properties 
                foreach (string userAgent in inputUserAgents)
                {
                    Dictionary<string, string> properties = ProcessUserAgent(pipelines[(PerformanceProfiles.Balanced, Datafile1)], userAgent);
                    Console.WriteLine("\nResults:\n");
                    // Print properties to the console
                    foreach (KeyValuePair<string, string> property in properties)
                    {
                        Console.WriteLine($"{property.Key}: {property.Value}");
                    }

                }
            }

            else
            {
                Console.WriteLine("Invalid input.");
            }
        }

        private static Dictionary<string, string> ProcessUserAgent(IPipeline pipeline, string userAgent)
        {
            using (var data = CreateFlowData(pipeline, userAgent))
            {
                data.Process();
                var output = data.Get<IDeviceData>();

                // Extract properties
                Dictionary<string, string> properties = ExtractProperties(userAgent, output);

                return properties;
            }
        }

        private static IPipeline CreatePipeline(PerformanceProfiles performanceProfile, string dataFile)
        {
            var loggerFactory = new LoggerFactory();
            var engine = new DeviceDetectionHashEngineBuilder(loggerFactory)
               // .SetDrift(100) // Should look for sub strings over a wider range.
                .SetPerformanceProfile(performanceProfile)
                .SetUsePredictiveGraph(true)
                .SetUsePerformanceGraph(false)
                .SetAutoUpdate(false)
                .SetDataFileSystemWatcher(false)
                .Build(dataFile, false);
            return new PipelineBuilder(loggerFactory).AddFlowElement(engine).Build();
            //return new DeviceDetectionPipelineBuilder()
            //    .UseOnPremise(dataFile, false)
            //    .SetPerformanceProfile(performanceProfile)
            //   .SetUsePredictiveGraph(true)
            //   .SetUsePerformanceGraph(false)
            //    .SetShareUsage(false)
            //    .SetAutoUpdate(false)
            //    .SetDataUpdateOnStartUp(false)
            //    .SetDataFileSystemWatcher(false)
            //    .Build();
        }

        private static IFlowData CreateFlowData(IPipeline pipeline, string userAgent)
        {
            var data = pipeline.CreateFlowData();
            data.AddEvidence("header.user-agent", userAgent);
            return data;
        }

        private static Dictionary<string, string> ExtractProperties(string userAgent, IDeviceData output)
        {
            return new Dictionary<string, string>
    {
        { "UserAgent", userAgent },
        { "HardwareVendor", output.HardwareVendor.GetHumanReadable() },
        { "HardwareName", output.HardwareName.GetHumanReadable() },
        { "HardwareModel", output.HardwareModel.GetHumanReadable() },
        { "PlatformVendor", output.PlatformVendor.GetHumanReadable() },
        { "PlatformName", output.PlatformName.GetHumanReadable() },
        { "PlatformVersion", output.PlatformVersion.GetHumanReadable() },
        { "BrowserVendor", output.BrowserVendor.GetHumanReadable() },
        { "BrowserName", output.BrowserName.GetHumanReadable() },
        { "BrowserVersion", output.BrowserVersion.GetHumanReadable() },
        { "IsCrawler", output.IsCrawler.GetHumanReadable() },
        { "CrawlerName", output.CrawlerName.GetHumanReadable() },
        { "ProfileIDs", output.DeviceId.GetHumanReadable() },
        { "Evidence", output.UserAgents.GetHumanReadable() },
        // Uncomment these if needed
        // { "HardwareFamily", output.HardwareFamily.GetHumanReadable() },
        // { "OEM", output.OEM.GetHumanReadable() }
    };
        }


        private static void WritePropertiesToFile(string property, string filePath)
        {
            using (var textWriter = new StreamWriter(filePath, true))
            {
                textWriter.WriteLine(property);
            }
        }


        private static void WritePropertiesToCsv(List<string> properties, string filePath)
        {
            using (var textWriter = new StreamWriter(filePath, true))
            {
                foreach (var property in properties)
                {
                    textWriter.WriteLine(FormatForCsv(property));
                }
            }
        }

        private static string FormatForCsv(string value)
        {
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
    }
}

public static class Extensions
{
    public static string GetHumanReadable(this IAspectPropertyValue<string> apv)
    {
        return apv.HasValue ? apv.Value : $"Unknown ({apv.NoValueMessage})";
    }



    public static string GetHumanReadable(this IAspectPropertyValue<bool> apv)
    {
        return apv.HasValue ? apv.Value.ToString() : $"Unknown ({apv.NoValueMessage})";
    }



    public static string GetHumanReadable(this IAspectPropertyValue<IReadOnlyList<string>> apv)
    {
        return apv.HasValue ? string.Join("| ", apv.Value) : $"Unknown ({apv.NoValueMessage})";
    }



    public static string GetHumanReadable(this IAspectPropertyValue<int> apv)
    {
        return apv.HasValue ? apv.Value.ToString() : $"Unknown ({apv.NoValueMessage})";
    }
}