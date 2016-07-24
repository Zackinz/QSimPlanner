﻿using QSP.RouteFinding.Airports;
using QSP.RouteFinding.AirwayStructure;
using System;
using System.IO;
using QSP.LibraryExtension;

namespace QSP.RouteFinding.TerminalProcedures.Sid
{
    public static class SidHandlerFactory
    {
        /// <exception cref="LoadSidFileException"></exception>
        public static SidHandler GetHandler(
            string icao,
            string navDataLocation,
            WaypointList wptList,
            WaypointListEditor editor,
            AirportManager airportList)
        {
            string fileLocation = navDataLocation + "\\PROC\\" + icao + ".txt";

            try
            {
                string allTxt = File.ReadAllText(fileLocation);
                return new SidHandler(icao, allTxt, wptList, editor, airportList);
            }
            catch (Exception ex)
            {
                throw new LoadSidFileException(
                    "Failed to read " + fileLocation + ".", ex);
            }
        }

        internal static object GetHandler(string icao, object navDataLocation, Locator<WaypointList> wptListLocator, object p, Locator<AirportManager> airportListLocator)
        {
            throw new NotImplementedException();
        }
    }
}
