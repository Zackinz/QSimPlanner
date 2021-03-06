﻿using NUnit.Framework;
using QSP.RouteFinding.Tracks.Pacots.Eastbound;
using System.Linq;

namespace UnitTest.RouteFinding.Tracks.Pacots.Eastbound
{
    [TestFixture]
    public class InterpreterTest
    {
        private string[] flexRoute = new string[] {
            "KALNA", "42N160E", "45N170E", "47N180E", "49N170W", "50N160W",
            "51N150W", "51N140W", "ORNAI" };

        [Test]
        public void ParseTest()
        {
            string text = @"TRACK 1.
 FLEX ROUTE : KALNA 42N160E 45N170E 47N180E 49N170W 50N160W 51N150W
              51N140W ORNAI
JAPAN ROUTE : ONION OTR5 KALNA
  NAR ROUTE : ACFT LDG KSEA--ORNAI SIMLU KEPKO TOU MARNR KSEA
              ACFT LDG KPDX--ORNAI SIMLU KEPKO TOU KEIKO KPDX
              ACFT LDG CYVR--ORNAI SIMLU KEPKO YAZ FOCHE CYVR
        RMK : ACFT LDG OTHER DEST--ORNAI SIMLU KEPKO UPR TO DEST";
            
            var result = Interpreter.Parse(text);

            Assert.AreEqual(1, result.ID);
            Assert.IsTrue(result.FlexRoute.SequenceEqual(flexRoute));

            Assert.IsTrue(result.ConnectionRoute == @"JAPAN ROUTE : ONION OTR5 KALNA
  NAR ROUTE : ACFT LDG KSEA--ORNAI SIMLU KEPKO TOU MARNR KSEA
              ACFT LDG KPDX--ORNAI SIMLU KEPKO TOU KEIKO KPDX
              ACFT LDG CYVR--ORNAI SIMLU KEPKO YAZ FOCHE CYVR
        ");
            Assert.IsTrue(result.Remark == " ACFT LDG OTHER DEST--ORNAI SIMLU KEPKO UPR TO DEST");
        }

        [Test]
        public void ParseTestNoRemark()
        {
            string text = @"TRACK 1.
 FLEX ROUTE : KALNA 42N160E 45N170E 47N180E 49N170W 50N160W 51N150W
              51N140W ORNAI
JAPAN ROUTE : ONION OTR5 KALNA
  NAR ROUTE : ACFT LDG KSEA--ORNAI SIMLU KEPKO TOU MARNR KSEA
              ACFT LDG KPDX--ORNAI SIMLU KEPKO TOU KEIKO KPDX
              ACFT LDG CYVR--ORNAI SIMLU KEPKO YAZ FOCHE CYVR";
            
            var result = Interpreter.Parse(text);

            Assert.AreEqual(1, result.ID);
            Assert.IsTrue(result.FlexRoute.SequenceEqual(flexRoute));

            Assert.IsTrue(result.ConnectionRoute == @"JAPAN ROUTE : ONION OTR5 KALNA
  NAR ROUTE : ACFT LDG KSEA--ORNAI SIMLU KEPKO TOU MARNR KSEA
              ACFT LDG KPDX--ORNAI SIMLU KEPKO TOU KEIKO KPDX
              ACFT LDG CYVR--ORNAI SIMLU KEPKO YAZ FOCHE CYVR");
            Assert.IsTrue(result.Remark == "");
        }
    }
}
