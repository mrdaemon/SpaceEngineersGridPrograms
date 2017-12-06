using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using VRageMath;
using VRage.Game;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;

namespace SpaceEngineers.UWBlockPrograms.BatteryMonitorEnterpriseEdition {
    public sealed class Program : MyGridProgram {

        // =====================================================================
        // -- BEGIN PROGRAM BLOCK --
        // =====================================================================

        // Included here for Reference and Hilarity
        // Author: François-Guy Gallant - https://github.com/DenkouNova

        int currentTick = 0;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            EnterpriseArrayMonitorFactory("Battery Monitor LCD");
        }

        public abstract class AbstractExtendedBattery {
            abstract public int CurrentInput { get; }
            abstract public int CurrentOutput { get; }
            abstract public int StoredPower { get; }
            abstract public int MaxStoredPower { get; }

            abstract public int NumberOfBatteries { get; }

            public int BatteryChargePercent { get { return (int)Math.Round((double)StoredPower * 100 / MaxStoredPower); } }

            public bool IsCharging { get { return CurrentInput > CurrentOutput; } }
            public bool IsDischarging { get { return CurrentInput < CurrentOutput; } }

            public int MinutesBeforeChargingOrDischarging {
                get {
                    return IsDischarging ? (-1 * StoredPower * 60) / (CurrentInput - CurrentOutput) :
                    IsCharging ? ((MaxStoredPower - StoredPower) * 60) / (CurrentInput - CurrentOutput) :
                    0;
                }
            }
        }

        public class ExtendedBattery : AbstractExtendedBattery {
            private IMyBatteryBlock Battery;

            public string DetailedInfo { get { return Battery.DetailedInfo; } }

            public override int CurrentInput { get { return GetNumberFromInfoString(GetInfoString("Current Input: ")); } }
            public override int CurrentOutput { get { return GetNumberFromInfoString(GetInfoString("Current Output: ")); } }
            public override int StoredPower { get { return GetNumberFromInfoString(GetInfoString("Stored power: ")); } }
            public override int MaxStoredPower { get { return GetNumberFromInfoString(GetInfoString("Max Stored Power: ")); } }

            public override int NumberOfBatteries { get { return 1; } }

            public ExtendedBattery(IMyBatteryBlock battery) { Battery = battery; }

            private int GetNumberFromInfoString(string infoString)
            {
                var numberString = infoString.Substring(0, infoString.IndexOf(" "));
                double baseNumber = Convert.ToDouble(numberString);
                int multiplier = GetMultiplier(infoString);
                return (int)(baseNumber * multiplier);
            }

            private string GetInfoString(string inputString)
            {
                var batteryInfos = Battery.DetailedInfo.Split('\n').ToList();
                return batteryInfos.FirstOrDefault(x => x.IndexOf(inputString) >= 0).Substring(inputString.Length);
            }

            private int GetMultiplier(string input)
            {
                if (input.Contains("GW")) return 1000000000;
                if (input.Contains("MW")) return 1000000;
                if (input.Contains("kW")) return 1000;
                return 1;
            }
        }

        public class ExtendedBatteryArray : AbstractExtendedBattery {
            private List<ExtendedBattery> BatteryArray;

            public override int CurrentInput { get { return BatteryArray.Sum(x => x.CurrentInput); } }
            public override int CurrentOutput { get { return BatteryArray.Sum(x => x.CurrentOutput); } }
            public override int StoredPower { get { return BatteryArray.Sum(x => x.StoredPower); } }
            public override int MaxStoredPower { get { return BatteryArray.Sum(x => x.MaxStoredPower); } }
            public override int NumberOfBatteries { get { return BatteryArray.Count; } }

            public ExtendedBatteryArray(List<ExtendedBattery> batteryArray)
            {
                BatteryArray = batteryArray;
            }
        }

        private List<ExtendedBattery> GetBatteries()
        {
            var batteryArray = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteryArray);
            var extendedBatteries = new List<ExtendedBattery>();
            batteryArray.ForEach(x => extendedBatteries.Add(new ExtendedBattery(x)));
            return extendedBatteries;
        }

        private string ProgressBar(int percent, int length)
        {
            char[] fillBar = new char[length];
            int percentFactor = Convert.ToInt32(length * percent / 100);
            for (int i = 0; i < 79; i++) {
                if (i <= percentFactor) {
                    fillBar[i] = '|';
                } else {
                    fillBar[i] = (char)39;
                }
            }
            return new String(fillBar);
        }

        private string GetTimeString(int minutes)
        {
            if (minutes < 60) return minutes + " min";
            if (minutes < 1440) return (minutes / 60) + " hours " + GetTimeString(minutes % 60);
            return (minutes / 1440) + "days " + GetTimeString(minutes % 1440);
        }

        private string GetQuantityString(int quantity, string unit)
        {
            if (quantity > 1000000000) return String.Format("{0:0.00}", (decimal)quantity / 1000000000) + " G" + unit;
            if (quantity > 1000000) return String.Format("{0:0.00}", (decimal)quantity / 1000000) + " M" + unit;
            if (quantity > 1000) return String.Format("{0:0.00}", (decimal)quantity / 1000) + " k" + unit;
            return String.Format("{0:0.00}", quantity) + " " + unit;
        }

        public void EnterpriseArrayMonitorFactory(string displayName)
        {
            var batteries = this.GetBatteries();

            StringBuilder sb = new StringBuilder();

            var batteryArray = new ExtendedBatteryArray(this.GetBatteries());

            sb.AppendLine(
                "==================================\n" +
                "   B2EE v1.001.1: Array Status    \n" +
                "==================================\n\n" +
                "                   Battery Charge: " + batteryArray.BatteryChargePercent + "%\n    [" +
                ProgressBar(batteryArray.BatteryChargePercent, length: 80) + "]\n\n " +
                "Total batteries: " + batteryArray.NumberOfBatteries);

            sb.AppendLine(
                batteryArray.IsDischarging ? " Fully discharged in: " +
                    GetTimeString(batteryArray.MinutesBeforeChargingOrDischarging) :
                batteryArray.IsCharging ? " Fully charged in: " +
                    GetTimeString(batteryArray.MinutesBeforeChargingOrDischarging) :
                "No movement");

            sb.AppendLine("\n" +
                " Current charge: " + GetQuantityString(batteryArray.StoredPower, "Wh") + "\n" +
                " Maximum capacity: " + GetQuantityString(batteryArray.MaxStoredPower, "Wh") + "\n");

            var chargingDirectionString =
                batteryArray.IsDischarging ? "<  <  <  <  <  <  <  <  <  <  <  <  <  <  <  <  <" :
                batteryArray.IsCharging ? ">  >  >  >  >  >  >  >  >  >  >  >  >  >  >  >  >" :
                "==================================";

            if (batteryArray.IsDischarging) {
                chargingDirectionString = "   ".Substring(currentTick % 3) + chargingDirectionString;
            } else if (batteryArray.IsCharging) {
                chargingDirectionString = "   ".Substring(2 - (currentTick % 3)) + chargingDirectionString;
            }

            sb.AppendLine(
                chargingDirectionString + "\n" +
                "    Input: " + GetQuantityString(batteryArray.CurrentInput, "W") + "      " +
                "    Output: " + GetQuantityString(batteryArray.CurrentOutput, "W") + "\n" +
                " " + chargingDirectionString);

            currentTick++;
            if (currentTick == Int32.MaxValue) currentTick = 0;

            // Update display   
            var displayScreen = GridTerminalSystem.GetBlockWithName(displayName) as IMyTextPanel;
            displayScreen.WritePublicText(sb.ToString());
        }

        // public void Save()
        // {
        //    // Called when the program needs to save its state. Use
        //    // this method to save your state to the Storage field
        //    // or some other means.
        //
        //    // This method is optional and can be removed if not
        //    // needed.
        // }

        // =====================================================================
        // -- END PROGRAM BLOCK --
        // =====================================================================
    }
}