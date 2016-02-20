﻿using System;
using static QSP.AviationTools.Coordinates.Utilities;
using static QSP.LibraryExtension.Arrays;
using static QSP.LibraryExtension.StringParser.Utilities;

namespace QSP.RouteFinding.RouteAnalyzers
{
    // Split an string representing a route into an array of strings, which is then processed by various 
    // route analyzers.
    //
    // This is mainly used to convert all supported lat/lon formats to decimal format.
    //
    // 1. Input: The string, consisting of waypoint(WPT), airway(AWY), etc.
    //           These should be seperated by at least one of the following char/strings:
    //           (1) space
    //           (2) Tab
    //           (3) LF
    //           (4) CR
    //
    // 2. Not case-sensitive to input string. They get converted to upper case before parsing.
    //
    // 3. Format:
    //    (1) Any AWY can be replaced by DCT. The route will be a direct between the two waypoints.
    //    (2) Any DCT can be omitted.

    public class CoordinateFormatter
    {
        private string route;

        public CoordinateFormatter(string route)
        {
            this.route = route;
        }

        public string[] Split()
        {
            var s = route.ToUpper()
                         .Split(DelimiterWords, StringSplitOptions.RemoveEmptyEntries)
                         .RemoveElements("DCT");

            ConvertTo5LetterFormat(s);
            return s;
        }
    }
}