﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using QSP.AircraftProfiles.Configs;

namespace QSP.AircraftProfiles.Configs
{
    public class AcConfigManager
    {
        private Dictionary<string, List<AircraftConfig>> aircrafts;
        private Dictionary<string, AircraftConfig> registrations;

        public AcConfigManager()
        {
            aircrafts = new Dictionary<string, List<AircraftConfig>>();
            registrations = new Dictionary<string, AircraftConfig>();
        }

        public IEnumerable<AircraftConfig> Aircrafts
        {
            get
            {
                return registrations.Values;
            }
        }

        public int Count
        {
            get
            {
                return registrations.Count;
            }
        }

        /// <exception cref="ArgumentException">
        /// The registration already exists.</exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void Add(AircraftConfig item)
        {
            registrations.Add(item.Config.Registration, item);
            addToAircraft(item);
        }

        private void addToAircraft(AircraftConfig item)
        {
            List<AircraftConfig> acSameType;

            if (aircrafts.TryGetValue(item.Config.AC, out acSameType))
            {
                acSameType.Add(item);
            }
            else
            {
                aircrafts.Add(item.Config.AC,
                    new List<AircraftConfig>() { item });
            }
        }

        /// <summary>
        /// Returns whether the aircraft is found and removed.
        /// </summary>
        public bool Remove(string registration)
        {
            AircraftConfig config;

            if (registrations.TryGetValue(registration, out config))
            {
                registrations.Remove(registration);
                aircrafts[config.Config.AC].Remove(config);
                return true;
            }

            return false;
        }

        public void Clear()
        {
            aircrafts = new Dictionary<string, List<AircraftConfig>>();
            registrations = new Dictionary<string, AircraftConfig>();
        }

        /// <summary>
        /// Check whether the takeoff and landing performance files exist
        /// for all aircraft configs. 
        /// </summary>
        /// <exception cref="PerfFileNotFoundException"></exception>
        public void Validate(
            IEnumerable<TOPerfCalculation.PerfTable> takeoffTables,
            IEnumerable<LandingPerfCalculation.PerfTable> ldgTables)
        {
            var invalidAc = new List<AircraftConfig>();

            foreach (var i in registrations)
            {
                var config = i.Value;
                var to = config.Config.TOProfile;
                var ldg = config.Config.LdgProfile;

                bool toNotFound =
                    to != AircraftConfigItem.NoToLdgProfileText &&
                    takeoffTables.FirstOrDefault(
                    x => x.Entry.ProfileName == to) == null;

                bool ldgNotFound =
                    ldg != AircraftConfigItem.NoToLdgProfileText &&
                    ldgTables.FirstOrDefault(
                    x => x.Entry.ProfileName == ldg) == null;

                if (toNotFound || ldgNotFound)
                {
                    invalidAc.Add(config);
                }
            }

            if (invalidAc.Count > 0)
            {
                throw new PerfFileNotFoundException(errorMsg(invalidAc));
            }
        }

        private static string errorMsg(List<AircraftConfig> invalidItems)
        {
            var msg = new StringBuilder(
                 "Cannot find takeoff/landing performance profiles " +
                 "for the following aircraft(s):\n");

            foreach (var i in invalidItems)
            {
                var c = i.Config;
                msg.AppendLine(c.Registration + " (" + c.AC + ")");
            }

            return msg.ToString();
        }

        /// <summary>
        /// Returns null if not found.
        /// </summary>
        public AircraftConfig Find(string registration)
        {
            AircraftConfig config;

            if (registrations.TryGetValue(registration, out config))
            {
                return config;
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<AircraftConfig> FindAircraft(string aircraft)
        {
            List<AircraftConfig> configs;

            if (aircrafts.TryGetValue(aircraft, out configs))
            {
                return configs;
            }
            else
            {
                return new List<AircraftConfig>();
            }
        }
    }
}
