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

namespace SpaceEngineers.UWBlockPrograms.BatteryMonitor
{
    public sealed class Program : MyGridProgram
    {
 
        //=======================================================================
        //////////////////////////BEGIN//////////////////////////////////////////
        //=======================================================================

        public Program()
        {
            // Set update tickrate
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string args)
        {
            string displayName = "Battery Monitor LCD";

            // Extend to non-local grids?                    
            bool doNonLocalGrids = false;

            var displayScreen = GridTerminalSystem.GetBlockWithName(displayName) as IMyTextPanel;
            bool noBatteries = false;

            var batteryArray = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteryArray);

            // Battery Details
            int maxStoredInt = 0;
            int currentInputInt = 0;
            int currentOutputInt = 0;
            int currentStoredInt = 0;
            int batteryTotal = 0;

            for (int j = 0; j < batteryArray.Count; j++)
            {
                if (doNonLocalGrids | batteryArray[j].CubeGrid == displayScreen.CubeGrid)
                {
                    string batteryDetailTemp = batteryArray[j].DetailedInfo;
                    string[] batteryDetail = batteryDetailTemp.Split('\n');

                    for (int i = 3; i < 7; i++)
                    {
                        char[] tempCharArray = batteryDetail[i].ToCharArray();
                        if (i == 3)
                        {
                            batteryTotal++;
                            string maxStored = "";
                            foreach (char ch in tempCharArray)
                            {
                                if (char.IsDigit(ch))
                                {
                                    maxStored = maxStored + ch.ToString();
                                }
                            }
                            if (batteryDetail[i].Contains("MW"))
                            {
                                maxStored += "0000";
                            }
                            else if (batteryDetail[i].Contains("kW"))
                            {
                                maxStored += "0";
                            }
                            maxStoredInt += Int32.Parse(maxStored);
                        }
                        else if (i == 4)
                        {
                            string currentInput = "";
                            foreach (char ch in tempCharArray)
                            {
                                if (char.IsDigit(ch))
                                {
                                    currentInput = currentInput + ch.ToString();
                                }
                            }
                            if (batteryDetail[i].Contains("MW"))
                            {
                                currentInput += "0000";
                            }
                            else if (batteryDetail[i].Contains("kW"))
                            {
                                currentInput += "0";
                            }
                            currentInputInt += Int32.Parse(currentInput);
                        }
                        else if (i == 5)
                        {
                            string currentOutput = "";
                            foreach (char ch in tempCharArray)
                            {
                                if (char.IsDigit(ch))
                                {
                                    currentOutput = currentOutput + ch.ToString();
                                }
                            }

                            if (batteryDetail[i].Contains("MW"))
                            {
                                currentOutput += "0000";
                            }
                            else if (batteryDetail[i].Contains("kW"))
                            {
                                currentOutput += "0";
                            }
                            currentOutputInt += Int32.Parse(currentOutput);
                        }
                        else
                        {
                            string currentStored = "";

                            foreach (char ch in tempCharArray)
                            {
                                if (char.IsDigit(ch))
                                {
                                    currentStored = currentStored + ch.ToString();
                                }
                            }

                            if (batteryDetail[i].Contains("MW"))
                            {
                                currentStored += "0000";
                            }
                            else if (batteryDetail[i].Contains("kW"))
                            {
                                currentStored += "0";
                            }
                            currentStoredInt += Int32.Parse(currentStored);
                        }
                    }
                }
                else if (j == batteryArray.Count - 1 & batteryTotal == 0)
                {
                    noBatteries = true;
                }
            }

            // Get battery percentage
            double percentFilled = 0;
            if (!noBatteries)
            {
                percentFilled = (double)currentStoredInt / maxStoredInt;
            }

            // Progress Bar (80 columns)	  
            char[] fillBar = new char[80];
            int percentFactor = Convert.ToInt32(80 * percentFilled);
            for (int i = 0; i < 79; i++)
            {
                if (i <= percentFactor)
                {
                    fillBar[i] = '|';
                }
                else
                {
                    fillBar[i] = (char)39;
                }
            }

            // Gnarly Spaghetti of alien code that converts Wh to W. Painfully inaccurate and shitty.
            // TODO: Fix one day
            string inputConverted = "";
            string outputConverted = "";
            string currentConverted = "";
            string maxConverted = "";

            if (currentInputInt >= 1000000)
            {
                inputConverted = (currentInputInt / 1000000).ToString() + " MW";
            }
            else if (currentInputInt >= 1000)
            {
                inputConverted = (currentInputInt / 1000).ToString() + " KW";
            }
            else
            {
                inputConverted = currentInputInt.ToString() + " W";
            }

            if (currentOutputInt >= 1000000)
            {
                outputConverted = (currentOutputInt / 1000000).ToString() + " MW";
            }
            else if (currentOutputInt >= 1000)
            {
                outputConverted = (currentOutputInt / 1000).ToString() + " KW";
            }
            else
            {
                outputConverted = currentOutputInt.ToString() + " W";
            }

            if (currentStoredInt >= 1000000)
            {
                currentConverted = (currentStoredInt / 1000000).ToString() + " MW";
            }
            else if (currentStoredInt >= 1000)
            {
                currentConverted = (currentStoredInt / 1000).ToString() + " KW";
            }
            else
            {
                currentConverted = currentStoredInt.ToString() + " W";
            }

            if (maxStoredInt >= 1000000)
            {
                maxConverted = (maxStoredInt / 1000000).ToString() + " MW";
            }
            else if (maxStoredInt >= 1000)
            {
                maxConverted = (maxStoredInt / 1000).ToString() + " KW";
            }
            else
            {
                maxConverted = maxStoredInt.ToString() + " W";
            }

            // Battery Status: charging or discharging?	  
            string chargeDirection = "";
            string chargeStatement = "";
            if (percentFilled != 1 & batteryTotal > 0)
            {
                if (currentInputInt >= currentOutputInt)
                {
                    string timeString = "";
                    chargeDirection = ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>";
                    double timeBasic = Math.Round((double)(maxStoredInt - currentStoredInt) / (currentInputInt - currentOutputInt) * 60);
                    if (timeBasic >= 1440)
                    {
                        timeString = Math.Round(timeBasic / 1440).ToString() + " days";
                    }
                    else if (timeBasic >= 60)
                    {
                        timeString = Math.Round(timeBasic / 60).ToString() + " hours";
                    }
                    else if (timeBasic > 1)
                    {
                        timeString = timeBasic.ToString() + " minutes";
                    }
                    else
                    {
                        timeString = "<1 minute";
                    }
                    chargeStatement = "Array fully charged in: " + timeString;
                }
                else
                {
                    string timeString = "";
                    chargeDirection = "<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<";
                    double timeBasic = Math.Round((double)currentStoredInt / (currentOutputInt - currentInputInt) * 60);
                    if (timeBasic >= 1440)
                    {
                        timeString = Math.Round(timeBasic / 1440).ToString() + " days";
                    }
                    else if (timeBasic >= 60)
                    {
                        timeString = Math.Round(timeBasic / 60).ToString() + " hours";
                    }
                    else if (timeBasic > 1)
                    {
                        timeString = timeBasic.ToString() + " minutes";
                    }
                    else
                    {
                        timeString = "<1 minute";
                    }
                    chargeStatement = "Array fully discharged in: " + timeString;
                }
            }
            else
            {
                chargeDirection = "-----------------------------------------------------------";
                chargeStatement = "Array fully charged: Power drain minimal";
            }

            // Display ghetto string template
            string displayFinal =
        "==================================\n" +
        " Battery Array Monitor v2.0b: Array Status    \n" +
        "==================================\n\n" +
        "                   Battery Charge: " + Math.Round(percentFilled * 100) + "%\n    [" +
        new String(fillBar) + "]\n\n " +
        "Total batteries: " + batteryTotal.ToString() + "\n " +
        chargeStatement + "\n\n " +
        "Current charge: " + currentConverted + "\n " +
        "Maximum capacity: " + maxConverted + "\n\n" +
        chargeDirection + "\n" +
        "    Input: " + inputConverted + "                  Output: " + outputConverted + "\n" +
        chargeDirection;

            // Update display
            displayScreen.WritePublicText(displayFinal);
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

//=======================================================================
//////////////////////////END////////////////////////////////////////////
//=======================================================================

    }
}