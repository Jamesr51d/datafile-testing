
using FiftyOne.DeviceDetection.Hash.Engine.OnPremise.FlowElements;
using FiftyOne.DeviceDetection;
using FiftyOne.Pipeline.Core.Data;
using FiftyOne.Pipeline.Core.FlowElements;
using FiftyOne.Pipeline.Engines;
using Microsoft.Extensions.Logging;
using LocalDetection;
using static LocalDetection.Program;
using FiftyOne.Pipeline.Engines.Data;

namespace LocalDetection
{ 
    public class BuildPipeline
    {
        public static IPipeline CreatePipeline(
            PerformanceProfiles performanceProfile,
            string dataFile,
            bool setPerf = false,
            bool setPred = true)
        {
            var loggerFactory = new LoggerFactory();
            var engine = new DeviceDetectionHashEngineBuilder(loggerFactory)
                 .SetDrift(0) // Should look for sub strings over a wider range.
                .SetPerformanceProfile(performanceProfile)
                .SetUsePredictiveGraph(setPerf)
                .SetUsePerformanceGraph(setPred)
                .SetAutoUpdate(false)
                .SetDataFileSystemWatcher(false)
                // .SetProperties()
                .Build(dataFile, false);
            return new PipelineBuilder(loggerFactory).AddFlowElement(engine).Build();
        }

        public static IFlowData CreateFlowData(IPipeline pipeline, string userAgent)
        {
            var data = pipeline.CreateFlowData();
            data.AddEvidence("header.user-agent", userAgent);
            return data;
        }

        public static Dictionary<string, string> ExtractProperties(string userAgent, IDeviceData output)
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
               
           /*    { "HardwareFamily", output.HardwareFamily.GetHumanReadable() },
               { "OEM", output.OEM.GetHumanReadable() }*/
            };
        }
    }
    public class UserAgentProcessor
    {
        public Dictionary<string, string> ProcessUserAgent(IPipeline pipeline, string userAgent)
        {
            using (var data = BuildPipeline.CreateFlowData(pipeline, userAgent))
            {
                data.Process();
                var output = data.Get<IDeviceData>();

                // Extract properties
                Dictionary<string, string> properties = BuildPipeline.ExtractProperties(userAgent, output);

                return properties;
            }
        }
    }
}
