/*
Copyright(c) <2018> <University of Washington>
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Dqdv.Types;
using Dqdv.Types.Plot;

namespace Plotting
{
    public class Plotter
    {
        #region Constants

        private static readonly List<Point> NoPoints = new List<Point>();
        private static readonly List<Point2Y> NoPoints2Y = new List<Point2Y>();
        #endregion

        #region Private types

        private class Parameters
        {
            public int? MaxPointsPerSeries { get; set; }
            public int Simplification { get; set; }
            public int? FromCycle { get; set; }
            public int? ToCycle { get; set; }
            public int? EveryNthCycle { get; set; }
            public IndexRangeFilter CustomCycleFilter { get; set; }

            public bool IsChargeEnabled { get; set; }
            public bool IsDischargeEnabled { get; set; }

            public double? Threshold { get; set; }
            public double? MinY { get; set; }
            public double? MinY2 { get; set; }
            public double? MaxY { get; set; }
            public double? MaxY2 { get; set; }
            public CurrentUoM CurrentUoM { get; set; }
            public CapacityUoM CapacityUoM { get; set; }
            public TimeUoM TimeUoM { get; set; }
            public PowerUoM PowerUoM { get; set; }
            public EnergyUoM EnergyUoM { get; set; }
            public ResistanceUoM ResistanceUoM { get; set; }
            public NormalizeBy NormalizeBy { get; set; }
        }

        #endregion

        #region Private fields

        private readonly IProjectDataRepository _projectDataRepository;

        #endregion

        #region Constructor

        public Plotter(IProjectDataRepository projectDataRepository)
        {
            _projectDataRepository = projectDataRepository;
        }

        #endregion

        #region Public methods

        public Chart Plot(PlotType plotType, PlotParameters parameters, int[] projects, string trace)
        {
            var forcedEveryNthCycle = CalcForcedEveryNthCycle(plotType, projects, parameters, trace);
            var param = MakeParameters(parameters, forcedEveryNthCycle);
        
            var chart = CreateChart(plotType, param);
            chart.ForcedEveryNthCycle = forcedEveryNthCycle;

            foreach (var pid in projects)
                Plot(chart, pid, plotType, param, trace);
            return chart;
        }

        #endregion

        #region Private methods

        private Parameters MakeParameters(PlotParameters parameters, int? forcedEveryNthCycle)
        {
            return new Parameters
            {
                MaxPointsPerSeries = parameters?.MaxPointsPerSeries,
                Simplification = parameters?.Simplification ?? 0,
                FromCycle = parameters?.FromCycle,
                ToCycle = parameters?.ToCycle,
                EveryNthCycle = forcedEveryNthCycle ?? parameters?.EveryNthCycle,
                CustomCycleFilter = string.IsNullOrWhiteSpace(parameters?.CustomCycleFilter) ? null : new IndexRangeFilter(parameters.CustomCycleFilter),
                IsChargeEnabled = !(parameters?.DisableCharge ?? false),
                IsDischargeEnabled = !(parameters?.DisableDischarge ?? false),
                Threshold = parameters?.Threshold,
                MinY = parameters?.MinY,
                MaxY = parameters?.MaxY,
                CurrentUoM = parameters?.CurrentUoM ?? CurrentUoM.mA,
                CapacityUoM = parameters?.CapacityUoM ?? CapacityUoM.mAh,
                TimeUoM = parameters?.TimeUoM ?? TimeUoM.Seconds,
                PowerUoM = parameters?.PowerUoM ?? PowerUoM.W,
                EnergyUoM = parameters?.EnergyUoM ?? EnergyUoM.Wh,
                ResistanceUoM = parameters?.ResistanceUoM ?? ResistanceUoM.Ohm,
                NormalizeBy = parameters?.NormalizeBy ?? NormalizeBy.None
            };
        }

        private Chart CreateChart(PlotType plotType, Parameters parameters)
        {
            var chart = new Chart
            {
                Projects = new List<Project>(),
                Series = new List<Series>(),
                Label = new Label {Font = new Font(FontFamily.GenericMonospace, 8)}
            };

            switch (plotType)
            {
                case PlotType.EndCurrent:
                    chart.XAxisText = Title.CycleNumber;
                    chart.XAxisIsInteger = true;
                    chart.YAxisText = new[] { FormatCurrentAxisTitle(parameters) };
                    break;

                case PlotType.EndVoltage:
                    chart.XAxisText = Title.CycleNumber;
                    chart.XAxisIsInteger = true;
                    chart.YAxisText = new[] { Title.Voltage };
                    break;

                case PlotType.EndCapacity:
                    chart.XAxisText = Title.CycleNumber;
                    chart.XAxisIsInteger = true;
                    chart.YAxisText = new[] { FormatCapacityAxisTitle(parameters) };
                    break;

                case PlotType.CapacityRetention:
                    chart.XAxisText = Title.CycleNumber;
                    chart.XAxisIsInteger = true;
                    chart.YAxisText = new[] { FormatRetentionAxisTitle() };
                    break;

                case PlotType.Energy:
                    chart.XAxisText = Title.CycleNumber;
                    chart.XAxisIsInteger = true;
                    chart.YAxisText = new[] { FormatEnergyAxisTitle(parameters) };
                    break;

                case PlotType.MidVoltage:
                    chart.XAxisText = Title.CycleNumber;
                    chart.XAxisIsInteger = true;
                    chart.YAxisText = new[] { Title.MediumVoltage };
                    break;

                case PlotType.VoltageCapacity:
                    chart.XAxisText = FormatCapacityAxisTitle(parameters);
                    chart.YAxisText = new[] { Title.Voltage };
                    break;

                case PlotType.CoulombicEfficiency:
                    chart.XAxisText = Title.CycleNumber;
                    chart.XAxisIsInteger = true;
                    chart.YAxisText = new[] { Title.CoulombicEfficiency };
                    break;

                case PlotType.CyclicVoltammetry:
                    chart.XAxisText = Title.Voltage;
                    chart.YAxisText = new[] { FormatCurrentAxisTitle(parameters) };
                    break;

                case PlotType.DifferentialVoltage:
                    chart.XAxisText = FormatCapacityAxisTitle(parameters);
                    chart.YAxisText = new[] { Title.DVDQ };
                    break;

                case PlotType.DifferentialCapacity:
                    chart.XAxisText = Title.Voltage;
                    chart.YAxisText = new[] { Title.DQDV };
                    break;

                case PlotType.EndTimeEndCurrent:
                    chart.XAxisText = Title.Time;
                    chart.YAxisText = new[] { FormatCurrentAxisTitle(parameters) };
                    break;

                case PlotType.EndTimeEndVoltage:
                    chart.XAxisText = Title.Time;
                    chart.YAxisText = new[] { Title.Voltage };
                    break;

                case PlotType.ResistanceOhms:
                    chart.XAxisText = Title.CycleNumber;
                    chart.XAxisIsInteger = true;
                    chart.YAxisText = new[] { FormatResistanceAxisTitle(parameters) };
                    break;
            }

            return chart;
        }

        private int? CalcForcedEveryNthCycle(PlotType plotType, 
            int[] projects, 
            PlotParameters parameters, 
            string trace)
        {
            if (plotType != PlotType.VoltageCapacity && 
                plotType != PlotType.DifferentialVoltage && 
                plotType != PlotType.DifferentialCapacity)
                return null;

            var maxCycles = parameters?.MaxCycles ?? 0;
            if (maxCycles <= 0)
                return null;

            var maxCyclesPerProject = Math.Max(maxCycles / projects.Length - 1, 1);

            var forcedEveryNthCycle = projects.Max(pid =>
            {
                var cycles = _projectDataRepository.GetCycles(pid, trace).Count;

                if (!string.IsNullOrEmpty(parameters?.CustomCycleFilter))
                {
                    var rangeFilter = new IndexRangeFilter(parameters.CustomCycleFilter).RangesItems;
                    cycles = rangeFilter.Count;
                    var result = cycles / maxCyclesPerProject;
                    if (cycles % maxCyclesPerProject != 0)
                        result += 1;
                    return result;
                }
                else
                {
                    var fromCycle = Math.Max(parameters?.FromCycle ?? 1, 1);
                    var toCycle = Math.Min(parameters?.ToCycle ?? cycles, cycles);
                    cycles = toCycle - fromCycle + 1;
                    var result = cycles / maxCyclesPerProject;
                    if (cycles % maxCyclesPerProject != 0)
                        result += 1;
                    return result;
                }
            });

            if (forcedEveryNthCycle < 2)
                return null;

            if (parameters?.EveryNthCycle != null && 
                parameters.EveryNthCycle.Value >= forcedEveryNthCycle)
                return null;

            return forcedEveryNthCycle;
        }

        private void Plot(Chart chart, int projectId, PlotType plotType, Parameters parameters, string trace)
        {
            var project = _projectDataRepository.GetProject(projectId, trace);
            chart.Projects.Add(project);

            switch (plotType)
            {
                case PlotType.EndCurrent:
                    PlotEndCurrent(chart, project, parameters, trace);
                    break;

                case PlotType.EndVoltage:
                    PlotEndVoltage(chart, project, parameters, trace);
                    break;

                case PlotType.EndCapacity:
                    PlotEndCapacity(chart, project, parameters, trace);
                    break;

                case PlotType.CapacityRetention:
                    PlotCapacityRetention(chart, project, parameters, trace);
                    break;

                case PlotType.MidVoltage:
                    PlotMidVoltage(chart, project, parameters, trace);
                    break;

                case PlotType.VoltageCapacity:
                    PlotVoltageCapacity(chart, project, parameters, trace);
                    break;

                case PlotType.CoulombicEfficiency:
                    PlotCoulombicEfficiency(chart, project, parameters, trace);
                    break;

                case PlotType.CyclicVoltammetry:
                    PlotCyclicVoltammetry(chart, project, parameters, trace);
                    break;

                case PlotType.DifferentialVoltage:
                    PlotDifferentialVoltage(chart, project, parameters, trace);
                    break;

                case PlotType.DifferentialCapacity:
                    PlotDifferentialCapacity(chart, project, parameters, trace);
                    break;

                case PlotType.EndTimeEndCurrent:
                    PlotEndTimeEndCurrent(chart, project, parameters, trace);
                    break;
                case PlotType.EndTimeEndVoltage:
                    PlotEndTimeEndVoltage(chart, project, parameters, trace);
                    break;
                case PlotType.ResistanceOhms:
                    PlotResistanceOhms(chart, project, parameters, trace);
                    break;
            }
        }

        private void PlotResistanceOhms(Chart chart, Project project, Parameters parameters, string trace)
        {
            var cycles = _projectDataRepository.GetCycles(project.Id, trace);
            var multiplier = GetResistanceMultiplier(project, parameters);
            
            if (parameters.IsChargeEnabled)
            {
                var points = Plot(cycles, parameters, c => multiplier * c.ResistanceOhms);
                AddSeries(chart, project, parameters, points, Title.Charge);
            }
            if (parameters.IsDischargeEnabled)
            {
                var points = Plot(cycles, parameters, c => multiplier * c.DischargeResistance);
                AddSeries(chart, project, parameters, points, Title.Discharge);
            }
        }

        private void PlotEndCurrent(Chart chart, Project project, Parameters parameters, string trace)
        {
            var cycles = _projectDataRepository.GetCycles(project.Id, trace);
            var multiplier = GetCurrentMultiplier(project, parameters);

            if (parameters.IsChargeEnabled)
            {
                var points = Plot(cycles, parameters, c => multiplier * c.EndCurrent, c => multiplier * c.StatisticMetaData?.EndCurrentStdDev);
                AddSeries(chart, project, parameters, points, Title.Charge);
            }

            if (parameters.IsDischargeEnabled)
            {
                var points = Plot(cycles, parameters, c => multiplier * c.DischargeEndCurrent, c => multiplier * c.StatisticMetaData?.DischargeEndCurrentStdDev);
                AddSeries(chart, project, parameters, points, Title.Discharge);
            }
        }

        private void PlotEndVoltage(Chart chart, Project project, Parameters parameters, string trace)
        {
            var cycles = _projectDataRepository.GetCycles(project.Id, trace);

            if (parameters.IsChargeEnabled)
            {
                var points = Plot(cycles, parameters, c => c.EndVoltage, c => c.StatisticMetaData?.EndVoltageStdDev);
                AddSeries(chart, project, parameters, points, Title.Charge);
            }

            if (parameters.IsDischargeEnabled)
            {
                var points = Plot(cycles, parameters, c => c.DischargeEndVoltage, c => c.StatisticMetaData?.DischargeEndVoltageStdDev);
                AddSeries(chart, project, parameters, points, Title.Discharge);
            }
        }

        private void PlotMidVoltage(Chart chart, Project project, Parameters parameters, string trace)
        {
            var cycles = _projectDataRepository.GetCycles(project.Id, trace);
            var points = Plot(cycles, parameters, c => c.MidVoltage, c => c.StatisticMetaData?.MidVoltageStdDev);
            AddSeries(chart, project, parameters, points, Title.MidVoltage);
        }

        private void PlotCapacityRetention(Chart chart, Project project, Parameters parameters, string trace)
        {
            var cycles = _projectDataRepository.GetCycles(project.Id, trace);
            var multiplier = GetCapacityMultiplier(project, parameters);

            if (parameters.IsChargeEnabled)
            {
                var points = Plot(cycles, parameters, c => multiplier * c.ChargeCapacityRetention);
                AddSeries(chart, project, parameters, points, Title.Charge);
            }

            if (parameters.IsDischargeEnabled)
            {
                var points = Plot(cycles, parameters, c => multiplier * c.DischargeCapacityRetention);
                AddSeries(chart, project, parameters, points, Title.Discharge);
            }
        }

        private void PlotEndCapacity(Chart chart, Project project, Parameters parameters, string trace)
        {
            var cycles = _projectDataRepository.GetCycles(project.Id, trace);
            var multiplier = GetCapacityMultiplier(project, parameters);

            if (parameters.IsChargeEnabled)
            {
                var points = Plot(cycles, parameters, c => multiplier * c.ChargeCapacity, c => multiplier * c.StatisticMetaData?.ChargeCapacityStdDev);
                AddSeries(chart, project, parameters, points, Title.Charge);
            }

            if (parameters.IsDischargeEnabled)
            {
                var points = Plot(cycles, parameters, c => multiplier * c.DischargeCapacity, c => multiplier * c.StatisticMetaData?.DischargeCapacityStdDev);
                AddSeries(chart, project, parameters, points, Title.Discharge);
            }
        }

        private void PlotVoltageCapacity(Chart chart, Project project, Parameters parameters, string trace)
        {
            var cycles = _projectDataRepository.GetCycles(project.Id, trace);
            var allPoints = _projectDataRepository.GetPoints(project.Id, trace);
            var multiplier = GetCapacityMultiplier(project, parameters);

            foreach (var cycle in FilterCycles(cycles, parameters))
            {
                if (parameters.IsChargeEnabled)
                {
                    var points = Plot(allPoints, cycle, p => multiplier * p.Capacity, p => p.Voltage, parameters, p => p.CycleStep == CycleStep.ChargeCC || p.CycleStep == CycleStep.ChargeCV).ToList();
                    AddSeries(chart, project, parameters, points, Title.Charge, cycle.Index);
                }

                if (parameters.IsDischargeEnabled)
                {
                    var points = Plot(allPoints, cycle, p => multiplier * p.Capacity, p => p.Voltage, parameters, p => p.CycleStep == CycleStep.Discharge).ToList();
                    AddSeries(chart, project, parameters, points, Title.Discharge, cycle.Index);
                }
            }
        }

        private void PlotCoulombicEfficiency(Chart chart, Project project, Parameters parameters, string trace)
        {
            var cycles = _projectDataRepository.GetCycles(project.Id, trace);
            var points = Plot(cycles, parameters, c => 100.0f * (c.DischargeCapacity / c.ChargeCapacity), c => c.StatisticMetaData?.CoulombicEfficiencyStdDev);
            AddSeries(chart, project, parameters, points, Title.CoulombicEfficiency);
        }

        private void PlotCyclicVoltammetry(Chart chart, Project project, Parameters parameters, string trace)
        {
            var cycles = _projectDataRepository.GetCycles(project.Id, trace);
            var allPoints = _projectDataRepository.GetPoints(project.Id, trace);
            var multiplier = GetCurrentMultiplier(project, parameters);
            var points = FilterCycles(cycles, parameters)
                .SelectMany(c => Plot(allPoints, c, p => p.Voltage, p => multiplier * p.Current, parameters))
                .ToList();
            AddSeries(chart, project, parameters, points, Title.CyclicVoltammetry);
        }

        private void PlotDifferentialVoltage(Chart chart, Project project, Parameters parameters, string trace)
        {
            var cycles = _projectDataRepository.GetCycles(project.Id, trace);
            var allPoints = _projectDataRepository.GetPoints(project.Id, trace);
            var multiplier = GetCapacityMultiplier(project, parameters);

            foreach (var cycle in FilterCycles(cycles, parameters))
            {
                if (parameters.IsChargeEnabled)
                {
                    var points = PlotDerivative(allPoints, cycle.FirstPointIndex, cycle.PointCount,
                        p => p.CycleStep == CycleStep.ChargeCC || p.CycleStep == CycleStep.ChargeCV, p => multiplier * p.Capacity, p => p.Voltage, parameters).ToList();
                    AddSeries(chart, project, parameters, points, Title.Charge, cycle.Index);
                }

                if (parameters.IsDischargeEnabled)
                {
                    var points = PlotDerivative(allPoints, cycle.FirstPointIndex, cycle.PointCount,
                        p => p.CycleStep == CycleStep.Discharge, p => multiplier * p.Capacity, p => p.Voltage, parameters).ToList();
                    AddSeries(chart, project, parameters, points, Title.Discharge, cycle.Index);
                }
            }
        }

        private void PlotDifferentialCapacity(Chart chart, Project project, Parameters parameters, string trace)
        {
            var cycles = _projectDataRepository.GetCycles(project.Id, trace);
            var allPoints = _projectDataRepository.GetPoints(project.Id, trace);
            var multiplier = GetCapacityMultiplier(project, parameters);

            foreach (var cycle in FilterCycles(cycles, parameters))
            {
                if (parameters.IsChargeEnabled)
                {
                    var points = PlotDerivative(allPoints, cycle.FirstPointIndex, cycle.PointCount,
                        p => p.CycleStep == CycleStep.ChargeCC || p.CycleStep == CycleStep.ChargeCV, p => p.Voltage, p => multiplier * p.Capacity, parameters).ToList();
                    AddSeries(chart, project, parameters, points, Title.Charge, cycle.Index);
                }

                if (parameters.IsDischargeEnabled)
                {
                    var points = PlotDerivative(allPoints, cycle.FirstPointIndex, cycle.PointCount,
                        p => p.CycleStep == CycleStep.Discharge, p => p.Voltage, p => multiplier * p.Capacity, parameters).ToList();
                    AddSeries(chart, project, parameters, points, Title.Discharge, cycle.Index);
                }
            }
        }

        private void PlotEndTimeEndCurrent(Chart chart, Project project, Parameters parameters, string trace)
        {
            var cycles = _projectDataRepository.GetCycles(project.Id, trace);
            var allPoints = _projectDataRepository.GetPoints(project.Id, trace);
            var multiplier = GetCurrentMultiplier(project, parameters);
            var points = FilterCycles(cycles, parameters)
                .SelectMany(c => Plot(allPoints, c, p => p.Time, p => multiplier * p.Current, parameters))
                .ToList();
            AddSeries(chart, project, parameters, points, Title.TimeVsCurrent);
        }

        private void PlotEndTimeEndVoltage(Chart chart, Project project, Parameters parameters, string trace)
        {
            var cycles = _projectDataRepository.GetCycles(project.Id, trace);
            var allPoints = _projectDataRepository.GetPoints(project.Id, trace);
            var points = FilterCycles(cycles, parameters)
                .SelectMany(c => Plot(allPoints, c, p => p.Time, p => p.Voltage, parameters))
                .ToList();
            AddSeries(chart, project, parameters, points, Title.TimeVsVoltage);
        }

        private static string FormatCurrentAxis(CurrentUoM currentUoM, NormalizeBy normalizeBy)
        {
            switch (currentUoM)
            {
                case CurrentUoM.A:
                    switch (normalizeBy)
                    {
                        case NormalizeBy.Area:
                            return Title.CurrentCustom2;

                        case NormalizeBy.Mass:
                            return Title.CurrentCustom3;

                        default:
                            return Title.CurrentCustom1;
                    }
                case CurrentUoM.mA:
                    switch (normalizeBy)
                    {
                        case NormalizeBy.Area:
                            return Title.CurrentCustom5;

                        case NormalizeBy.Mass:
                            return Title.CurrentCustom6;

                        default:
                            return Title.CurrentCustom4;
                    }
                case CurrentUoM.uA:
                    switch (normalizeBy)
                    {
                        case NormalizeBy.Area:
                            return Title.CurrentCustom8;

                        case NormalizeBy.Mass:
                            return Title.CurrentCustom9;

                        default:
                            return Title.CurrentCustom7;
                    }
                default:
                    return Title.Current;
            }
        }

        private static string FormatCapacityAxis(CapacityUoM capacityUoM, NormalizeBy normalizeBy)
        {
            switch (capacityUoM)
            {
                case CapacityUoM.Ah:
                    switch (normalizeBy)
                    {
                        case NormalizeBy.Area:
                            return Title.CapacityCustom2;

                        case NormalizeBy.Mass:
                            return Title.CapacityCustom3;

                        default:
                            return Title.CapacityCustom1;
                    }
                case CapacityUoM.mAh:
                    switch (normalizeBy)
                    {
                        case NormalizeBy.Area:
                            return Title.CapacityCustom4;

                        case NormalizeBy.Mass:
                            return Title.CapacityCustom5;

                        default:
                            return Title.CapacityCustom6;
                    }
                default:
                    return Title.Capacity;
            }
        }

        private static string FormatResistanceAxisTitle(Parameters parameters)
        {
            return FormatResistanceAxis(parameters.ResistanceUoM);
        }

        private static string FormatResistanceAxis(ResistanceUoM resistanceUoM)
        {
            switch (resistanceUoM)
            {
                case ResistanceUoM.Ohm:
                    return Title.ResistanceCustom1;
                case ResistanceUoM.muOhm:
                    return Title.ResistanceCustom2;
                case ResistanceUoM.mOhm:
                    return Title.ResistanceCustom3;
                case ResistanceUoM.kOhm:
                    return Title.ResistanceCustom4;
                case ResistanceUoM.MOhm:
                    return Title.ResistanceCustom5;
                case ResistanceUoM.GOhm:
                    return Title.ResistanceCustom6;
                default:
                    return Title.ResistanceCustom1;
            }
        }

        private static string FormatCurrentAxisTitle(Parameters parameters)
        {
            return FormatCurrentAxis(parameters.CurrentUoM, parameters.NormalizeBy);
        }

        private static string FormatRetentionAxisTitle()
        {
            return Title.CapacityRetention;
        }

        private static string FormatEnergyAxisTitle(Parameters parameters)
        {
            switch (parameters.EnergyUoM)
            {
                case EnergyUoM.Wh:
                    return Title.EnergyCustom1;
                case EnergyUoM.muWh:
                    return Title.EnergyCustom2;
                case EnergyUoM.mWh:
                    return Title.EnergyCustom3;
                case EnergyUoM.kWh:
                    return Title.EnergyCustom4;
                case EnergyUoM.MWh:
                    return Title.EnergyCustom5;
                case EnergyUoM.GWh:
                    return Title.EnergyCustom6;
                default:
                    return Title.EnergyCustom1;
            }
        }

        private static string FormatCapacityAxisTitle(Parameters parameters)
        {
            return FormatCapacityAxis(parameters.CapacityUoM, parameters.NormalizeBy);
        }

        private static double GetNormalizeByDivider(Project project, Parameters parameters)
        {
            switch (parameters.NormalizeBy)
            {
                case NormalizeBy.Area:
                    return project.Area ?? 1.0;

                case NormalizeBy.Mass:
                    if (project.Mass.HasValue)
                    {
                        return project.ActiveMaterialFraction.HasValue ? 
                            project.Mass.Value * project.ActiveMaterialFraction.Value : 
                            project.Mass.Value;
                    }
                    else
                    {
                        return 1.0;
                    }
                default:
                    return 1.0;
            }
        }

        private static double GetCurrentMultiplier(CurrentUoM currentUoM)
        {
            switch (currentUoM)
            {
                case CurrentUoM.A:
                    return 0.001;

                case CurrentUoM.uA:
                    return 1000;

                default:
                    return 1;
            }
        }

        private static double GetCapacityMultiplier(CapacityUoM capacityUoM)
        {
            switch (capacityUoM)
            {
                case CapacityUoM.Ah:
                    return 0.001;

                default:
                    return 1;
            }
        }

        private static double GetTimeMultiplier(TimeUoM timeUoM)
        {
            switch (timeUoM)
            {
                case TimeUoM.Seconds:
                    return 1;
                case TimeUoM.Minutes:
                    return 0.0166666666666667; // 1 / 60
                case TimeUoM.Hours:
                    return 2.777777777777778e-4; // 1 / 3600
                case TimeUoM.Days:
                    return 1.157407407407407e-5; // 1 / (3600 * 24)
                default:
                    return 1;
            }
        }

        private static double GetPowerMultiplier(PowerUoM powerUoM)
        {
            switch (powerUoM)
            {
                case PowerUoM.W:
                    return 1;
                case PowerUoM.muW:
                    return 1000000;
                case PowerUoM.mW:
                    return 1000;
                case PowerUoM.kW:
                    return 0.001;
                case PowerUoM.MW:
                    return 0.000001;
                case PowerUoM.GW:
                    return 0.000000001;
                default:
                    return 1;
            }
        }

        private static double GetEnergyMultiplier(EnergyUoM energyUoM)
        {
            switch (energyUoM)
            {
                case EnergyUoM.Wh:
                    return 1;
                case EnergyUoM.muWh:
                    return 1000000;
                case EnergyUoM.mWh:
                    return 1000;
                case EnergyUoM.kWh:
                    return 0.001; 
                case EnergyUoM.MWh:
                    return 0.000001;
                case EnergyUoM.GWh:
                    return 0.0000000001;
                default:
                    return 1;
            }
        }

        private static double GetResistanceMultiplier(Project project, Parameters parameters)
        {
            var multiplier = GetResistanceMultiplier(parameters.ResistanceUoM) / GetNormalizeByDivider(project, parameters);
            return double.IsInfinity(multiplier) || double.IsNaN(multiplier) ? 1.0 : multiplier;
        }

        private static double GetResistanceMultiplier(ResistanceUoM resistanceUoM)
        {
            switch (resistanceUoM)
            {
                case ResistanceUoM.Ohm:
                    return 1;
                case ResistanceUoM.mOhm:
                    return 0.000001;
                case ResistanceUoM.kOhm:
                    return 0.001; 
                case ResistanceUoM.MOhm:
                    return 1000;
                default:
                    return 1;
            }
        }

        private static double GetCurrentMultiplier(Project project, Parameters parameters)
        {
            var multiplier = GetCurrentMultiplier(parameters.CurrentUoM) / GetNormalizeByDivider(project, parameters);
            return double.IsInfinity(multiplier) || double.IsNaN(multiplier) ? 1.0 : multiplier;
        }

        private static double GetCapacityMultiplier(Project project, Parameters parameters)
        {
            var multiplier = GetCapacityMultiplier(parameters.CapacityUoM) / GetNormalizeByDivider(project, parameters);
            return double.IsInfinity(multiplier) || double.IsNaN(multiplier) ? 1.0 : multiplier;
        }

        private static double GetTimeMultiplier(Project project, Parameters parameters)
        {
            var multiplier = GetTimeMultiplier(parameters.TimeUoM) / GetNormalizeByDivider(project, parameters);
            return double.IsInfinity(multiplier) || double.IsNaN(multiplier) ? 1.0 : multiplier;
        }

        private static double GetEnergyMultiplier(Project project, Parameters parameters)
        {
            var multiplier = GetEnergyMultiplier(parameters.EnergyUoM) / GetNormalizeByDivider(project, parameters);
            return double.IsInfinity(multiplier) || double.IsNaN(multiplier) ? 1.0 : multiplier;
        }

        private static IEnumerable<Cycle> FilterCycles(List<Cycle> cycles, Parameters parameters)
        {
            var lastCycleIndex = cycles.LastOrDefault()?.Index ?? 0;
            var query = cycles.AsEnumerable();

            if (parameters.FromCycle != null)
                query = query.Where(c => c.Index >= parameters.FromCycle.Value);

            if (parameters.ToCycle != null)
                query = query.Where(c => c.Index <= parameters.ToCycle.Value);

            if (parameters.EveryNthCycle != null)
            {
                var first = parameters.FromCycle ?? 1;
                var last = parameters.ToCycle ?? lastCycleIndex;
                query = query.Where(c => (c.Index - first + 1) % parameters.EveryNthCycle.Value == 0 || c.Index == first || c.Index == last || c.Index == lastCycleIndex);
            }

            if (parameters.CustomCycleFilter != null)
                query = query.Where(c => parameters.CustomCycleFilter.Contains(c.Index));

            return query;
        }

        private static IEnumerable<Point> ApplyYFilter(List<Point> points, Parameters parameters)
        {
            var minY = parameters.MinY;
            if (minY != null)
                points = points.Where(p => p.Y >= minY.Value).ToList();

            var maxY = parameters.MaxY;
            if (maxY != null)
                points = points.Where(p => p.Y <= maxY.Value).ToList();

            return points;
        }

        private static List<Point> Plot(List<Cycle> cycles, Parameters parameters, Func<Cycle, double?> getValue, Func<Cycle, double?> getStdDevValue)
        {
            List<Point> points = FilterCycles(cycles, parameters)
                .Select(c =>
                    new
                    {
                        c.Index,
                        Value = getValue(c),
                        HighError = getStdDevValue(c).HasValue ? getValue(c) + getStdDevValue(c) : null,
                        LowError = getStdDevValue(c).HasValue ? getValue(c) - getStdDevValue(c) : null,
                    })
                .Where(c => c.Value != null)
                .Select(c =>
                    new Point
                    {
                        X = c.Index,
                        Y = c.Value.Value,
                        HighError = c.HighError,
                        LowError = c.LowError
                    }).ToList();

            points = ApplyYFilter(points, parameters).ToList();
            return points.ToList();
        }

        private static List<Point> Plot(List<Cycle> cycles, Parameters parameters, Func<Cycle, double?> getValue)
        {
            List<Point> points = FilterCycles(cycles, parameters)
                .Select(c =>
                    new
                    {
                        c.Index,
                        Value = getValue(c)
                    })
                .Where(c => c.Value != null)
                .Select(c =>
                    new Point
                    {
                        X = c.Index,
                        Y = c.Value.Value
                    }).ToList();

            points = ApplyYFilter(points, parameters).ToList();
            return points.ToList();
        }

        private static IEnumerable<Point> Plot(List<DataPoint> points, Cycle cycle, Func<DataPoint, double?> getX, Func<DataPoint, double?> getY, Parameters parameters, Func<DataPoint, bool> filter = null)
        {
            return ApplyYFilter(Plot(points, cycle.FirstPointIndex, cycle.PointCount, getX, getY, filter).ToList(), parameters);
        }

        private static IEnumerable<Point> Plot(List<DataPoint> points, int offset, int count, Func<DataPoint, double?> getX, Func<DataPoint, double?> getY, Func<DataPoint, bool> filter = null)
        {
            for (var i = offset; i < offset + count; i++)
            {
                if (filter != null && !filter(points[i]))
                    continue;

                var x = getX(points[i]);
                if (x == null)
                    continue;

                var y = getY(points[i]);
                if (y == null)
                    continue;

                yield return new Point
                {
                    X = x.Value,
                    Y = y.Value
                };
            }
        }

        private static IEnumerable<Point> PlotDerivative(List<DataPoint> points, int offset, int count, Func<DataPoint, bool> filter, Func<DataPoint, double?> getX, Func<DataPoint, double?> getY, Parameters parameters)
        {
            var prevX = (Double?)null;
            var prevY = (Double?)null;

            for (var i = offset; i < offset + count; i++)
            {
                if (!filter(points[i]))
                    continue;

                var currX = getX(points[i]);
                if (currX == null)
                    continue;

                var currY = getY(points[i]);
                if (currY == null)
                    continue;

                if (prevX == null)
                {
                    prevX = currX;
                    prevY = currY;
                    continue;
                }

                var dX = currX.Value - prevX.Value;
                if (parameters.Threshold != null && Math.Abs(dX) < parameters.Threshold)
                    continue;

                var dY = currY.Value - prevY.Value;
                var value = dY / dX;
                if (double.IsNaN(value) || double.IsInfinity(value))
                    continue;

                prevX = currX;
                prevY = currY;

                if (parameters.MinY != null && value < parameters.MinY.Value)
                    continue;

                if (parameters.MaxY != null && value > parameters.MaxY.Value)
                    continue;

                yield return new Point
                {
                    X = prevX.Value,
                    Y = value
                };
            }
        }

        private static void AddSeries(Chart chart, Project project, Parameters parameters, List<Point> points, string name, int? cycleIndex = null, bool isZAxis = false)
        {
            var displayName = $"{project.Name}: {name}";
            if (cycleIndex != null)
                displayName += $" (Cycle {cycleIndex})";

            if (points != null && (parameters.Simplification>0 || parameters.MaxPointsPerSeries != null))
                points = new LineSimplifier().Simplify(points, parameters.MaxPointsPerSeries ?? 1000);

            chart.Series.Add(
                new Series
                {
                    ProjectId = project.Id,
                    Name = name,
                    IsZAxis = isZAxis,
                    CycleIndex = cycleIndex,
                    DisplayName = displayName,
                    Points = points ?? NoPoints
                });
        }

        #endregion

        #region Plot2Y

        private static List<Point2Y> Plot2Y(List<Cycle> cycles, Parameters parameters, bool is2Y, Func<Cycle, double?[]> getValue)
        {
            var points = is2Y ? FilterCycles(cycles, parameters)
                .Select(c =>
                    new
                    {
                        c.Index,
                        Value1 = getValue(c)[0],
                        Value2 = getValue(c)[1]
                    })
                .Where(c => c.Value1 != null && c.Value2 != null)
                .Select(c =>
                    new Point2Y
                    {
                        X = c.Index,
                        Y1 = c.Value1.Value,
                        Y2 = c.Value2.Value
                    }) : FilterCycles(cycles, parameters)
                .Select(c =>
                    new
                    {
                        c.Index,
                        Value1 = getValue(c)[0],
                        Value2 = (double?)null
                    })
                .Where(c => c.Value1 != null)
                .Select(c =>
                    new Point2Y
                    {
                        X = c.Index,
                        Y1 = c.Value1.Value,
                        Y2 = c.Value2 == null ? 0 : c.Value2.Value
                    });

            points = ApplyYFilter(points.ToList(), parameters);
            return points.ToList();
        }

        private static IEnumerable<Point2Y> Plot2Y(List<DataPoint> points, bool is2Y, Cycle cycle, Func<DataPoint, double?> getX, Func<DataPoint, double?[]> getY, Parameters parameters, Func<DataPoint, bool> filter = null)
        {
            return ApplyYFilter(Plot2Y(points, is2Y, cycle.FirstPointIndex, cycle.PointCount, getX, getY, filter).ToList(), parameters, is2Y);
        }

        private static IEnumerable<Point2Y> Plot2Y(List<DataPoint> points, bool is2Y, int offset, int count, Func<DataPoint, double?> getX, Func<DataPoint, double?[]> getY, Func<DataPoint, bool> filter = null)
        {
            for (var i = offset; i < offset + count; i++)
            {
                if (filter != null && !filter(points[i]))
                    continue;

                var x = getX(points[i]);
                if (x == null)
                    continue;
                if (is2Y)
                {
                    var y1 = getY(points[i])[0];
                    var y2 = getY(points[i])[1];
                    if (y1 == null || y2 == null)
                        continue;

                    yield return new Point2Y
                    {
                        X = x.Value,
                        Y1 = y1.Value,
                        Y2 = y2.Value
                    };
                }
                else
                {
                    var y1 = getY(points[i])[0];

                    if (y1 == null)
                        continue;

                    yield return new Point2Y
                    {
                        X = x.Value,
                        Y1 = y1.Value
                    };
                }
            }
        }

        private static IEnumerable<Point2Y> PlotDerivative2Y(
            List<DataPoint> points,
            bool is2Y,
            int offset,
            int count,
            Func<DataPoint, bool> filter,
            Func<DataPoint, double?> getX,
            Func<DataPoint, double?[]> getY,
            Parameters parameters)
        {
            var prevX = (Double?)null;
            var prevY1 = (Double?)null;
            var prevY2 = (Double?)null;
            if (is2Y)
            {
                for (var i = offset; i < offset + count; i++)
                {
                    if (!filter(points[i]))
                        continue;

                    var currX = getX(points[i]);
                    if (currX == null)
                        continue;

                    var currY1 = getY(points[i])[0];
                    var currY2 = getY(points[i])[1];
                    if (currY1 == null || currY2 == null)
                        continue;

                    if (prevX == null)
                    {
                        prevX = currX;
                        prevY1 = currY1;
                        prevY2 = currY2;
                        continue;
                    }

                    var dX = currX.Value - prevX.Value;
                    if (parameters.Threshold != null && Math.Abs(dX) < parameters.Threshold)
                        continue;

                    var dY1 = currY1.Value - prevY1.Value;
                    var value1 = dY1 / dX;
                    var dY2 = currY2.Value - prevY2.Value;
                    var value2 = dY2 / dX;
                    if (double.IsNaN(value1) || double.IsInfinity(value1))
                        continue;
                    if (double.IsNaN(value2) || double.IsInfinity(value2))
                        continue;

                    prevX = currX;
                    prevY1 = currY1;
                    prevY2 = currY2;
                    if (parameters.MinY != null && value1 < parameters.MinY.Value)
                        continue;

                    if (parameters.MaxY != null && value1 > parameters.MaxY.Value)
                        continue;

                    if (parameters.MinY2 != null && value2 < parameters.MinY2.Value)
                        continue;

                    if (parameters.MaxY2 != null && value2 > parameters.MaxY2.Value)
                        continue;

                    yield return new Point2Y
                    {
                        X = prevX.Value,
                        Y1 = value1,
                        Y2 = value2
                    };
                }
            }
            else
            {
                for (var i = offset; i < offset + count; i++)
                {
                    if (!filter(points[i]))
                        continue;

                    var currX = getX(points[i]);
                    if (currX == null)
                        continue;

                    var currY1 = getY(points[i])[0];

                    if (currY1 == null)
                        continue;

                    if (prevX == null)
                    {
                        prevX = currX;
                        prevY1 = currY1;
                        continue;
                    }

                    var dX = currX.Value - prevX.Value;
                    if (parameters.Threshold != null && Math.Abs(dX) < parameters.Threshold)
                        continue;

                    var dY1 = currY1.Value - prevY1.Value;
                    var value1 = dY1 / dX;

                    if (double.IsNaN(value1) || double.IsInfinity(value1))
                        continue;

                    prevX = currX;
                    prevY1 = currY1;

                    if (parameters.MinY != null && value1 < parameters.MinY.Value)
                        continue;

                    if (parameters.MaxY != null && value1 > parameters.MaxY.Value)
                        continue;

                    yield return new Point2Y
                    {
                        X = prevX.Value,
                        Y1 = value1
                    };
                }
            }
        }

        private static IEnumerable<Point2Y> ApplyYFilter(List<Point2Y> points, Parameters parameters, bool is2Y = false)
        {
            var minY1 = parameters.MinY;
            var minY2 = is2Y ? parameters.MinY2 : (double?)null;
            if (is2Y)
            {
                if (minY1 != null && minY2 != null)
                    points = points.Where(p => p.Y1 >= minY1.Value && p.Y2 >= minY2.Value).ToList();

                var maxY1 = parameters.MaxY;
                var maxY2 = parameters.MaxY2;
                if (maxY1 != null && maxY2 != null)
                    points = points.Where(p => p.Y1 <= maxY1.Value && p.Y2 <= maxY2.Value).ToList();
            }
            else
            {
                if (minY1 != null)
                    points = points.Where(p => p.Y1 >= minY1.Value).ToList();

                var maxY1 = parameters.MaxY;

                if (maxY1 != null)
                    points = points.Where(p => p.Y1 <= maxY1.Value).ToList();
            }
            return points;
        }     
        #endregion
    }
}
