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
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Dqdv.Types;
using Dqdv.Types.Plot;
using Plotting.UnitTransformation;

namespace Plotting
{
    public class TemplatePlotter : PlotterBase
    {
        public static double Tolerance = .00001;
        private static readonly Foo _foo = new Foo();

        public TemplatePlotter(IProjectDataRepository projectDataRepository) : base(projectDataRepository)
        {
        }

        public Chart Plot(PlotTemplate plotTemplate, PlotParameters parameters, IList<DataLayer.Project> projects, string trace)
        {
            int? forcedEveryNthCycle = CalcForcedEveryNthCycle(plotTemplate, projects, parameters);
            ChartSettings = MakeParameters(parameters, forcedEveryNthCycle);
            Chart = CreateChart(plotTemplate);
            Chart.ForcedEveryNthCycle = forcedEveryNthCycle;

            foreach (DataLayer.Project project in projects)
            {
                Plot(project.Id, plotTemplate, trace);
            }

            return Chart;
        }

        ////////////////////////////////////////////////////////////
        // Private Methods/Atributes
        ////////////////////////////////////////////////////////////

        private Chart Chart { get; set; }

        private int? CalcForcedEveryNthCycle(PlotTemplate plotTemplate, 
            IList<DataLayer.Project> projects, 
            PlotParameters parameters)
        {
            if (plotTemplate.UseAgregateData)
            {
                return null;
            }

            var maxCycles = parameters?.MaxCycles ?? 0;
            if (maxCycles <= 0)
            {
                return null;
            }

            int maxCyclesPerProject = Math.Max(maxCycles / projects.Count - 1, 1);
            int forcedEveryNthCycle = int.MinValue;
            foreach (DataLayer.Project project in projects)
            {
                int cyclesCount = project.NumCycles;

                if (!string.IsNullOrEmpty(parameters?.CustomCycleFilter))
                {
                    var rangeFilter = new IndexRangeFilter(parameters.CustomCycleFilter).RangesItems;
                    cyclesCount = rangeFilter.Count;
                    var result = cyclesCount / maxCyclesPerProject;
                    if (cyclesCount % maxCyclesPerProject != 0)
                    {
                        result += 1;
                    }
                    if (forcedEveryNthCycle < result)
                    {
                        forcedEveryNthCycle = result;
                    }
                }
                else
                {
                    int fromCycle = Math.Max(parameters?.FromCycle ?? 1, 1);
                    int toCycle = Math.Min(parameters?.ToCycle ?? cyclesCount, cyclesCount);
                    cyclesCount = toCycle - fromCycle + 1;
                    int result = cyclesCount / maxCyclesPerProject;
                    if (cyclesCount % maxCyclesPerProject != 0)
                    {
                        result += 1;
                    }
                    if (forcedEveryNthCycle < result)
                    {
                        forcedEveryNthCycle = result;
                    }
                }
            }

            if (forcedEveryNthCycle < 2)
            {
                return null;
            }

            if (parameters?.EveryNthCycle != null && parameters.EveryNthCycle.Value >= forcedEveryNthCycle)
            {
                return null;
            }

            return forcedEveryNthCycle;
        }

        private Chart CreateChart(PlotTemplate plotTemplate)
        {
            var chart = new Chart
            {
                Projects = new List<Project>(),
                Series = new List<Series>(),
                Label = new Label {Font = new Font(FontFamily.GenericMonospace, 8)},
                XAxisText = plotTemplate.xAxis.Title,
                XAxisIsInteger = plotTemplate.xAxis.Title.Contains("Cycle"),
                YAxisText = plotTemplate.yAxis.Select(t => t.Title).ToArray()
            };

            return chart;
        }

        private void Plot(int projectId, PlotTemplate plotTemplate, string trace)
        {
            var project = ProjectDataRepository.GetProject(projectId, trace);
            Chart.Projects.Add(project);

            PlotsByTemplate(project, plotTemplate, trace);
        }

        private void PlotsByTemplate(Project project, PlotTemplate plotTemplate, string trace)
        {
            var allPoints = ProjectDataRepository.GetPoints(project.Id, trace);
            var cycles = ProjectDataRepository.GetCycles(project.Id, trace);

            plotTemplate.CheckChargeDischarge = ChartSettings.IsChargeEnabled || ChartSettings.IsDischargeEnabled;

            GetChartByYzAxis(
                project, 
                cycles,
                plotTemplate.CheckChargeDischarge, 
                plotTemplate.UseCycleData, 
                plotTemplate.UseFirstCycle, 
                plotTemplate.UseAgregateData, 
                plotTemplate.UseCRateCalculation,
                plotTemplate.UseDischargeCRateCalculation, 
                plotTemplate.xAxis,
                plotTemplate.yAxis, 
                allPoints);
        }


        private void AddSeriesToValueAxis(
            Func<string, List<Point>> plotFunction, 
            Func<string, PropertyDescriptor> propertyDescriptorFunc,
            Func<string, string> seriesNameFormatter,
            Project project,
            SeriesTemplate ySerie,
            bool isSecondaryAxis)
        {
            foreach (var argumentName in ySerie.Numerator.Arg1.Arg.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var descriptor = propertyDescriptorFunc(argumentName);
                if (descriptor == null || (!ChartSettings.IsChargeEnabled && descriptor.IsChargeStep) || (!ChartSettings.IsDischargeEnabled && descriptor.IsDischargeStep))
                    continue;

                var pointsData = plotFunction(argumentName);
                AddSeries(Chart, project, ChartSettings, pointsData, seriesNameFormatter(argumentName), isZAxis: isSecondaryAxis);
            }
        } 

        private void GetChartByYzAxis( 
            Project project, 
            List<Cycle> cycles, 
            bool checkChargeDischarge, 
            bool useCycleData, 
            bool useFirstCycle, 
            bool useAgregateData, 
            bool useCRateCalculation,
            bool useDischargeCRateCalculation,
            SeriesTemplate xSeriesTemplate, 
            List<SeriesTemplate> ySeriesTemplate, 
            List<DataPoint> allPoints)
        {
            bool is2Y = ySeriesTemplate.Count > 1;
            //var multiplierY = GetMultiplier(project, ChartSettings, ySeriesTemplate[0].Numerator.Arg1.MultiplierType);
            //var multiplierZ = is2Y ? GetMultiplier(project, parameters, ySeriesTemplate[1].Numerator.Arg1.MultiplierType) : 1;

            if (useCycleData)
            {
                //Any charge or discharge C-Rate trigger will work, but main logic which current to pick is inside
                if (useCRateCalculation || useDischargeCRateCalculation)
                {
                    AddSeriesToValueAxis(
                        item => PlotCRate(project, cycles, c => GetNormalizedCyclePropValue(item, project, c, ChartSettings), useDischargeCRateCalculation), 
                        GetCyclePropertyDescriptor,
                        item => item,
                        project, 
                        ySeriesTemplate[0], 
                        false);

                    if (is2Y)
                    {
                        AddSeriesToValueAxis(
                            item => PlotCRate(project, cycles, c => GetNormalizedCyclePropValue(item, project, c, ChartSettings), useDischargeCRateCalculation),
                            GetCyclePropertyDescriptor,
                            item => item,
                            project,
                            ySeriesTemplate[1],
                            true);
                    }
                }
                else
                {
                    AddSeriesToValueAxis(
                        item => Plot(cycles, c => GetNormalizedCyclePropValue(item, project, c, ChartSettings)),
                        GetCyclePropertyDescriptor,
                        item => item,
                        project, 
                        ySeriesTemplate[0], 
                        false);
                    if (is2Y)
                    {
                        AddSeriesToValueAxis(
                            item => Plot(cycles, c => GetNormalizedCyclePropValue(item, project, c, ChartSettings)),
                            GetCyclePropertyDescriptor,
                            item => item,
                            project, 
                            ySeriesTemplate[1], 
                            true);
                    }
                }

                Chart.XAxisText = GetLabel(GetCyclePropertyDescriptor, xSeriesTemplate.Numerator.Arg1);
                Chart.YAxisText = new[] { GetLabel(GetCyclePropertyDescriptor, ySeriesTemplate[0].Numerator.Arg1) };
                if (is2Y)
                {
                    Chart.YAxisText = new[] { Chart.YAxisText[0], GetLabel(GetCyclePropertyDescriptor, ySeriesTemplate[1].Numerator.Arg1) };
                }

                return;
            }


            if (checkChargeDischarge)
            {
                if (useFirstCycle)
                {
                    foreach (var cycle in FilterCycles(cycles, ChartSettings))
                    {
                        if (ChartSettings.IsChargeEnabled)
                        {
                            AddSeriesToValueAxis(
                                item => PlotDerivative(allPoints, cycle.FirstPointIndex, cycle.PointCount,
                                            p => p.CycleStep == CycleStep.ChargeCC || p.CycleStep == CycleStep.ChargeCV,
                                            p => GetNormalizedDataPointPropValue(xSeriesTemplate.Numerator.Arg1.Arg, project, p, ChartSettings),
                                            p => GetNormalizedDataPointPropValue(item, project, p, ChartSettings), ChartSettings).ToList(),
                                GetDataPointPropertyDescriptor,
                                item => $"{ySeriesTemplate[0].Title} {Title.Charge} (Cycle {cycle.Index})",
                                project,
                                ySeriesTemplate[0],
                                false);

                            if (is2Y)
                            {
                                AddSeriesToValueAxis(
                                    item => PlotDerivative(allPoints, cycle.FirstPointIndex, cycle.PointCount,
                                        p => p.CycleStep == CycleStep.ChargeCC || p.CycleStep == CycleStep.ChargeCV,
                                        p => GetNormalizedDataPointPropValue(xSeriesTemplate.Numerator.Arg1.Arg, project, p, ChartSettings),
                                        p => GetNormalizedDataPointPropValue(item, project, p, ChartSettings), ChartSettings).ToList(),
                                    GetDataPointPropertyDescriptor,
                                    item => item,
                                    project,
                                    ySeriesTemplate[1],
                                    false);
                            }
                        }

                        if (ChartSettings.IsDischargeEnabled)
                        {
                            AddSeriesToValueAxis(
                                item => PlotDerivative(allPoints, cycle.FirstPointIndex, cycle.PointCount,
                                    p => p.CycleStep == CycleStep.Discharge,
                                    p => GetNormalizedDataPointPropValue(xSeriesTemplate.Numerator.Arg1.Arg, project, p, ChartSettings),
                                    p => GetNormalizedDataPointPropValue(item, project, p, ChartSettings), ChartSettings).ToList(),
                                GetDataPointPropertyDescriptor,
                                item => $"{ySeriesTemplate[0].Title} {Title.Discharge} (Cycle {cycle.Index})",
                                project,
                                ySeriesTemplate[0],
                                false);

                            if (is2Y)
                            {
                                var pointsY2 = PlotDerivative(allPoints, cycle.FirstPointIndex, cycle.PointCount,
                                    p => p.CycleStep == CycleStep.Discharge,
                                    p => GetNormalizedDataPointPropValue(xSeriesTemplate.Numerator.Arg1.Arg, project, p, ChartSettings),
                                    p => GetNormalizedDataPointPropValue(ySeriesTemplate[1].Numerator.Arg1.Arg, project, p, ChartSettings),
                                    ChartSettings).ToList();
                                AddSeries(Chart, project, ChartSettings, pointsY2, $"{ySeriesTemplate[1].Title} {Title.Discharge}", cycle.Index, isZAxis: true);
                            }
                        }
                    }

                    Chart.XAxisText = GetLabel(GetDataPointPropertyDescriptor, xSeriesTemplate.Numerator.Arg1);
                    //chart.YAxisText = new[] { GetLabel(GetDataPointPropertyDescriptor(ySeriesTemplate[0].Numerator.Arg1), parameters) };
                    if (is2Y)
                    {
                        Chart.YAxisText = new[] { Chart.YAxisText[0], GetLabel(GetDataPointPropertyDescriptor, ySeriesTemplate[1].Numerator.Arg1) };
                    }
                }
                else
                if (useAgregateData)
                {
                    //var pointsY1 = FilterCycles(cycles, parameters)
                    //    .SelectMany(c => Plot(allPoints,
                    //        c,
                    //        p => xSeriesTemplate.Numerator.Arg1.MultiplierType == MultiplierType.None ?
                    //            GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg) :
                    //            multiplierY * GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg),
                    //        p => ySeriesTemplate[0].Numerator.Arg1.MultiplierType == MultiplierType.None ?
                    //            GetPropValue(p, ySeriesTemplate[0].Numerator.Arg1.Arg) :
                    //            multiplierY * GetPropValue(p, ySeriesTemplate[0].Numerator.Arg1.Arg), parameters)).ToList();
                    var pointsY1 = FilterCycles(cycles, ChartSettings)
                        .SelectMany(c => Plot(allPoints, c, 
                                            p => GetNormalizedDataPointPropValue(xSeriesTemplate.Numerator.Arg1.Arg, project, p, ChartSettings), 
                                            p => GetNormalizedDataPointPropValue(ySeriesTemplate[0].Numerator.Arg1.Arg, project, p, ChartSettings), 
                                            ChartSettings)
                        ).ToList();

                    AddSeries(Chart, project, ChartSettings, pointsY1, ySeriesTemplate[0].Title);
                    if (is2Y)
                    {
                        //var pointsY2 = FilterCycles(cycles, parameters)
                        //    .SelectMany(c => Plot(allPoints,
                        //        c,
                        //        p => xSeriesTemplate.Numerator.Arg1.MultiplierType == MultiplierType.None ?
                        //            GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg) :
                        //            multiplierY * GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg),
                        //        p => ySeriesTemplate[1].Numerator.Arg1.MultiplierType == MultiplierType.None ?
                        //            GetPropValue(p, ySeriesTemplate[1].Numerator.Arg1.Arg) :
                        //            multiplierY * GetPropValue(p, ySeriesTemplate[1].Numerator.Arg1.Arg), parameters)).ToList();
                        var pointsY2 = FilterCycles(cycles, ChartSettings)
                            .SelectMany(c => Plot(allPoints,
                                c,
                                p => GetNormalizedDataPointPropValue(xSeriesTemplate.Numerator.Arg1.Arg, project, p, ChartSettings),
                                p => GetNormalizedDataPointPropValue(ySeriesTemplate[1].Numerator.Arg1.Arg, project, p, ChartSettings), ChartSettings)).ToList();

                        AddSeries(Chart, project, ChartSettings, pointsY2, ySeriesTemplate[1].Title, isZAxis: true);
                    }

                    Chart.XAxisText = GetLabel(GetDataPointPropertyDescriptor, xSeriesTemplate.Numerator.Arg1);
                    Chart.YAxisText = new[] { GetLabel(GetDataPointPropertyDescriptor, ySeriesTemplate[0].Numerator.Arg1) };
                    if (is2Y)
                    {
                        Chart.YAxisText = new[] { Chart.YAxisText[0], GetLabel(GetDataPointPropertyDescriptor, ySeriesTemplate[1].Numerator.Arg1) };
                    }
                }
                else
                {
                    foreach (var cycle in FilterCycles(cycles, ChartSettings))
                    {
                        if (ChartSettings.IsChargeEnabled)
                        {
                            //var pointsY1 = Plot(allPoints, cycle,                                
                            //    p => xSeriesTemplate.Numerator.Arg1.MultiplierType == MultiplierType.None ?
                            //        GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg) :
                            //        multiplierY * GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg),
                            //    p => ySeriesTemplate[0].Numerator.Arg1.MultiplierType == MultiplierType.None ?
                            //        GetPropValue(p, ySeriesTemplate[0].Numerator.Arg1.Arg) :
                            //        multiplierY * GetPropValue(p, ySeriesTemplate[0].Numerator.Arg1.Arg), parameters,
                            //    p => p.CycleStep == CycleStep.ChargeCC || p.CycleStep == CycleStep.ChargeCV).ToList();

                            var pointsY1 = Plot(allPoints, cycle,
                                p => GetNormalizedDataPointPropValue(xSeriesTemplate.Numerator.Arg1.Arg, project, p, ChartSettings),
                                p => GetNormalizedDataPointPropValue(ySeriesTemplate[0].Numerator.Arg1.Arg, project, p, ChartSettings), ChartSettings,
                                p => p.CycleStep == CycleStep.ChargeCC || p.CycleStep == CycleStep.ChargeCV).ToList();
                            AddSeries(Chart, project, ChartSettings, pointsY1, $"{ySeriesTemplate[0].Title} {Title.Charge}", cycle.Index);

                            if (is2Y)
                            {
                                //var pointsY2 = Plot(allPoints, cycle,                                   
                                //    p => xSeriesTemplate.Numerator.Arg1.MultiplierType == MultiplierType.None ?
                                //        GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg) :
                                //        multiplierY * GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg),
                                //    p => ySeriesTemplate[1].Numerator.Arg1.MultiplierType == MultiplierType.None ?
                                //        GetPropValue(p, ySeriesTemplate[1].Numerator.Arg1.Arg) :
                                //        multiplierY * GetPropValue(p, ySeriesTemplate[1].Numerator.Arg1.Arg), parameters,
                                //         p => p.CycleStep == CycleStep.ChargeCC || p.CycleStep == CycleStep.ChargeCV).ToList();
                                var pointsY2 = Plot(allPoints, cycle,
                                    p => GetNormalizedDataPointPropValue(xSeriesTemplate.Numerator.Arg1.Arg, project, p, ChartSettings),
                                    p => GetNormalizedDataPointPropValue(ySeriesTemplate[1].Numerator.Arg1.Arg, project, p, ChartSettings), ChartSettings,
                                    p => p.CycleStep == CycleStep.ChargeCC || p.CycleStep == CycleStep.ChargeCV).ToList();
                                AddSeries(Chart, project, ChartSettings, pointsY2, $"{ySeriesTemplate[1].Title} {Title.Charge}", cycle.Index, isZAxis: true);
                            }
                        }

                        if (ChartSettings.IsDischargeEnabled)
                        {
                            //var pointsY1 = Plot(allPoints, cycle,
                            //    p => xSeriesTemplate.Numerator.Arg1.MultiplierType == MultiplierType.None ?
                            //        GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg) :
                            //        multiplierY * GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg),
                            //    p => ySeriesTemplate[0].Numerator.Arg1.MultiplierType == MultiplierType.None ?
                            //        GetPropValue(p, ySeriesTemplate[0].Numerator.Arg1.Arg) :
                            //        multiplierY * GetPropValue(p, ySeriesTemplate[0].Numerator.Arg1.Arg), parameters,
                            //    p => p.CycleStep == CycleStep.Discharge).ToList();
                            var pointsY1 = Plot(allPoints, cycle,
                                p => GetNormalizedDataPointPropValue(xSeriesTemplate.Numerator.Arg1.Arg, project, p, ChartSettings),
                                p => GetNormalizedDataPointPropValue(ySeriesTemplate[0].Numerator.Arg1.Arg, project, p, ChartSettings), ChartSettings,
                                p => p.CycleStep == CycleStep.Discharge).ToList();
                            AddSeries(Chart, project, ChartSettings, pointsY1, $"{ySeriesTemplate[0].Title} {Title.Discharge}", cycle.Index);

                            if (is2Y)
                            {
                                //var pointsY2 = Plot(allPoints, cycle,
                                //    p => xSeriesTemplate.Numerator.Arg1.MultiplierType == MultiplierType.None ?
                                //        GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg) :
                                //        multiplierY * GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg),
                                //    p => ySeriesTemplate[1].Numerator.Arg1.MultiplierType == MultiplierType.None ?
                                //        GetPropValue(p, ySeriesTemplate[1].Numerator.Arg1.Arg) :
                                //        multiplierY * GetPropValue(p, ySeriesTemplate[1].Numerator.Arg1.Arg), parameters,
                                //    p => p.CycleStep == CycleStep.Discharge).ToList();
                                var pointsY2 = Plot(allPoints, cycle,
                                    p => GetNormalizedDataPointPropValue(xSeriesTemplate.Numerator.Arg1.Arg, project, p, ChartSettings),
                                    p => GetNormalizedDataPointPropValue(ySeriesTemplate[1].Numerator.Arg1.Arg, project, p, ChartSettings), ChartSettings,
                                    p => p.CycleStep == CycleStep.Discharge).ToList();
                                AddSeries(Chart, project, ChartSettings, pointsY2, $"{ySeriesTemplate[1].Title} {Title.Discharge}", cycle.Index, isZAxis: true);
                            }
                        }
                    }

                    Chart.XAxisText = GetLabel(GetDataPointPropertyDescriptor, xSeriesTemplate.Numerator.Arg1);
                    Chart.YAxisText = new[] { GetLabel(GetDataPointPropertyDescriptor, ySeriesTemplate[0].Numerator.Arg1) };
                    if (is2Y)
                    {
                        Chart.YAxisText = new[] { Chart.YAxisText[0], GetLabel(GetDataPointPropertyDescriptor, ySeriesTemplate[1].Numerator.Arg1) };
                    }
                }
            }
            else
            {
                //if (useCycleData)
                //{
                //    var pointsData = Plot(cycles, parameters, c => GetNormalizedCyclePropValue(ySeriesTemplate[0].Numerator.Arg1.Arg, project, c, parameters));
                //    AddSeries(chart, project, parameters, pointsData, ySeriesTemplate[0].Title);
                //    if (is2Y)
                //    {
                //        pointsData = Plot(cycles, parameters, c => GetNormalizedCyclePropValue(ySeriesTemplate[1].Numerator.Arg1.Arg, project, c, parameters));
                //        AddSeries(chart, project, parameters, pointsData, ySeriesTemplate[1].Title, isZAxis: true);
                //    }

                //    chart.XAxisText = GetLabel(GetCyclePropertyDescriptor(xSeriesTemplate.Numerator.Arg1.Arg), parameters);
                //    chart.YAxisText = new[] { GetLabel(GetCyclePropertyDescriptor(ySeriesTemplate[0].Numerator.Arg1.Arg), parameters) };
                //    if (is2Y)
                //    {
                //        chart.YAxisText = new[] { chart.YAxisText[0], GetLabel(GetCyclePropertyDescriptor(ySeriesTemplate[1].Numerator.Arg1.Arg), parameters) };
                //    }
                //}
                //else
                if (useAgregateData)
                {
                    //var pointsY1 = FilterCycles(cycles, parameters)
                    //    .SelectMany(c => Plot(allPoints,
                    //        c,
                    //        p => xSeriesTemplate.Numerator.Arg1.MultiplierType == MultiplierType.None ?
                    //            GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg) :
                    //            multiplierY * GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg),
                    //        p => ySeriesTemplate[0].Numerator.Arg1.MultiplierType == MultiplierType.None ?
                    //            GetPropValue(p, ySeriesTemplate[0].Numerator.Arg1.Arg) :
                    //            multiplierY * GetPropValue(p, ySeriesTemplate[0].Numerator.Arg1.Arg), parameters)).ToList();

                    var pointsY1 = FilterCycles(cycles, ChartSettings)
                        .SelectMany(c => Plot(allPoints,
                            c,
                            p => GetNormalizedDataPointPropValue(xSeriesTemplate.Numerator.Arg1.Arg, project, p, ChartSettings),
                            p => GetNormalizedDataPointPropValue(ySeriesTemplate[0].Numerator.Arg1.Arg, project, p, ChartSettings),
                            ChartSettings)).ToList();
                    AddSeries(Chart, project, ChartSettings, pointsY1, ySeriesTemplate[0].Title);
                    if (is2Y)
                    {
                        //var pointsY2 = FilterCycles(cycles, parameters)
                        //    .SelectMany(c => Plot(allPoints,
                        //        c,
                        //        p => xSeriesTemplate.Numerator.Arg1.MultiplierType == MultiplierType.None ?
                        //            GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg) :
                        //            multiplierY * GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg),
                        //        p => ySeriesTemplate[1].Numerator.Arg1.MultiplierType == MultiplierType.None ?
                        //            GetPropValue(p, ySeriesTemplate[1].Numerator.Arg1.Arg) :
                        //            multiplierY * GetPropValue(p, ySeriesTemplate[1].Numerator.Arg1.Arg), parameters)).ToList();
                        var pointsY2 = FilterCycles(cycles, ChartSettings)
                            .SelectMany(c => Plot(allPoints,
                                c,
                                p => GetNormalizedDataPointPropValue(xSeriesTemplate.Numerator.Arg1.Arg, project, p, ChartSettings),
                                p => GetNormalizedDataPointPropValue(ySeriesTemplate[1].Numerator.Arg1.Arg, project, p, ChartSettings),
                                ChartSettings)).ToList();
                        AddSeries(Chart, project, ChartSettings, pointsY2, ySeriesTemplate[1].Title, isZAxis: true);
                    }

                    Chart.XAxisText = GetLabel(GetDataPointPropertyDescriptor, xSeriesTemplate.Numerator.Arg1);
                    Chart.YAxisText = new[] { GetLabel(GetDataPointPropertyDescriptor, ySeriesTemplate[0].Numerator.Arg1) };
                    if (is2Y)
                    {
                        Chart.YAxisText = new[] { Chart.YAxisText[0], GetLabel(GetDataPointPropertyDescriptor, ySeriesTemplate[1].Numerator.Arg1) };
                    }
                }
                else
                {
                    foreach (var cycle in FilterCycles(cycles, ChartSettings))
                    {
                        if (ChartSettings.IsChargeEnabled)
                        {
                            //var pointsY1 = PlotDerivative(allPoints, cycle.FirstPointIndex, cycle.PointCount,
                            //    p => p.CycleStep == CycleStep.ChargeCC || p.CycleStep == CycleStep.ChargeCV,
                            //    p => xSeriesTemplate.Numerator.Arg1.MultiplierType == MultiplierType.None ?
                            //        GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg) :
                            //        multiplierY * GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg),
                            //    p => ySeriesTemplate[0].Numerator.Arg1.MultiplierType == MultiplierType.None ?
                            //        GetPropValue(p, ySeriesTemplate[0].Numerator.Arg1.Arg) :
                            //        multiplierY * GetPropValue(p, ySeriesTemplate[0].Numerator.Arg1.Arg), parameters).ToList();
                            var pointsY1 = PlotDerivative(allPoints, cycle.FirstPointIndex, cycle.PointCount,
                                p => p.CycleStep == CycleStep.ChargeCC || p.CycleStep == CycleStep.ChargeCV,
                                p => GetNormalizedDataPointPropValue(xSeriesTemplate.Numerator.Arg1.Arg, project, p, ChartSettings),
                                p => GetNormalizedDataPointPropValue(ySeriesTemplate[0].Numerator.Arg1.Arg, project, p, ChartSettings),
                                ChartSettings).ToList();
                            AddSeries(Chart, project, ChartSettings, pointsY1, $"{ySeriesTemplate[0].Title} {Title.Charge}", cycle.Index);

                            if (is2Y)
                            {
                                //var pointsY2 = PlotDerivative(allPoints, cycle.FirstPointIndex, cycle.PointCount,
                                //    p => p.CycleStep == CycleStep.ChargeCC || p.CycleStep == CycleStep.ChargeCV,
                                //    p => xSeriesTemplate.Numerator.Arg1.MultiplierType == MultiplierType.None ?
                                //        GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg) :
                                //        multiplierY * GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg),
                                //    p => ySeriesTemplate[1].Numerator.Arg1.MultiplierType == MultiplierType.None ?
                                //        GetPropValue(p, ySeriesTemplate[1].Numerator.Arg1.Arg) :
                                //        multiplierY * GetPropValue(p, ySeriesTemplate[1].Numerator.Arg1.Arg), parameters).ToList();
                                var pointsY2 = PlotDerivative(allPoints, cycle.FirstPointIndex, cycle.PointCount,
                                    p => p.CycleStep == CycleStep.ChargeCC || p.CycleStep == CycleStep.ChargeCV,
                                    p => GetNormalizedDataPointPropValue(xSeriesTemplate.Numerator.Arg1.Arg, project, p, ChartSettings),
                                    p => GetNormalizedDataPointPropValue(ySeriesTemplate[1].Numerator.Arg1.Arg, project, p, ChartSettings),
                                    ChartSettings).ToList();
                                AddSeries(Chart, project, ChartSettings, pointsY2, $"{ySeriesTemplate[1].Title} {Title.Charge}", cycle.Index, isZAxis: true);
                            }
                        }

                        if (ChartSettings.IsDischargeEnabled)
                        {
                            //var pointsY1 = PlotDerivative(allPoints, cycle.FirstPointIndex, cycle.PointCount,
                            //    p => p.CycleStep == CycleStep.Discharge,
                            //    p => xSeriesTemplate.Numerator.Arg1.MultiplierType == MultiplierType.None ?
                            //        GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg) :
                            //        multiplierY * GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg),
                            //    p => ySeriesTemplate[0].Numerator.Arg1.MultiplierType == MultiplierType.None ?
                            //        GetPropValue(p, ySeriesTemplate[0].Numerator.Arg1.Arg) :
                            //        multiplierY * GetPropValue(p, ySeriesTemplate[0].Numerator.Arg1.Arg), parameters).ToList();
                            var pointsY1 = PlotDerivative(allPoints, cycle.FirstPointIndex, cycle.PointCount,
                                p => p.CycleStep == CycleStep.Discharge,
                                p => GetNormalizedDataPointPropValue(xSeriesTemplate.Numerator.Arg1.Arg, project, p, ChartSettings),
                                p => GetNormalizedDataPointPropValue(ySeriesTemplate[0].Numerator.Arg1.Arg, project, p, ChartSettings),
                                ChartSettings).ToList();
                            AddSeries(Chart, project, ChartSettings, pointsY1, $"{ySeriesTemplate[0].Title} {Title.Discharge}", cycle.Index);

                            if (is2Y)
                            {
                                //var pointsY2 = PlotDerivative(allPoints, cycle.FirstPointIndex, cycle.PointCount,
                                //    p => p.CycleStep == CycleStep.Discharge,
                                //    p => xSeriesTemplate.Numerator.Arg1.MultiplierType == MultiplierType.None ?
                                //        GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg) :
                                //        multiplierY * GetPropValue(p, xSeriesTemplate.Numerator.Arg1.Arg),
                                //    p => ySeriesTemplate[1].Numerator.Arg1.MultiplierType == MultiplierType.None ?
                                //        GetPropValue(p, ySeriesTemplate[1].Numerator.Arg1.Arg) :
                                //        multiplierY * GetPropValue(p, ySeriesTemplate[1].Numerator.Arg1.Arg), parameters).ToList();
                                var pointsY2 = PlotDerivative(allPoints, cycle.FirstPointIndex, cycle.PointCount,
                                    p => p.CycleStep == CycleStep.Discharge,
                                    p => GetNormalizedDataPointPropValue(xSeriesTemplate.Numerator.Arg1.Arg, project, p, ChartSettings),
                                    p => GetNormalizedDataPointPropValue(ySeriesTemplate[1].Numerator.Arg1.Arg, project, p, ChartSettings),
                                    ChartSettings).ToList();
                                AddSeries(Chart, project, ChartSettings, pointsY2, $"{ySeriesTemplate[1].Title} {Title.Discharge}", cycle.Index, isZAxis: true);
                            }
                        }
                    }

                    Chart.XAxisText = GetLabel(GetDataPointPropertyDescriptor, xSeriesTemplate.Numerator.Arg1);
                    Chart.YAxisText = new[] { GetLabel(GetDataPointPropertyDescriptor, ySeriesTemplate[0].Numerator.Arg1) };
                    if (is2Y)
                    {
                        Chart.YAxisText = new[] { Chart.YAxisText[0], GetLabel(GetDataPointPropertyDescriptor, ySeriesTemplate[1].Numerator.Arg1) };
                    }
                }
            }
        }

        public static double CalculateCRate(double activeMaterialFraction,
            double theoreticalCapacity,
            double mass,
            double? current)
        {
            double cConst = activeMaterialFraction * theoreticalCapacity * mass;

            if (Math.Abs(cConst) < Tolerance)
            {
                //avoid division by 0
                return 0;
            }

            double cRate = Math.Abs(current.GetValueOrDefault(0)) / cConst;

            //https://stackoverflow.com/questions/977796/why-does-math-round2-5-return-2-instead-of-3
            return cRate >= 1 ? Math.Round(cRate, 0, MidpointRounding.AwayFromZero) : Math.Round(cRate, 2);
        }

        private List<Point> PlotCRate(Project project, 
            List<Cycle> cycles, 
            Func<Cycle, double?> getValue,
            bool useDischargeCRateCalculation)
        {
            var points = FilterCycles(cycles, ChartSettings)
                .Select(c =>
                    new
                    {
                        c.Index,
                        Value = getValue(c) / project.ActiveMaterialFraction.GetValueOrDefault(1) / project.Mass.GetValueOrDefault(1),
                        Discrete = CalculateCRate(
                            project.ActiveMaterialFraction.GetValueOrDefault(1),
                            project.TheoreticalCapacity.GetValueOrDefault(1),
                            project.Mass.GetValueOrDefault(1),
                            useDischargeCRateCalculation ? c.StartDischargeCurrent : c.StartCurrent)
                    })
                .Select(c =>
                    new Point
                    {
                        X = c.Index,
                        // we can't omit null values, because export may shifts rows when chart has more than 1 serie 
                        // s[0]{X=1,Y=1.1}, s[0]{X=2,Y=1.2}, s[0]{X=3,Y=1.3}
                        // s[1]{X=2,Y=2.1}, s[1]{X=3,Y=2.3}
                        // will be exported as 
                        // 1  1.1 2.1
                        // 2  1.2 2.3
                        // 3  1.3
                        // so set to NaN and post process later
                        Y = c.Value.GetValueOrDefault(double.NaN), 
                        Discrete = c.Discrete
                    }).ToList();

            return ApplyYFilter(points, ChartSettings).ToList();
        }

        private List<Point> Plot(List<Cycle> cycles, Func<Cycle, double?> getValue)
        {
            var points = FilterCycles(cycles, ChartSettings)
                .Select(c =>
                    new
                    {
                        c.Index,
                        Value = getValue(c)
                    })
                .Where(c => c.Value != null)
                .Where(c => !double.IsInfinity(c.Value.GetValueOrDefault(0)))
                .Select(c =>
                    new Point
                    {
                        X = c.Index,
                        Y = c.Value.Value
                    }).ToList();

            return ApplyYFilter(points, ChartSettings).ToList();
        }

        private double? GetNormalizedCyclePropValue(string argument, Project project, Cycle cycle, Parameters parameters)
        {
            var descriptor = GetCyclePropertyDescriptor(argument);
            var unitMeasurement = GetUnit(descriptor.UnitMultiplierType, parameters);
            var data = descriptor.Data(new CyclePropertyContext(cycle, project));
            var normalizedDivider = unitMeasurement.IsAllowNormalization ? GetNormalizeByDivider(project, parameters) : 1.0;

            return unitMeasurement.Apply(data) / normalizedDivider;
        }

        private double? GetNormalizedDataPointPropValue(string argument, Project project, DataPoint dataPoint, Parameters parameters)
        {
            var descriptor = GetDataPointPropertyDescriptor(argument);
            var unitMeasurement = GetUnit(descriptor.UnitMultiplierType, parameters);
            var data = descriptor.Data(dataPoint);
            var normalizedDivider = unitMeasurement.IsAllowNormalization ? GetNormalizeByDivider(project, parameters) : 1.0;

            return unitMeasurement.Apply(data) / normalizedDivider;
        }

        public static PropertyDescriptor GetCyclePropertyDescriptor(string argument)
        {
            return _foo.GetCycleProperty(argument);
        }

        public static PropertyDescriptor GetDataPointPropertyDescriptor(string argument)
        {
            return _foo.GetDataPointProperty(argument);
        }


        private string GetLabel(Func<string,PropertyDescriptor> propertyDescriptorGetter, ArgumentTemplate argument)
        {
            var argumentName = argument.Arg.Split(':')[0]; // if we have merged property on some axis, take first one. Properties should have the same unit of measurement
            var isMergedArgument = argument.Arg.Split(':').Length > 1;
            var descriptor = propertyDescriptorGetter(argumentName);

            if (descriptor == null)
                return argumentName;

            var unitMeasurement = GetUnit(descriptor.UnitMultiplierType, ChartSettings);
            if (descriptor.UnitMultiplierType == MultiplierType.Time)
                return unitMeasurement.ToString();

            var prefix = !isMergedArgument ? (descriptor.IsChargeStep ? "Charge " : descriptor.IsDischargeStep ? "Discharge " : "") : "";
            var normilizedLabel = ChartSettings.NormalizeBy.GetType().GetField(Enum.GetName(typeof(NormalizeBy), ChartSettings.NormalizeBy)).GetCustomAttribute<DescriptionAttribute>().Description;
            var formattingLabel = ChartSettings.NormalizeBy == NormalizeBy.None || !unitMeasurement.IsAllowNormalization ? "" : $"/{normilizedLabel}";
            return string.IsNullOrEmpty(unitMeasurement.UnitAbbreviation) ?
                $"{prefix}{descriptor.Description}{formattingLabel}"
                : $"{prefix}{descriptor.Description} ({unitMeasurement.UnitAbbreviation}{formattingLabel})";
        }
    }


    public class Foo
    {
        private readonly Dictionary<string, PropertyDescriptor> _dataPointPropertyDescriptors = new Dictionary<string, PropertyDescriptor>
        {
            {
                "Time", new PropertyDescriptor
                {
                    Name = "Time",
                    UnitMultiplierType = MultiplierType.Time,
                    Description = "Time",
                    Data = o => ((DataPoint)o).Time
                }
            },
            {
                "Temperature", new PropertyDescriptor
                {
                    Name = "Temperature",
                    UnitMultiplierType = MultiplierType.Temperature,
                    Description = "Temperature",
                    Data = o => ((DataPoint)o).Temperature
                }
            },
            {
                "Power", new PropertyDescriptor
                {
                    Name = "Power",
                    UnitMultiplierType = MultiplierType.Power,
                    Description = "Power",
                    Data = o => ((DataPoint)o).Power
                }
            },
            {
                "Energy", new PropertyDescriptor
                {
                    Name = "Energy",
                    UnitMultiplierType = MultiplierType.Energy,
                    Description = "Energy",
                    Data = o => ((DataPoint)o).Energy
                }
            },
            {
                "CycleIndex", new PropertyDescriptor
                {
                    Name = "CycleIndex",
                    UnitMultiplierType = MultiplierType.None,
                    Description = "Cycle Number",
                    Data = o => ((DataPoint)o).CycleIndex
                }
            },
            {
                "Current", new PropertyDescriptor
                {
                    Name = "Current",
                    UnitMultiplierType = MultiplierType.Current,
                    Description = "Current",
                    Data = o => ((DataPoint)o).Current
                }
            },
            {
                "Voltage", new PropertyDescriptor
                {
                    Name = "Voltage",
                    UnitMultiplierType = MultiplierType.Voltage,
                    Description = "Voltage",
                    Data = o => ((DataPoint)o).Voltage
                }
            },
            {
                "Capacity", new PropertyDescriptor
                {
                    Name = "Capacity",
                    UnitMultiplierType = MultiplierType.Capacity,
                    Description = "Capacity",
                    Data = o => ((DataPoint)o).Capacity
                }
            },
        };

        private readonly Dictionary<string, PropertyDescriptor> _cyclePropertyDescriptors = new Dictionary<string, PropertyDescriptor>
        {
            {
                "CycleNumber", new PropertyDescriptor
                {
                    Name = "CycleNumber",
                    UnitMultiplierType = MultiplierType.None,
                    Description = "Cycle Number",
                    Data = o => ((CyclePropertyContext)o).Cycle.Index
                }
            },
            {
                "ChargeCapacity", new PropertyDescriptor
                {
                    Name = "ChargeCapacity",
                    UnitMultiplierType = MultiplierType.Capacity,
                    Description = "Capacity",
                    IsChargeStep = true,
                    Data = o => ((CyclePropertyContext)o).Cycle.ChargeCapacity
                }
            },
            {
                "DischargeCapacity", new PropertyDescriptor
                {
                    Name = "DischargeCapacity",
                    UnitMultiplierType = MultiplierType.Capacity,
                    Description = "Capacity",
                    IsDischargeStep = true,
                    Data = o => ((CyclePropertyContext)o).Cycle.DischargeCapacity
                }
            },
            {
                "CoulombicEfficiency", new PropertyDescriptor
                {
                    Name = "CoulombicEfficiency",
                    UnitMultiplierType = MultiplierType.None,
                    Description = "Coulombic Efficiency %",
                    Data = o =>  100.0f * (((CyclePropertyContext)o).Cycle.DischargeCapacity / ((CyclePropertyContext)o).Cycle.ChargeCapacity)
                }
            },
            {
                "AreaSpecificImpedance", new PropertyDescriptor
                {
                    Name = "Charge Area Specific Impedance",
                    UnitMultiplierType = MultiplierType.None,
                    Description = "Area-Specific Impedance",
                    Data = o =>
                    {
                        var context = (CyclePropertyContext)o;
                        return context.Cycle.ResistanceOhms * context.Project.Area.GetValueOrDefault(1);
                    }
                }
            },
            {
                "DischargeAreaSpecificImpedance", new PropertyDescriptor
                {
                    Name = "Discharge Area Specific Impedance",
                    UnitMultiplierType = MultiplierType.None,
                    Description = "Area-Specific Impedance",
                    Data = o =>
                    {
                        var context = (CyclePropertyContext)o;
                        return context.Cycle.DischargeResistance * context.Project.Area.GetValueOrDefault(1);
                    }
                }
            },
            {
                "EnergyEfficiency", new PropertyDescriptor
                {
                    Name = "EnergyEfficiency",
                    UnitMultiplierType = MultiplierType.None,
                    Description = "Energy Efficiency %",
                    Data = o => 100.0f * (((CyclePropertyContext)o).Cycle.DischargeEnergy / ((CyclePropertyContext)o).Cycle.ChargeEnergy)
                }
            },
            {
                "ChargeCapacityRetention", new PropertyDescriptor
                {
                    Name = "ChargeCapacityRetention",
                    UnitMultiplierType = MultiplierType.None,
                    Description = "Capacity Retention %",
                    IsChargeStep = true,
                    Data = o => ((CyclePropertyContext)o).Cycle.ChargeCapacityRetention
                }
            },
            {
                "DischargeCapacityRetention", new PropertyDescriptor
                {
                    Name = "DischargeCapacityRetention",
                    UnitMultiplierType = MultiplierType.None,
                    Description = "Capacity Retention %",
                    IsDischargeStep = true,
                    Data = o => ((CyclePropertyContext)o).Cycle.DischargeCapacityRetention
                }
            },
            {
                "ChargeEnergy", new PropertyDescriptor
                {
                    Name = "ChargeEnergy",
                    UnitMultiplierType = MultiplierType.Energy,
                    Description = "Energy",
                    IsChargeStep = true,
                    Data = o => ((CyclePropertyContext)o).Cycle.ChargeEnergy
                }
            },
            {
                "DischargeEnergy", new PropertyDescriptor
                {
                    Name = "DischargeEnergy",
                    UnitMultiplierType = MultiplierType.Energy,
                    Description = "Energy",
                    IsDischargeStep = true,
                    Data = o => ((CyclePropertyContext)o).Cycle.DischargeEnergy
                }
            },
            {
                "Power", new PropertyDescriptor
                {
                    Name = "Power",
                    UnitMultiplierType = MultiplierType.Power,
                    Description = "Power",
                    IsChargeStep = true,
                    Data = o => ((CyclePropertyContext)o).Cycle.Power
                }
            },
            {
                "DischargePower", new PropertyDescriptor
                {
                    Name = "DischargePower",
                    UnitMultiplierType = MultiplierType.Power,
                    Description = "Power",
                    IsDischargeStep = true,
                    Data = o => ((CyclePropertyContext)o).Cycle.DischargePower
                }
            },
            {
                "Temperature", new PropertyDescriptor
                {
                    Name = "Temperature",
                    UnitMultiplierType = MultiplierType.Temperature,
                    Description = "Temperature",
                    Data = o => ((CyclePropertyContext)o).Cycle.Temperature
                }
            },
            {
                "ChargeEndCurrent", new PropertyDescriptor
                {
                    Name = "EndCurrent",
                    UnitMultiplierType = MultiplierType.Current,
                    Description = "Current",
                    IsChargeStep = true,
                    Data = o => ((CyclePropertyContext)o).Cycle.EndCurrent
                }
            },
            {
                "DischargeEndCurrent", new PropertyDescriptor
                {
                    Name = "DischargeEndCurrent",
                    UnitMultiplierType = MultiplierType.Current,
                    Description = "Current",
                    IsDischargeStep = true,
                    Data = o => ((CyclePropertyContext)o).Cycle.DischargeEndCurrent
                }
            },
            {
                "StartCurrent", new PropertyDescriptor
                {
                    Name = "StartCurrent", UnitMultiplierType = MultiplierType.Current,
                    Description = "Current",
                    IsChargeStep = true,
                    Data = o => ((CyclePropertyContext)o).Cycle.StartCurrent
                }
            },
            {
                "StartDischargeCurrent", new PropertyDescriptor
                {
                    Name = "StartDischargeCurrent",
                    UnitMultiplierType = MultiplierType.Current,
                    Description = "Current",
                    IsDischargeStep = true,
                    Data = o => ((CyclePropertyContext)o).Cycle.StartDischargeCurrent
                }
            },
            {
                "StartChargeVoltage", new PropertyDescriptor
                {
                    Name = "StartChargeVoltage",
                    UnitMultiplierType = MultiplierType.Voltage,
                    Description = "Voltage",
                    IsChargeStep = true,
                    Data = o => ((CyclePropertyContext)o).Cycle.StartChargeVoltage
                }
            },
            {
                "StartDischargeVoltage", new PropertyDescriptor
                {
                    Name = "StartDischargeVoltage",
                    UnitMultiplierType = MultiplierType.Voltage,
                    Description = "Voltage",
                    IsDischargeStep = true,
                    Data = o => ((CyclePropertyContext)o).Cycle.StartDischargeVoltage
                }
            },
            {
                "ChargeEndVoltage", new PropertyDescriptor
                {
                    Name = "EndVoltage",
                    UnitMultiplierType = MultiplierType.Voltage,
                    Description = "Voltage",
                    IsChargeStep = true,
                    Data = o => ((CyclePropertyContext)o).Cycle.EndVoltage
                }
            },
            {
                "DischargeEndVoltage", new PropertyDescriptor
                {
                    Name = "DischargeEndVoltage",
                    UnitMultiplierType = MultiplierType.Voltage,
                    Description = "Voltage",
                    IsDischargeStep = true,
                    Data = o => ((CyclePropertyContext)o).Cycle.DischargeEndVoltage
                }
            },
            {
                "MidVoltage", new PropertyDescriptor
                {
                    Name = "MidVoltage",
                    UnitMultiplierType = MultiplierType.Voltage,
                    Description = "Medium Voltage",
                    Data = o => ((CyclePropertyContext)o).Cycle.MidVoltage
                }
            },
            {
                "ResistanceOhms", new PropertyDescriptor
                {
                    Name = "ResistanceOhms",
                    UnitMultiplierType = MultiplierType.Resistance,
                    Description = "Resistance",
                    IsChargeStep = true,
                    Data = o => ((CyclePropertyContext)o).Cycle.ResistanceOhms
                }
            },
            {
                "DischargeResistance", new PropertyDescriptor
                {
                    Name = "DischargeResistance",
                    UnitMultiplierType = MultiplierType.Resistance,
                    Description = "Resistance",
                    IsDischargeStep = true,
                    Data = o => ((CyclePropertyContext)o).Cycle.DischargeResistance
                } 
            }
        };

        public PropertyDescriptor GetDataPointProperty(string propertyName)
        {
            _dataPointPropertyDescriptors.TryGetValue(propertyName, out var descriptor);
            return descriptor;
        }

        public PropertyDescriptor GetCycleProperty(string propertyName)
        {
            _cyclePropertyDescriptors.TryGetValue(propertyName, out var descriptor);
            return descriptor;
        }
    }

    public class PropertyDescriptor
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public MultiplierType UnitMultiplierType { get; set; }

        public Func<object, double?> Data { get; set; }

        public bool IsChargeStep { get; set; }

        public bool IsDischargeStep { get; set; }
    }

    public class CyclePropertyContext 
    {
        public CyclePropertyContext(Cycle cycle, Project project)
        {
            Cycle = cycle;
            Project = project;
        }

        public Cycle Cycle { get; }

        public Project Project { get; }
    }
}