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

namespace SpaceEngineers.UWBlockPrograms.OxygenFarm
{
    public sealed class Program : MyGridProgram
    {
        //=======================================================================
        //////////////////////////BEGIN//////////////////////////////////////////
        //=======================================================================

        // Terminal Width is 80 Columns
        static Int32 TERMWIDTH = 80;
        static string CDATATAG = "oxygenfarm";
        static string DISPLAYNAME = "Oxygen Farm LCD";

        private List<IMyGasTank> storageTanks;
        private List<IMyOxygenFarm> oxygenFarms;

        // Constructor
        public Program()
        {
            // Update every 100 Ticks
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            // Initialize Oxygen Storage Tanks and Farms
            storageTanks = DiscoverStorageTanks(CDATATAG);
            oxygenFarms = DiscoverOxygenFarms();
        }

        // Program Entry Point
        public void Main(string args, UpdateType updateSource)
        {
            // Initialize Output Screen by name
            var displayScreen = GridTerminalSystem.GetBlockWithName(DISPLAYNAME) as IMyTextPanel;

            // Ghetto String Templating Galore
            var strout = new StringBuilder();

            // Storage Tank Details
            double totalCapacity = 0;
            double totalOxygen = 0;
           
            foreach(var tank in storageTanks)
            {
                totalCapacity += tank.Capacity;
                totalOxygen += (tank.Capacity * tank.FilledRatio);
            }

            double filledPercentage = totalOxygen * 100 / totalCapacity;

            strout.AppendLine(
                "==================================\n" +
                "      Oxygen Farm Monitor  v0.1a  \n" +
                "==================================\n\n" +
                "         Oxygen Storage: " + filledPercentage + "%\n  [" +
                Progress((Int32)filledPercentage, TERMWIDTH) + "]\n\n" +
                "Total Tanks: " + storageTanks.Count +"\n" +
                "Total Farms: " + oxygenFarms.Count + "\n"
                );

            // Flush output to display
            displayScreen.WritePublicText(strout.ToString());
        }

        // On Save Callback
        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means.

            // This method is optional and can be removed if not
            // needed.
        }

        private List<IMyGasTank> DiscoverStorageTanks(string cdatatag)
        {
            var discoveredTanks = new List<IMyGasTank>();
            GridTerminalSystem.GetBlocksOfType<IMyGasTank>(discoveredTanks);

            // Alright look let me level with you here, I have NO IDEA how to distinguish
            // between an oxygen and an hydrogen tank.
            // I have therefore decided to just wing the fuck out of it with the super Subtype ID
            return discoveredTanks.Where(
                t => t.CustomData == "oxygenfarm" &&
                     t.BlockDefinition.SubtypeId.Contains("Oxygen")
            ).ToList<IMyGasTank>();
        }

        private List<IMyOxygenFarm> DiscoverOxygenFarms()
        {
            var discoveredFarms = new List<IMyOxygenFarm>();
            GridTerminalSystem.GetBlocksOfType<IMyOxygenFarm>(discoveredFarms);

            return discoveredFarms;
        }

        // Fill and return Progress Bar
        private string Progress(int percent, int length)
        {
            char[] progressBar = new char[length];
            int completedFactor = Convert.ToInt32(length * percent / 100);

            for (int i = 0; i < (length-1); i++)
            {
                if(i <= completedFactor)
                {
                    progressBar[i] = '|';
                }
                else
                {
                    
                    progressBar[i] = (char)39;
                }
            }

            return new string(progressBar);
        }

//=======================================================================
//////////////////////////END////////////////////////////////////////////
//=======================================================================

    }
}