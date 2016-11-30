﻿using System.Linq;
using NUnit.Framework;
using QSP.AviationTools.Coordinates;
using QSP.FuelCalculation.Calculations;
using QSP.FuelCalculation.Results.Nodes;
using static QSP.FuelCalculation.Calculations.NodeMarker;

namespace UnitTest.FuelCalculation.Calculations
{
    [TestFixture]
    public class NodeMarkerTest
    {
        [Test]
        public void TocIndexTest1()
        {
            var nodes = TransformNode(
                new[] { 50.0, 10000.0, 10000.0001, 10000.0, 3000.0 });

            Assert.AreEqual(1, TocIndex(nodes));
        }

        [Test]
        public void TocIndexTest2()
        {
            var nodes = TransformNode(new[] { 50.0, 10000.0, 3000.0 });
            Assert.AreEqual(1, TocIndex(nodes));
        }

        [Test]
        public void TodIndexTest1()
        {
            var nodes = TransformNode(
                new[] { 50.0, 10000.0, 10000.0001, 10000.0, 3000.0 });

            Assert.AreEqual(3, TodIndex(nodes));
        }

        [Test]
        public void TodIndexTest2()
        {
            var nodes = TransformNode(new[] { 50.0, 10000.0, 3000.0 });
            Assert.AreEqual(1, TodIndex(nodes));
        }

        [Test]
        public void ScIndiciesTest()
        {
            var alts = new[] {50.0, 10000.0, 10000.0001, 10000.0, 10800.0,
                12000.0, 12000.0001, 14000.0, 3000.0 };

            var nodes = TransformNode(alts);

            Assert.IsTrue(new[] { 3, 6 }.SequenceEqual(ScIndices(nodes)));
        }

        private static IPlanNode[] TransformNode(double[] alts)
        {
            var result = new IPlanNode[alts.Length];

            for (int i = 0; i < alts.Length; i++)
            {
                result[i] = GetNode(alts[i], i, i);
            }

            return result;
        }

        private static IPlanNode GetNode(double alt, double lat, double lon)
        {
            var val = new IntermediateNode(new LatLon(lat, lon));
            return new PlanNode(val, null, null, null, alt, 
                0.0, 0.0, 0.0, 0.0, 0.0, 0.0);
        }
    }
}