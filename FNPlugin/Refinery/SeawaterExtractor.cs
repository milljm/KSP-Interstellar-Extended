﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using OpenResourceSystem;
using FNPlugin.Extensions;

namespace FNPlugin.Refinery
{
    class SeawaterExtractor : IRefineryActivity
    {
        // persistant
        [KSPField(isPersistant = true)]
        protected int lastBodyID = -1; // ID of the last body. Allows us to skip some expensive calls
        [KSPField(isPersistant = true)]
        protected double lastTotalLiquidScooped = 0; // we need to hold this for offline collecting

        /* Individual percentages of all consituents of the local atmosphere. These are bound to be found in different
         * concentrations in all atmospheres. These are persistant because getting them every update through 
         * the functions (see way below) would be wasteful. I'm placing them up here to make them easier to spot.
         */

        [KSPField(isPersistant = true)]
        protected double _argonPercentage = 0; // percentage of argon in the local ocean
        [KSPField(isPersistant = true)]
        protected double _dioxidePercentage = 0; // percentage of carbon dioxide in the local ocean
        [KSPField(isPersistant = true)]
        protected double _helium3Percentage = 0; // etc.
        [KSPField(isPersistant = true)]
        protected double _helium4Percentage = 0;
        [KSPField(isPersistant = true)]
        protected double _hydrogenPercentage = 0;
        [KSPField(isPersistant = true)]
        protected double _methanePercentage = 0;
        [KSPField(isPersistant = true)]
        protected double _monoxidePercentage = 0;
        [KSPField(isPersistant = true)]
        protected double _neonPercentage = 0;
        [KSPField(isPersistant = true)]
        protected double _nitrogenPercentage = 0;
        [KSPField(isPersistant = true)]
        protected double _oxygenPercentage = 0;
        [KSPField(isPersistant = true)]
        protected double _waterPercentage = 0;
        [KSPField(isPersistant = true)]
        protected double _heavywaterPercentage = 0;
        [KSPField(isPersistant = true)]
        protected double _xenonPercentage = 0;
        [KSPField(isPersistant = true)]
        protected double _deuteriumPercentage = 0;
        [KSPField(isPersistant = true)]
        protected double _kryptonPercentage = 0;
        [KSPField(isPersistant = true)]
        protected double _sodiumPercentage = 0;

        const int labelWidth = 200;
        const int valueWidth = 200;

        protected Part _part;
        protected Vessel _vessel;
        protected String _status = "";

        protected double _current_power;
        protected double _fixedConsumptionRate;
        protected double _consumptionStorageRatio;
        protected List<double> storageRatios;


        protected double _intakeLiquid_density;

        // all the resources that it should be possible to collect from oceans
        protected double _argon_density;
        protected double _dioxide_density;
        protected double _helium3_density;
        protected double _helium4_density;
        protected double _hydrogen_density;
        protected double _methane_density;
        protected double _monoxide_density;
        protected double _neon_density;
        protected double _nitrogen_density;
        protected double _oxygen_density;
        protected double _water_density;
        protected double _heavywater_density;
        protected double _xenon_density;
        protected double _deuterium_density;
        protected double _krypton_density;
        protected double _sodium_density;

        protected double _intakeLiquid_consumption_rate;

        protected double _argon_production_rate;
        protected double _dioxide_production_rate;
        protected double _helium3_production_rate;
        protected double _helium4_production_rate;
        protected double _hydrogen_production_rate;
        protected double _methane_production_rate;
        protected double _monoxide_production_rate;
        protected double _neon_production_rate;
        protected double _nitrogen_production_rate;
        protected double _oxygen_production_rate;
        protected double _water_production_rate;
        protected double _heavywater_production_rate;
        protected double _xenon_production_rate;
        protected double _deuterium_production_rate;
        protected double _krypton_production_rate;
        protected double _sodium_production_rate;

        protected string _intakeLiquid_resource_name;
        protected string _argon_resource_name;
        protected string _dioxide_resource_name;
        protected string _helium3_resource_name;
        protected string _helium4_resource_name;
        protected string _hydrogen_resource_name;
        protected string _methane_resource_name;
        protected string _monoxide_resource_name;
        protected string _neon_resource_name;
        protected string _nitrogen_resource_name;
        protected string _oxygen_resource_name;
        protected string _water_resource_name;
        protected string _heavywater_resource_name;
        protected string _xenon_resource_name;
        protected string _deuterium_resource_name;
        protected string _krypton_resource_name;
        protected string _sodium_resource_name;

        protected double _current_rate;

        private GUIStyle _bold_label;

        public int RefineryType { get { return 1; } }

        public String ActivityName { get { return "Seawater Extraction"; } }

        public double CurrentPower { get { return _current_power; } }

        private double _effectiveMaxPower;

        public bool HasActivityRequirements
        {
            get
            {
                return IsThereAnyLiquid();
            }
        }

        public double PowerRequirements { get { return PluginHelper.BaseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }



        public SeawaterExtractor(Part part)
        {
            _part = part;
            _vessel = part.vessel;

            // get the name of all relevant resources
            _intakeLiquid_resource_name = InterstellarResourcesConfiguration.Instance.IntakeLiquid;
            _argon_resource_name = InterstellarResourcesConfiguration.Instance.Argon;
            _dioxide_resource_name = InterstellarResourcesConfiguration.Instance.CarbonDioxide;
            _helium3_resource_name = InterstellarResourcesConfiguration.Instance.LqdHelium3;
            _helium4_resource_name = InterstellarResourcesConfiguration.Instance.LqdHelium4;
            _hydrogen_resource_name = InterstellarResourcesConfiguration.Instance.Hydrogen;
            _methane_resource_name = InterstellarResourcesConfiguration.Instance.Methane;
            _monoxide_resource_name = InterstellarResourcesConfiguration.Instance.CarbonMoxoxide;
            _neon_resource_name = InterstellarResourcesConfiguration.Instance.NeonGas;
            _nitrogen_resource_name = InterstellarResourcesConfiguration.Instance.Nitrogen;
            _oxygen_resource_name = InterstellarResourcesConfiguration.Instance.Oxygen;
            _water_resource_name = InterstellarResourcesConfiguration.Instance.Water;
            _heavywater_resource_name = InterstellarResourcesConfiguration.Instance.HeavyWater;
            _xenon_resource_name = InterstellarResourcesConfiguration.Instance.XenonGas;
            _deuterium_resource_name = InterstellarResourcesConfiguration.Instance.LqdDeuterium;
            _krypton_resource_name = InterstellarResourcesConfiguration.Instance.KryptonGas;
            _sodium_resource_name = InterstellarResourcesConfiguration.Instance.Sodium;

            // get the densities of all relevant resources
            _intakeLiquid_density = PartResourceLibrary.Instance.GetDefinition(_intakeLiquid_resource_name).density;
            _argon_density = PartResourceLibrary.Instance.GetDefinition(_argon_resource_name).density;
            _dioxide_density = PartResourceLibrary.Instance.GetDefinition(_dioxide_resource_name).density;
            _helium3_density = PartResourceLibrary.Instance.GetDefinition(_helium3_resource_name).density;
            _helium4_density = PartResourceLibrary.Instance.GetDefinition(_helium4_resource_name).density;
            _hydrogen_density = PartResourceLibrary.Instance.GetDefinition(_hydrogen_resource_name).density;
            _methane_density = PartResourceLibrary.Instance.GetDefinition(_methane_resource_name).density;
            _monoxide_density = PartResourceLibrary.Instance.GetDefinition(_monoxide_resource_name).density;
            _neon_density = PartResourceLibrary.Instance.GetDefinition(_neon_resource_name).density;
            _nitrogen_density = PartResourceLibrary.Instance.GetDefinition(_nitrogen_resource_name).density;
            _oxygen_density = PartResourceLibrary.Instance.GetDefinition(_oxygen_resource_name).density;
            _water_density = PartResourceLibrary.Instance.GetDefinition(_water_resource_name).density;
            _heavywater_density = PartResourceLibrary.Instance.GetDefinition(_water_resource_name).density;
            _xenon_density = PartResourceLibrary.Instance.GetDefinition(_xenon_resource_name).density;
            _deuterium_density = PartResourceLibrary.Instance.GetDefinition(_deuterium_resource_name).density;
            _krypton_density = PartResourceLibrary.Instance.GetDefinition(_krypton_resource_name).density;
            _sodium_density = PartResourceLibrary.Instance.GetDefinition(_sodium_resource_name).density;
        }

        //protected double _maxCapacityIntakeLiquidMass;
        protected double _maxCapacityArgonMass;
        protected double _maxCapacityDioxideMass;
        protected double _maxCapacityHelium3Mass;
        protected double _maxCapacityHelium4Mass;
        protected double _maxCapacityHydrogenMass;
        protected double _maxCapacityMethaneMass;
        protected double _maxCapacityMonoxideMass;
        protected double _maxCapacityNeonMass;
        protected double _maxCapacityNitrogenMass;
        protected double _maxCapacityOxygenMass;
        protected double _maxCapacityWaterMass;
        protected double _maxCapacityHeavyWaterMass;
        protected double _maxCapacityXenonMass;
        protected double _maxCapacityDeuteriumMass;
        protected double _maxCapacityKryptonMass;
        protected double _maxCapacitySodiumMass;

        protected double _availableIntakeLiquidMass;
        protected double _spareRoomArgonMass;
        protected double _spareRoomDioxideMass;
        protected double _spareRoomHelium3Mass;
        protected double _spareRoomHelium4Mass;
        protected double _spareRoomHydrogenMass;
        protected double _spareRoomMethaneMass;
        protected double _spareRoomMonoxideMass;
        protected double _spareRoomNeonMass;
        protected double _spareRoomNitrogenMass;
        protected double _spareRoomOxygenMass;
        protected double _spareRoomWaterMass;
        protected double _spareRoomHeavyWaterMass;
        protected double _spareRoomXenonMass;
        protected double _spareRoomDeuteriumMass;
        protected double _spareRoomKryptonMass;
        protected double _spareRoomSodiumMass;

        List<AtmosphericIntake> intakesList; // create a new list for keeping track of atmospheric intakes
        double tempLqd;
        double tempSubmergedPercentage;
        double tempArea;

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime)
        {
            ExtractSeawater(rateMultiplier, powerFraction, productionModifier, allowOverflow, fixedDeltaTime, false);

            updateStatusMessage();
        }
        // this is a function used for IRefinery HasActivityRequirements check
        public bool IsThereAnyLiquid()
        {
            if (GetTotalLiquidScoopedPerSecond() > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /* This is just a short cycle that goes through the air intakes on the vessel, looks at which ones are submerged and multiplies the percentage of the part's submersion
         * with the amount of air it can intake (I'm taking the simplification that air intakes can also intake liquids and running with it). 
         * This value is later stored in the persistent totalAirValue, so that this process can access it when offline collecting.
         * tempLqd is just a variable used to temporarily hold the total amount while cycling through parts, then gets reset at every engine update.
         */
        public double GetTotalLiquidScoopedPerSecond()
        {
            intakesList = _vessel.FindPartModulesImplementing<AtmosphericIntake>(); // add any atmo intake part on the vessel to our list
            tempLqd = 0; // reset tempLqd before we go into the list
            tempArea = 0;
            tempSubmergedPercentage = 0;
            foreach (AtmosphericIntake intake in intakesList) // go through the list
            {
                if (intake.IntakeEnabled == true) // only process open intakes
                {
                    tempArea = intake.area; // get the area of the intake part (basically size of intake)
                    tempSubmergedPercentage = intake.part.submergedPortion; // get the percentage of submersion of the intake part (0-1), works only when everything is fully loaded
                    tempLqd += (tempArea * tempSubmergedPercentage); // add the current intake's liquid intake to our tempLqd. When done with the foreach cycle, we will have the total amount of liquid these intakes collect per cycle
                }
            }
            return tempLqd;
        }

        public void ExtractSeawater(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool offlineCollecting)
        {
            _effectiveMaxPower = productionModifier * PowerRequirements;

            _current_power = _effectiveMaxPower * powerFraction;
            _current_rate = CurrentPower / PluginHelper.ElectrolysisEnergyPerTon;

            // determine how much resource we have
            var partsThatContainArgon = _part.GetConnectedResources(_argon_resource_name);
            var partsThatContainDioxide = _part.GetConnectedResources(_dioxide_resource_name);
            var partsThatContainGasHelium3 = _part.GetConnectedResources(_helium3_resource_name);
            var partsThatContainGasHelium4 = _part.GetConnectedResources(_helium4_resource_name);
            var partsThatContainHydrogen = _part.GetConnectedResources(_hydrogen_resource_name);
            var partsThatContainMethane = _part.GetConnectedResources(_methane_resource_name);
            var partsThatContainMonoxide = _part.GetConnectedResources(_monoxide_resource_name);
            var partsThatContainNeon = _part.GetConnectedResources(_neon_resource_name);
            var partsThatContainNitrogen = _part.GetConnectedResources(_nitrogen_resource_name);
            var partsThatContainOxygen = _part.GetConnectedResources(_oxygen_resource_name);
            var partsThatContainWater = _part.GetConnectedResources(_water_resource_name);
            var partsThatContainHeavyWater = _part.GetConnectedResources(_heavywater_resource_name);
            var partsThatContainXenon = _part.GetConnectedResources(_xenon_resource_name);
            var partsThatContainDeuterium = _part.GetConnectedResources(_deuterium_resource_name);
            var partsThatContainKrypton = _part.GetConnectedResources(_krypton_resource_name);
            var partsThatContainSodium = _part.GetConnectedResources(_sodium_resource_name);

            // determine the maximum amount of a resource the vessel can hold (ie. tank capacities combined)
            _maxCapacityArgonMass = partsThatContainArgon.Sum(p => p.maxAmount) * _argon_density;
            _maxCapacityDioxideMass = partsThatContainDioxide.Sum(p => p.maxAmount) * _dioxide_density;
            _maxCapacityHelium3Mass = partsThatContainGasHelium3.Sum(p => p.maxAmount) * _helium3_density;
            _maxCapacityHelium4Mass = partsThatContainGasHelium4.Sum(p => p.maxAmount) * _helium4_density;
            _maxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _hydrogen_density;
            _maxCapacityMethaneMass = partsThatContainMethane.Sum(p => p.maxAmount) * _methane_density;
            _maxCapacityMonoxideMass = partsThatContainMonoxide.Sum(p => p.maxAmount) * _monoxide_density;
            _maxCapacityNeonMass = partsThatContainNeon.Sum(p => p.maxAmount) * _neon_density;
            _maxCapacityNitrogenMass = partsThatContainNitrogen.Sum(p => p.maxAmount) * _nitrogen_density;
            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygen_density;
            _maxCapacityWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _water_density;
            _maxCapacityHeavyWaterMass = partsThatContainHeavyWater.Sum(p => p.maxAmount) * _heavywater_density;
            _maxCapacityXenonMass = partsThatContainXenon.Sum(p => p.maxAmount) * _xenon_density;
            _maxCapacityDeuteriumMass = partsThatContainDeuterium.Sum(p => p.maxAmount) * _deuterium_density;
            _maxCapacityKryptonMass = partsThatContainKrypton.Sum(p => p.maxAmount) * _krypton_density;
            _maxCapacitySodiumMass = partsThatContainSodium.Sum(p => p.maxAmount) * _sodium_density;


            // how much liquid can we process
            _availableIntakeLiquidMass = GetTotalLiquidScoopedPerSecond() * _intakeLiquid_density;

            // determine how much spare room there is in the vessel's resource tanks (for the resources this is going to produce)
            _spareRoomArgonMass = partsThatContainArgon.Sum(r => r.maxAmount - r.amount) * _argon_density;
            _spareRoomDioxideMass = partsThatContainDioxide.Sum(r => r.maxAmount - r.amount) * _dioxide_density;
            _spareRoomHelium3Mass = partsThatContainGasHelium3.Sum(r => r.maxAmount - r.amount) * _helium3_density;
            _spareRoomHelium4Mass = partsThatContainGasHelium4.Sum(r => r.maxAmount - r.amount) * _helium4_density;
            _spareRoomHydrogenMass = partsThatContainHydrogen.Sum(r => r.maxAmount - r.amount) * _hydrogen_density;
            _spareRoomMethaneMass = partsThatContainMethane.Sum(r => r.maxAmount - r.amount) * _methane_density;
            _spareRoomMonoxideMass = partsThatContainMonoxide.Sum(r => r.maxAmount - r.amount) * _monoxide_density;
            _spareRoomNeonMass = partsThatContainNeon.Sum(r => r.maxAmount - r.amount) * _neon_density;
            _spareRoomNitrogenMass = partsThatContainNitrogen.Sum(r => r.maxAmount - r.amount) * _nitrogen_density;
            _spareRoomOxygenMass = partsThatContainOxygen.Sum(r => r.maxAmount - r.amount) * _oxygen_density;
            _spareRoomWaterMass = partsThatContainWater.Sum(r => r.maxAmount - r.amount) * _water_density;
            _spareRoomHeavyWaterMass = partsThatContainHeavyWater.Sum(r => r.maxAmount - r.amount) * _heavywater_density;
            _spareRoomXenonMass = partsThatContainXenon.Sum(r => r.maxAmount - r.amount) * _xenon_density;
            _spareRoomDeuteriumMass = partsThatContainDeuterium.Sum(r => r.maxAmount - r.amount) * _deuterium_density;
            _spareRoomKryptonMass = partsThatContainKrypton.Sum(r => r.maxAmount - r.amount) * _krypton_density;
            _spareRoomSodiumMass = partsThatContainSodium.Sum(r => r.maxAmount - r.amount) * _sodium_density;

            // this should determine how much resource this process can consume
            var fixedMaxLiquidConsumptionRate = _current_rate * fixedDeltaTime * _intakeLiquid_density;
            var liquidConsumptionRatio = offlineCollecting ? 1
                    : fixedMaxLiquidConsumptionRate > 0
                        ? Math.Min(fixedMaxLiquidConsumptionRate, _availableIntakeLiquidMass) / fixedMaxLiquidConsumptionRate
                        : 0;

            _fixedConsumptionRate = _current_rate * fixedDeltaTime * liquidConsumptionRatio;

            // begin the intake liquid processing
            // check if there is anything to consume and if there is spare room for at least one of the products
            if (_fixedConsumptionRate > 0 && (
                _spareRoomHydrogenMass > 0 || _spareRoomHelium3Mass > 0 || _spareRoomHelium4Mass > 0 || _spareRoomMonoxideMass > 0 ||
                _spareRoomNitrogenMass > 0 || _spareRoomArgonMass > 0 || _spareRoomDioxideMass > 0 || _spareRoomMethaneMass > 0 ||
                _spareRoomNeonMass > 0 || _spareRoomWaterMass > 0 || _spareRoomHeavyWaterMass > 0 || _spareRoomOxygenMass > 0 ||
                _spareRoomXenonMass > 0 || _spareRoomDeuteriumMass > 0 || _spareRoomKryptonMass > 0 || _spareRoomSodiumMass > 0))
            {
                /* Now to get the actual percentages from OceanicResourceHandler.
                 * Calls getOceanicResourceContent which calls getOceanicCompositionForBody which (if there's no definition, i.e. we're using a custom solar system
                 * with weird and fantastic new planets) in turn calls the new GenerateCompositionFromCelestialBody function, which creates a composition
                 * for the upper-level functions based on the planet's size and atmospheric pressure etc. So even though this is calling one method, it's actually going through two or three
                 *  total. Since we like CPUs and want to save them the hassle, let's close this off behind a cheap check.
                */
                if (FlightGlobals.currentMainBody.flightGlobalsIndex != lastBodyID) // did we change a SOI since last time? If yes, get new percentages. Should work the first time as well, since lastBodyID starts as -1, while bodies in the list start at 0
                {
                    // remember, all these are persistent. Once we get them, we won't need to calculate them again until we change SOI
                    _argonPercentage = OceanicResourceHandler.getOceanicResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _argon_resource_name);
                    _monoxidePercentage = OceanicResourceHandler.getOceanicResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _monoxide_resource_name);
                    _dioxidePercentage = OceanicResourceHandler.getOceanicResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _dioxide_resource_name);
                    _helium3Percentage = OceanicResourceHandler.getOceanicResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _helium3_resource_name);
                    _helium4Percentage = OceanicResourceHandler.getOceanicResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _helium4_resource_name);
                    _hydrogenPercentage = OceanicResourceHandler.getOceanicResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _hydrogen_resource_name);
                    _methanePercentage = OceanicResourceHandler.getOceanicResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _methane_resource_name);
                    _neonPercentage = OceanicResourceHandler.getOceanicResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _neon_resource_name);
                    _nitrogenPercentage = OceanicResourceHandler.getOceanicResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _nitrogen_resource_name);
                    _oxygenPercentage = OceanicResourceHandler.getOceanicResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _oxygen_resource_name);
                    _waterPercentage = OceanicResourceHandler.getOceanicResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _water_resource_name);
                    _heavywaterPercentage = OceanicResourceHandler.getOceanicResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _heavywater_resource_name);
                    _xenonPercentage = OceanicResourceHandler.getOceanicResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _xenon_resource_name);
                    _deuteriumPercentage = OceanicResourceHandler.getOceanicResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _deuterium_resource_name);
                    _kryptonPercentage = OceanicResourceHandler.getOceanicResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _krypton_resource_name);
                    _sodiumPercentage = OceanicResourceHandler.getOceanicResourceContent(FlightGlobals.currentMainBody.flightGlobalsIndex, _sodium_resource_name);

                    lastBodyID = FlightGlobals.currentMainBody.flightGlobalsIndex; // reassign the id of current body to the lastBodyID variable, ie. remember this planet, so that we skip this check next time!
                }

                if (offlineCollecting) // if we're collecting offline, we don't need to actually consume the resource, just provide the lines below with a number
                {
                    var totalProcessed = _fixedConsumptionRate; // since we're collecting offline, we can just use this number
                    _intakeLiquid_consumption_rate = totalProcessed * fixedDeltaTime;
                    ScreenMessages.PostScreenMessage("The seawater extractor processed " + _intakeLiquid_resource_name + " for " + fixedDeltaTime.ToString("F0") + " seconds, processing " + _intakeLiquid_consumption_rate.ToString("F2") + " units in total.", 60.0f, ScreenMessageStyle.UPPER_CENTER);     
                }
                else
                {
                    // how much of the consumed atmosphere is going to end up as these?
                    var fixedMaxArgonRate = _fixedConsumptionRate * _argonPercentage;
                    var fixedMaxDioxideRate = _fixedConsumptionRate * _dioxidePercentage;
                    var fixedMaxHelium3Rate = _fixedConsumptionRate * _helium3Percentage;
                    var fixedMaxHelium4Rate = _fixedConsumptionRate * _helium4Percentage;
                    var fixedMaxHydrogenRate = _fixedConsumptionRate * _hydrogenPercentage;
                    var fixedMaxMethaneRate = _fixedConsumptionRate * _methanePercentage;
                    var fixedMaxMonoxideRate = _fixedConsumptionRate * _monoxidePercentage;
                    var fixedMaxNeonRate = _fixedConsumptionRate * _neonPercentage;
                    var fixedMaxNitrogenRate = _fixedConsumptionRate * _nitrogenPercentage;
                    var fixedMaxOxygenRate = _fixedConsumptionRate * _oxygenPercentage;
                    var fixedMaxWaterRate = _fixedConsumptionRate * _waterPercentage;
                    var fixedMaxHeavyWaterRate = _fixedConsumptionRate * _heavywaterPercentage;
                    var fixedMaxXenonRate = _fixedConsumptionRate * _xenonPercentage;
                    var fixedMaxDeuteriumRate = _fixedConsumptionRate * _deuteriumPercentage;
                    var fixedMaxKryptonRate = _fixedConsumptionRate * _kryptonPercentage;
                    var fixedMaxSodiumRate = _fixedConsumptionRate * _sodiumPercentage;

                    // how much can we add to the tanks per cycle? If allowOverflow is on, just push it all in, regardless of if the tank can hold the amount. Otherwise adjust accordingly
                    var fixedMaxPossibleArgonRate = allowOverflow ? fixedMaxArgonRate : Math.Min(_spareRoomArgonMass, fixedMaxArgonRate);
                    var fixedMaxPossibleDioxideRate = allowOverflow ? fixedMaxDioxideRate : Math.Min(_spareRoomDioxideMass, fixedMaxDioxideRate);
                    var fixedMaxPossibleHelium3Rate = allowOverflow ? fixedMaxHelium3Rate : Math.Min(_spareRoomHelium3Mass, fixedMaxHelium3Rate);
                    var fixedMaxPossibleHelium4Rate = allowOverflow ? fixedMaxHelium4Rate : Math.Min(_spareRoomHelium4Mass, fixedMaxHelium4Rate);
                    var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomHydrogenMass, fixedMaxHydrogenRate);
                    var fixedMaxPossibleMethaneRate = allowOverflow ? fixedMaxMethaneRate : Math.Min(_spareRoomMethaneMass, fixedMaxMethaneRate);
                    var fixedMaxPossibleMonoxideRate = allowOverflow ? fixedMaxMonoxideRate : Math.Min(_spareRoomMonoxideMass, fixedMaxMonoxideRate);
                    var fixedMaxPossibleNeonRate = allowOverflow ? fixedMaxNeonRate : Math.Min(_spareRoomNeonMass, fixedMaxNeonRate);
                    var fixedMaxPossibleNitrogenRate = allowOverflow ? fixedMaxNitrogenRate : Math.Min(_spareRoomNitrogenMass, fixedMaxNitrogenRate);
                    var fixedMaxPossibleOxygenRate = allowOverflow ? fixedMaxOxygenRate : Math.Min(_spareRoomOxygenMass, fixedMaxOxygenRate);
                    var fixedMaxPossibleWaterRate = allowOverflow ? fixedMaxWaterRate : Math.Min(_spareRoomWaterMass, fixedMaxWaterRate);
                    var fixedMaxPossibleHeavyWaterRate = allowOverflow ? fixedMaxHeavyWaterRate : Math.Min(_spareRoomHeavyWaterMass, fixedMaxHeavyWaterRate);
                    var fixedMaxPossibleXenonRate = allowOverflow ? fixedMaxXenonRate : Math.Min(_spareRoomXenonMass, fixedMaxXenonRate);
                    var fixedMaxPossibleDeuteriumRate = allowOverflow ? fixedMaxDeuteriumRate : Math.Min(_spareRoomDeuteriumMass, fixedMaxDeuteriumRate);
                    var fixedMaxPossibleKryptonRate = allowOverflow ? fixedMaxKryptonRate : Math.Min(_spareRoomKryptonMass, fixedMaxKryptonRate);
                    var fixedMaxPossibleSodiumRate = allowOverflow ? fixedMaxSodiumRate : Math.Min(_spareRoomSodiumMass, fixedMaxSodiumRate);

                    // Check if the denominator for each is zero (in that case, assign zero outright, so that we don't end up with an infinite mess on our hands)
                    double arRatio = (fixedMaxArgonRate == 0) ? 0 : fixedMaxPossibleArgonRate / fixedMaxArgonRate;
                    double dioxRatio = (fixedMaxDioxideRate == 0) ? 0 : fixedMaxPossibleDioxideRate / fixedMaxDioxideRate;
                    double he3Ratio = (fixedMaxHelium3Rate == 0) ? 0 : fixedMaxPossibleHelium3Rate / fixedMaxHelium3Rate;
                    double he4Ratio = (fixedMaxHelium4Rate == 0) ? 0 : fixedMaxPossibleHelium4Rate / fixedMaxHelium4Rate;
                    double hydroRatio = (fixedMaxHydrogenRate == 0) ? 0 : fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate;
                    double methRatio = (fixedMaxMethaneRate == 0) ? 0 : fixedMaxPossibleMethaneRate / fixedMaxMethaneRate;
                    double monoxRatio = (fixedMaxMonoxideRate == 0) ? 0 : fixedMaxPossibleMonoxideRate / fixedMaxMonoxideRate;
                    double neonRatio = (fixedMaxNeonRate == 0) ? 0 : fixedMaxPossibleNeonRate / fixedMaxNeonRate;
                    double nitroRatio = (fixedMaxNitrogenRate == 0) ? 0 : fixedMaxPossibleNitrogenRate / fixedMaxNitrogenRate;
                    double oxyRatio = (fixedMaxOxygenRate == 0) ? 0 : fixedMaxPossibleOxygenRate / fixedMaxOxygenRate;
                    double waterRatio = (fixedMaxWaterRate == 0) ? 0 : fixedMaxPossibleWaterRate / fixedMaxWaterRate;
                    double heavywaterRatio = (fixedMaxHeavyWaterRate == 0) ? 0 : fixedMaxPossibleHeavyWaterRate / fixedMaxHeavyWaterRate;
                    double xenonRatio = (fixedMaxXenonRate == 0) ? 0 : fixedMaxPossibleXenonRate / fixedMaxXenonRate;
                    double deuteriumRatio = (fixedMaxDeuteriumRate == 0) ? 0 : fixedMaxPossibleDeuteriumRate / fixedMaxDeuteriumRate;
                    double kryptonRatio = (fixedMaxKryptonRate == 0) ? 0 : fixedMaxPossibleKryptonRate / fixedMaxKryptonRate;
                    double sodiumRatio = (fixedMaxSodiumRate == 0) ? 0 : fixedMaxPossibleSodiumRate / fixedMaxSodiumRate;

                    /* finds a non-zero minimum of all the ratios (calculated above, as fixedMaxPossibleZZRate / fixedMaxZZRate). It needs to be non-zero 
                    * so that the collecting works even when some of consitutents are absent from the local atmosphere (ie. when their definition is zero).
                    * Otherwise the consumptionStorageRatio would be zero and thus no atmosphere would be consumed. */
                    storageRatios = new List<double> { arRatio, dioxRatio, he3Ratio, he4Ratio, hydroRatio, methRatio, monoxRatio, neonRatio, nitroRatio, oxyRatio, waterRatio, heavywaterRatio, xenonRatio, deuteriumRatio, kryptonRatio, sodiumRatio };
                    if (storageRatios.All(d => d == 0)) // if all the ratios are zero (if the storage is full but the overflow is off, basically), assign zero to avoid problems (like calling .Min() below on no objects)
                    {
                        _consumptionStorageRatio = 0;
                    }
                    else
                    {
                        _consumptionStorageRatio = storageRatios.Where(x => x > 0).Min(); // otherwise pick the lowest non-zero ratio
                    }
                    

                    // this simulates the amount of resource consumed - it's not actually consumed, as it is not actually collected. Just simulated. This does away with the need for patching storage for intake liquid to every air intake part etc.
                    // and it also means this process can't be exploited (bring intake liquid from elsewhere to target planet and process it there) and overall it's just less messy.
                    _intakeLiquid_consumption_rate = (_consumptionStorageRatio * _fixedConsumptionRate / _intakeLiquid_density) / fixedDeltaTime * _intakeLiquid_density;
                }

                // calculate the rates of production for the individual constituents
                var argon_rate_temp = _intakeLiquid_consumption_rate * _argonPercentage;
                var dioxide_rate_temp = _intakeLiquid_consumption_rate * _dioxidePercentage;
                var helium3_rate_temp = _intakeLiquid_consumption_rate * _helium3Percentage;
                var helium4_rate_temp = _intakeLiquid_consumption_rate * _helium4Percentage;
                var hydrogen_rate_temp = _intakeLiquid_consumption_rate * _hydrogenPercentage;
                var methane_rate_temp = _intakeLiquid_consumption_rate * _methanePercentage;
                var monoxide_rate_temp = _intakeLiquid_consumption_rate * _monoxidePercentage;
                var neon_rate_temp = _intakeLiquid_consumption_rate * _neonPercentage;
                var nitrogen_rate_temp = _intakeLiquid_consumption_rate * _nitrogenPercentage;
                var oxygen_rate_temp = _intakeLiquid_consumption_rate * _oxygenPercentage;
                var water_rate_temp = _intakeLiquid_consumption_rate * _waterPercentage;
                var heavywater_rate_temp = _intakeLiquid_consumption_rate * _heavywaterPercentage;
                var xenon_rate_temp = _intakeLiquid_consumption_rate * _xenonPercentage;
                var deuterium_rate_temp = _intakeLiquid_consumption_rate * _deuteriumPercentage;
                var krypton_rate_temp = _intakeLiquid_consumption_rate * _kryptonPercentage;
                var sodium_rate_temp = _intakeLiquid_consumption_rate * _sodiumPercentage;

                // produce the resources
                _argon_production_rate = -_part.RequestResource(_argon_resource_name, -argon_rate_temp * fixedDeltaTime / _argon_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _argon_density;
                _dioxide_production_rate = -_part.RequestResource(_dioxide_resource_name, -dioxide_rate_temp * fixedDeltaTime / _dioxide_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _dioxide_density;
                _helium3_production_rate = -_part.RequestResource(_helium3_resource_name, -helium3_rate_temp * fixedDeltaTime / _helium3_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _helium3_density;
                _helium4_production_rate = -_part.RequestResource(_helium4_resource_name, -helium4_rate_temp * fixedDeltaTime / _helium4_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _helium4_density;
                _hydrogen_production_rate = -_part.RequestResource(_hydrogen_resource_name, -hydrogen_rate_temp * fixedDeltaTime / _hydrogen_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _hydrogen_density;
                _methane_production_rate = -_part.RequestResource(_methane_resource_name, -methane_rate_temp * fixedDeltaTime / _methane_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _methane_density;
                _monoxide_production_rate = -_part.RequestResource(_monoxide_resource_name, -monoxide_rate_temp * fixedDeltaTime / _monoxide_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _monoxide_density;
                _neon_production_rate = -_part.RequestResource(_neon_resource_name, -neon_rate_temp * fixedDeltaTime / _neon_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _neon_density;
                _nitrogen_production_rate = -_part.RequestResource(_nitrogen_resource_name, -nitrogen_rate_temp * fixedDeltaTime / _nitrogen_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _nitrogen_density;
                _oxygen_production_rate = -_part.RequestResource(_oxygen_resource_name, -oxygen_rate_temp * fixedDeltaTime / _oxygen_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _oxygen_density;
                _water_production_rate = -_part.RequestResource(_water_resource_name, -water_rate_temp * fixedDeltaTime / _water_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _water_density;
                _heavywater_production_rate = -_part.RequestResource(_heavywater_resource_name, -heavywater_rate_temp * fixedDeltaTime / _heavywater_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _heavywater_density;
                _xenon_production_rate = -_part.RequestResource(_xenon_resource_name, -xenon_rate_temp * fixedDeltaTime / _xenon_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _xenon_density;
                _deuterium_production_rate = -_part.RequestResource(_deuterium_resource_name, -deuterium_rate_temp * fixedDeltaTime / _deuterium_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _deuterium_density;
                _krypton_production_rate = -_part.RequestResource(_krypton_resource_name, -krypton_rate_temp * fixedDeltaTime / _krypton_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _krypton_density;
                _sodium_production_rate = -_part.RequestResource(_sodium_resource_name, -sodium_rate_temp * fixedDeltaTime / _sodium_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _sodium_density;
            }
            else
            {
                _intakeLiquid_consumption_rate = 0;
                _argon_production_rate = 0;
                _dioxide_production_rate = 0;
                _helium3_production_rate = 0;
                _helium4_production_rate = 0;
                _hydrogen_production_rate = 0;
                _methane_production_rate = 0;
                _monoxide_production_rate = 0;
                _neon_production_rate = 0;
                _nitrogen_production_rate = 0;
                _oxygen_production_rate = 0;
                _water_production_rate = 0;
                _heavywater_production_rate = 0;
                _xenon_production_rate = 0;
                _deuterium_production_rate = 0;
                _krypton_production_rate = 0;
                _sodium_production_rate = 0;
            }
        }


        public void UpdateGUI()
        {
            if (_bold_label == null)
            {
                _bold_label = new GUIStyle(GUI.skin.label);
                _bold_label.fontStyle = FontStyle.Bold;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Power", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(_effectiveMaxPower), GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Intake Lqd Consumption", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((_intakeLiquid_consumption_rate * GameConstants.HOUR_SECONDS).ToString("0.0000")) + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            if (_hydrogenPercentage > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Hydrogen Storage", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label(_spareRoomHydrogenMass.ToString("0.0000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Hydrogen Production Rate", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label((float)(_hydrogenPercentage * 100) + "% " + (_hydrogen_production_rate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();
            }

            if (_deuteriumPercentage > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Deuterium Storage", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label(_spareRoomDeuteriumMass.ToString("0.0000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Deuterium Production Rate", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label((float)(_deuteriumPercentage * 100) + "% " + (_deuterium_production_rate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();
            }

            if (_helium3Percentage > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Helium-3 Storage", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label(_spareRoomHelium3Mass.ToString("0.0000") + " mT / " + _maxCapacityHelium3Mass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Helium-3 Production Rate", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label((float)(_helium3Percentage * 100) + "% " + (_helium3_production_rate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();
            }

            if (_helium4Percentage > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Helium-4 Storage", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label(_spareRoomHelium4Mass.ToString("0.0000") + " mT / " + _maxCapacityHelium4Mass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Helium-4 Production Rate", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label((float)(_helium4Percentage * 100) + "% " + (_helium4_production_rate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();
            }

            if (_nitrogenPercentage > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Nitrogen Storage", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label(_spareRoomNitrogenMass.ToString("0.0000") + " mT / " + _maxCapacityNitrogenMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Nitrogen Production Rate", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label((float)(_nitrogenPercentage * 100) + "% " + (_nitrogen_production_rate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();
            }

            if (_oxygenPercentage > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Oxygen Storage", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label(_spareRoomOxygenMass.ToString("0.0000") + " mT / " + _maxCapacityOxygenMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Oxygen Production Rate", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label((float)(_oxygenPercentage * 100) + "% " + (_oxygen_production_rate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();
            }

            if (_argonPercentage > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Argon Storage", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label(_spareRoomArgonMass.ToString("0.0000") + " mT / " + _maxCapacityArgonMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                GUILayout.Label("Argon Production Rate", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label((float)(_argonPercentage * 100) + "% " + (_argon_production_rate * GameConstants.HOUR_SECONDS).ToString("0.0000") + " mT/hour", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();
            }

            if (_neonPercentage > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Neon Storage", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label(_spareRoomNeonMass.ToString("0.0000") + " mT / " + _maxCapacityNeonMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Neon Production Rate", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label((float)(_neonPercentage * 100) + "% " + (_neon_production_rate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();
            }

            if (_kryptonPercentage > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Krypton Storage", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label(_spareRoomKryptonMass.ToString("0.0000") + " mT / " + _maxCapacityKryptonMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Krypton Production Rate", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label((float)(_kryptonPercentage * 100) + "% " + (_krypton_production_rate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();
            }

            if (_waterPercentage > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Water Storage", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label(_spareRoomWaterMass.ToString("0.0000") + " mT / " + _maxCapacityWaterMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Water Production Rate", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label((float)(_waterPercentage * 100) + "% " + (_water_production_rate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();
            }

            if (_heavywaterPercentage > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Heavy Water Storage", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label(_spareRoomHeavyWaterMass.ToString("0.0000") + " mT / " + _maxCapacityHeavyWaterMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Heavy Water Production Rate", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label((float)(_heavywaterPercentage * 100) + "% " + (_heavywater_production_rate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();
            }

            if (_monoxidePercentage > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Carbon Monoxide Storage", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label(_spareRoomMonoxideMass.ToString("0.0000") + " mT / " + _maxCapacityMonoxideMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Carbon Monoxide Production Rate", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label((float)(_monoxidePercentage * 100) + "% " + (_monoxide_production_rate * GameConstants.HOUR_SECONDS).ToString("0.0000") + " mT/hour", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();
            }

            if (_dioxidePercentage > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Carbon Dioxide Storage", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label(_spareRoomDioxideMass.ToString("0.0000") + " mT / " + _maxCapacityDioxideMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Carbon Dioxide Production Rate", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label((float)(_dioxidePercentage * 100) + "% " + (_dioxide_production_rate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();
            }

            if (_methanePercentage > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Methane Storage", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label(_spareRoomMethaneMass.ToString("0.0000") + " mT / " + _maxCapacityMethaneMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Methane Production Rate", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label((float)(_methanePercentage * 100) + "% " + (_methane_production_rate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();
            }

            if (_xenonPercentage > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Xenon Storage", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label(_spareRoomXenonMass.ToString("0.0000") + " mT / " + _maxCapacityXenonMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Xenon Production Rate", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label((float)(_xenonPercentage * 100) + "% " + (_xenon_production_rate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();
            }

            if (_sodiumPercentage > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Sodium Storage", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label(_spareRoomSodiumMass.ToString("0.0000") + " mT / " + _maxCapacitySodiumMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Sodium Production Rate", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label((float)(_sodiumPercentage * 100) + "% " + (_sodium_production_rate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();
            }

        }

        private void updateStatusMessage()
        {
            if (_intakeLiquid_consumption_rate > 0)
                _status = "Extracting intake liquid";
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage, try allowing overflow";
        }
    }
}