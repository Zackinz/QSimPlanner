﻿using QSP.LibraryExtension;
using System;

namespace QSP.RouteFinding.FileExport.Providers
{
    public static class JarDesignAirbusProvider
    {
        /// <summary>
        /// Get string of the flight plan to export.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static string GetExportText(ExportInput input)
        {
            var route = input.Route;
            if (route.Count < 2) throw new ArgumentException();
            var from = route.FirstWaypoint.ID.Substring(0, 4);
            var to = route.LastWaypoint.ID.Substring(0, 4);

            var str = route.ToString(true);
            if (str == "DCT") return $"{from} {to}";

            // Replace SID/STAR with DCT.
            var split = str.Split(' ');
            if (split.Length < 3) throw new ArgumentException();
            var middle =string.Join(" ", split.WithoutFirstAndLast().FormatWaypointIds());
            return $"{from} DCT {middle} DCT {to}";
        }
    }
}
