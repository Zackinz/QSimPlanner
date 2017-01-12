﻿using NUnit.Framework;
using QSP.AircraftProfiles.Configs;
using QSP.FuelCalculation.FuelData;
using QSP.Utilities.Units;
using System.Collections.Generic;
using System.Linq;

namespace UnitTest.AircraftProfiles.Configs
{
    [TestFixture]
    public class AcConfigManagerTest
    {
        private static AircraftConfig config1 =
            new AircraftConfig(
                new AircraftConfigItem("B777-300ER",
                    "B-12345",
                    "Boeing 777-300ER",
                    "Boeing 777-300ER",
                    "Boeing 777-300ER",
                    123456.0,
                    234567.0,
                    345678.0,
                    456789.0,
                    567890.0,
                    1.0,
                    WeightUnit.KG),
                "path");

        private static AircraftConfig config2 =
            new AircraftConfig(
                new AircraftConfigItem("B777-300ER",
                    "B-9876",
                    "Boeing 777-300ER custom",
                    "Boeing 777-300ER custom",
                    "Boeing 777-300ER custom",
                    23456.0,
                    34567.0,
                    45678.0,
                    56789.0,
                    567890.0,
                    1.0,
                    WeightUnit.KG),
                "path");

        [Test]
        public void AddTest()
        {
            var manager = new AcConfigManager();
            manager.Add(config1);
            manager.Add(config2);

            Assert.AreEqual(2, manager.Count);
        }

        [Test]
        public void FindAircraftTest()
        {
            var manager = new AcConfigManager();
            manager.Add(config1);
            manager.Add(config2);

            var result = manager.FindAircraft(config1.Config.AC).ToList();

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(config1));
            Assert.IsTrue(result.Contains(config2));
        }

        [Test]
        public void FindByRegistrationTest()
        {
            var manager = new AcConfigManager();
            manager.Add(config1);

            var result = manager.Find(config1.Config.Registration);

            Assert.IsNotNull(result);
            Assert.AreEqual(config1, result);
        }

        [Test]
        public void RemoveTest()
        {
            var manager = new AcConfigManager();
            manager.Add(config1);
            manager.Add(config2);
            manager.Remove(config1.Config.Registration);

            Assert.AreEqual(1, manager.Count);

            var ac2 = manager.Find(config2.Config.Registration);
            Assert.AreEqual(config2, ac2);
        }

        [Test]
        public void ClearTest()
        {
            var manager = new AcConfigManager();
            manager.Add(config1);
            manager.Clear();

            Assert.AreEqual(0, manager.Count);
        }

        [Test]
        public void ValidateFileExistShouldPass()
        {
            var manager = new AcConfigManager();
            manager.Add(config1);

            var fuelTable = new FuelData(null, "Boeing 777-300ER", "");

            var toFile = new QSP.TOPerfCalculation.Entry("Boeing 777-300ER", "");
            var toTable = new QSP.TOPerfCalculation.PerfTable(null, toFile);

            var ldgFile = new QSP.LandingPerfCalculation.Entry("Boeing 777-300ER", "");
            var ldgTable = new QSP.LandingPerfCalculation.PerfTable(null, ldgFile);

            manager.Validate(
                new[] { fuelTable },
                new[] { toTable },
                new[] { ldgTable });
        }

        [Test]
        public void ValidateFileDoesNotExistShouldThrow()
        {
            var manager = new AcConfigManager();
            manager.Add(config1);

            Assert.Throws<PerfFileNotFoundException>(() =>
            manager.Validate(
                new FuelData[0],
                new List<QSP.TOPerfCalculation.PerfTable>(),
                new List<QSP.LandingPerfCalculation.PerfTable>()));
        }
    }
}
