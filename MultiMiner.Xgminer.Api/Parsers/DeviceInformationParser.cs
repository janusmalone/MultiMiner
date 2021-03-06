﻿using MultiMiner.Xgminer.Api.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiMiner.Xgminer.Api.Parsers
{
    public class DeviceInformationParser : ResponseTextParser
    {
        public static void ParseTextForDeviceInformation(string text, List<DeviceInformation> deviceInformation, int logInterval)
        {
            List<string> deviceBlob = text.Split('|').ToList();
            deviceBlob.RemoveAt(0);

            foreach (string deviceText in deviceBlob)
            {
                if (deviceText == "\0")
                    continue;

                //bfgminer may have multiple entries for the same key, e.g. Hardware Errors
                //seen with customer data/hardware
                //remove dupes using Distinct()
                var deviceAttributes = deviceText.Split(',').ToList().Distinct();

                Dictionary<string, string> keyValuePairs = deviceAttributes
                  .Where(value => value.Contains('='))
                  .Select(value => value.Split('='))
                  .ToDictionary(pair => pair[0], pair => pair[1]);

                //seen Count == 0 with user API logs
                if (keyValuePairs.Count > 0)
                {
                    DeviceInformation newDevice = new DeviceInformation();

                    newDevice.Kind = keyValuePairs.ElementAt(0).Key;

                    newDevice.Index = TryToParseInt(keyValuePairs, newDevice.Kind, -1);
                    if (newDevice.Index == -1)
                        continue;
                    
                    if (keyValuePairs.ContainsKey("Enabled")) //seen this needed with a user
                        newDevice.Enabled = keyValuePairs["Enabled"].Equals("Y");

                    if (keyValuePairs.ContainsKey("Status")) //check required for bfgminer
                        newDevice.Status = keyValuePairs["Status"];

                    if (keyValuePairs.ContainsKey("Name"))
                        newDevice.Name = keyValuePairs["Name"];
                    else
                        //default to Kind for older RPC API versions
                        newDevice.Name = newDevice.Kind;

                    //default to Index for older RPC API versions
                    newDevice.ID = newDevice.Index;
                    if (keyValuePairs.ContainsKey("ID"))
                        newDevice.ID = TryToParseInt(keyValuePairs, "ID", newDevice.Index);

                    //parse regardless of device type = ASICs may have Temp
                    newDevice.Temperature = TryToParseDouble(keyValuePairs, "Temperature", 0.00);
                    newDevice.FanSpeed = TryToParseInt(keyValuePairs, "Fan Speed", 0);
                    newDevice.FanPercent = TryToParseInt(keyValuePairs, "Fan Percent", 0);
                    newDevice.GpuClock = TryToParseInt(keyValuePairs, "GPU Clock", 0);
                    newDevice.MemoryClock = TryToParseInt(keyValuePairs, "Memory Clock", 0);
                    newDevice.GpuVoltage = TryToParseDouble(keyValuePairs, "GPU Voltage", 0.00);
                    newDevice.GpuActivity = TryToParseInt(keyValuePairs, "GPU Activity", 0);
                    newDevice.PowerTune = TryToParseInt(keyValuePairs, "Powertune", 0);
                    if (keyValuePairs.ContainsKey("Intensity")) //check required for bfgminer 3.3.0
                        newDevice.Intensity = keyValuePairs["Intensity"];

                    newDevice.AverageHashrate = TryToParseDouble(keyValuePairs, "MHS av", 0.00) * 1000;

                    //seen both MHS 5s and MHS 1s
                    //the key here is based on the value passed for --log to bfgminer
                    newDevice.CurrentHashrate = GetCurrentHashrate(keyValuePairs, logInterval);

                    if (newDevice.CurrentHashrate == 0.0)
                        //check for 20s
                        newDevice.CurrentHashrate = GetCurrentHashrate(keyValuePairs, 20);

                    if (newDevice.CurrentHashrate == 0.0)
                        //check for 5s
                        newDevice.CurrentHashrate = GetCurrentHashrate(keyValuePairs, 5);
                    
                    newDevice.AcceptedShares = TryToParseInt(keyValuePairs, "Accepted", 0);                    
                    newDevice.RejectedShares = TryToParseInt(keyValuePairs, "Rejected", 0);                    
                    newDevice.HardwareErrors = TryToParseInt(keyValuePairs, "Hardware Errors", 0);
                    newDevice.Utility = TryToParseDouble(keyValuePairs, "Utility", 0.00);
                    newDevice.WorkUtility = TryToParseDouble(keyValuePairs, "Work Utility", 0.00);                
                    newDevice.PoolIndex = TryToParseInt(keyValuePairs, "Last Share Pool", -1);
                    newDevice.HardwareErrorsPercent = TryToParseDouble(keyValuePairs, "Device Hardware%", 0.00);
                    newDevice.RejectedSharesPercent = TryToParseDouble(keyValuePairs, "Device Rejected%", 0.00);
                    
                    newDevice.LastShareDifficulty = TryToParseDouble(keyValuePairs, "Last Share Difficulty", 0.00);
                    newDevice.DifficultyAccepted = TryToParseDouble(keyValuePairs, "Difficulty Accepted", 0.00);
                    newDevice.DeviceElapsed = TryToParseInt(keyValuePairs, "Device Elapsed", 0);
                    if (newDevice.WorkUtility == 0.0)
                    {
                        if (newDevice.DeviceElapsed > 0)
                            newDevice.WorkUtility = newDevice.DifficultyAccepted / newDevice.DeviceElapsed * 60;
                        else if (newDevice.LastShareDifficulty > 0)
                            newDevice.WorkUtility = newDevice.Utility / newDevice.LastShareDifficulty;
                    }

                    deviceInformation.Add(newDevice);
                }
            }
        }

        private static double GetCurrentHashrate(Dictionary<string, string> keyValuePairs, int logInterval)
        {
            string currentRateKey = String.Format("MHS {0}s", logInterval);
            if (keyValuePairs.ContainsKey(currentRateKey))
                return TryToParseDouble(keyValuePairs, currentRateKey, 0.00) * 1000;
            return 0.0;
        }
    }
}
