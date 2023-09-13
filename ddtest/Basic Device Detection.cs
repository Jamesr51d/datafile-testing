using System;
using System.Collections.Generic;
using System.IO;
//using ConsoleApp;
using FiftyOne.DeviceDetection;
using FiftyOne.DeviceDetection.Cloud.FlowElements;
using FiftyOne.Pipeline.Core.Data;
using FiftyOne.Pipeline.Engines.Data;


            string newFile = @"C:\Users\jamesr\Downloads\NEW 08052023TAC-HashV41.hash";
            string CurrentFile = @"C:\Users\jamesr\Downloads\08052023TAC-HashV41.hash";
            string aprilFile = @"C:\Users\jamesr\Downloads\10-04 TAC-HashV41.hash";

            var pipeline = new DeviceDetectionPipelineBuilder()
             .UseOnPremise(CurrentFile, false)
             .SetAutoUpdate(false)
             .SetDataFileSystemWatcher(false)
             .Build();


            string userAgent = $"Mozilla/5.0 (Linux; Android 11; A509DL Build/RP1A.200720.011; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/113.0.5672.77 Mobile Safari/537.36";

using (var data = pipeline.CreateFlowData())
{
   
    data.AddEvidence("header.user-agent", userAgent);
    data.Process();
    var output = data.Get<IDeviceData>();

    // string row = $"{output.HardwareVendor.GetHumanReadable()},{output.HardwareModel.GetHumanReadable()},{output.HardwareName.GetHumanReadable()},{output.PlatformVendor.GetHumanReadable()},{output.PlatformName.GetHumanReadable()},{output.PlatformVersion.GetHumanReadable()},{output.BrowserVendor.GetHumanReadable()},{output.BrowserName.GetHumanReadable()},{output.BrowserVersion.GetHumanReadable()},{output.HardwareFamily.GetHumanReadable()},{output.OEM.GetHumanReadable()}";
    Console.WriteLine("UserAgent        :       " + userAgent);
    Console.WriteLine("HardwareVendor:      " + output.HardwareVendor.GetHumanReadable());
    Console.WriteLine("HardwareName:        " + output.HardwareName.GetHumanReadable());
    Console.WriteLine("HardwareModel:       " + output.HardwareModel.GetHumanReadable());
    Console.WriteLine("PlatformVendor:      " + output.PlatformVendor.GetHumanReadable());
    Console.WriteLine("PlatformName:        " + output.PlatformName.GetHumanReadable());
    Console.WriteLine("PlatformVersion:     " + output.PlatformVersion.GetHumanReadable());
    Console.WriteLine("BrowserVendor:       " + output.BrowserVendor.GetHumanReadable());
    Console.WriteLine("BrowserName:     " + output.BrowserName.GetHumanReadable());
    Console.WriteLine("BrowserVersion:      " + output.BrowserVersion.GetHumanReadable());
    Console.WriteLine("HardwareFamily:      " + output.HardwareFamily.GetHumanReadable());
    Console.WriteLine("OEM:" + output.OEM.GetHumanReadable());
    //Console.WriteLine(row);

}



public static class Extensions
    {
        public static string GetHumanReadable(this IAspectPropertyValue<string> apv)
        {
            return apv.HasValue ? apv.Value : $"Unknown ({apv.NoValueMessage})";
        }
        public static string GetHumanReadable(this IAspectPropertyValue<IReadOnlyList<string>> apv)
        {
            return apv.HasValue ? string.Join(", ", apv.Value) : $"Unknown ({apv.NoValueMessage})";
        }
        public static string GetHumanReadable(this IAspectPropertyValue<int> apv)
        {
            return apv.HasValue ? apv.Value.ToString() : $"Unknown ({apv.NoValueMessage})";
        }
    }

