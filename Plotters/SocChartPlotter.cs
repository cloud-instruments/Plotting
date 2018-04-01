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
using System.Linq;
using Dqdv.Types;
using Dqdv.Types.Plot;

namespace Plotting.Plotters
{
    public class SocChartPlotter : ChartPlotterBase
    {
        public SocChartPlotter(IProjectDataRepository projectDataRepository) : base(projectDataRepository)
        {
        }

        public override bool IsCalcEveryNthCycleForcedDisabled => false;

        ////////////////////////////////////////////////////////////
        // Protected Methods/Atributes
        ////////////////////////////////////////////////////////////

        protected override void SetupChart(Chart chart, Parameters parameters)
        {
            chart.XAxisText = Title.StateOfCharge;
            chart.XAxisIsInteger = false;
            if(chart.YAxisText == null)
            chart.YAxisText = new [] { FormatAxisTitle(MultiplierType.Voltage, parameters) };
        }

        protected override void Plot(Chart chart, Project project, string trace)
        {
            var stateOfCharge = (StateOfCharge)Context.Data;

            var allChargePoints = ProjectDataRepository.GetPoints(project.Id, trace)
                  .Where(point => point.Voltage.HasValue && point.Current.HasValue && point.Current.Value > 0 
                  && point.Capacity.HasValue && point.Capacity.Value != 0 
                  && (point.Voltage >= stateOfCharge.ChargeFrom && point.Voltage <= stateOfCharge.ChargeTo)
                    && (ChartSettings.CustomCycleFilter == null || ChartSettings.CustomCycleFilter.RangesItems.Count == 0 
                    || ChartSettings.CustomCycleFilter.RangesItems.Contains(point.CycleIndex)) )
                  .ToList();
            if (allChargePoints.Count > 0)
            {               
                var cycles = ProjectDataRepository.GetCycles(project.Id, trace).Where(c => allChargePoints.Any(p => p.CycleIndex == c.Index)).ToList();
                GetChartBySOC(chart, project, ChartSettings, cycles, stateOfCharge, allChargePoints);
            }
            var allDischargePoints = ProjectDataRepository.GetPoints(project.Id, trace)
                   .Where(point => point.Voltage.HasValue && point.Current.HasValue 
                   && point.Current.Value < 0 && point.Capacity.HasValue && point.Capacity.Value != 0 
                   && (point.Voltage >= stateOfCharge.ChargeFrom && point.Voltage <= stateOfCharge.ChargeTo)
                    && (ChartSettings.CustomCycleFilter == null || ChartSettings.CustomCycleFilter.RangesItems.Count == 0
                    || ChartSettings.CustomCycleFilter.RangesItems.Contains(point.CycleIndex)))
                   .ToList();
            if (allDischargePoints.Count > 0)
            {               
                var cycles = ProjectDataRepository.GetCycles(project.Id, trace).Where(c => allDischargePoints.Any(p => p.CycleIndex == c.Index)).ToList();
                GetChartBySODC(chart, project, ChartSettings, cycles, stateOfCharge, allDischargePoints);
            }
        }



        private void GetChartBySOC(Chart chart,
           Project project,
           Parameters parameters,
           List<Cycle> cycles,
           StateOfCharge stateOfCharge,  
           List<DataPoint> allPoints)
        {
            var firstCyscleIndex = 0;
            var pointCount = 0;
            foreach (var cycle in FilterCycles(cycles, parameters))
            {
                firstCyscleIndex = firstCyscleIndex + pointCount;
                var tempPoints = allPoints.Where(cycle1 => cycle1.CycleIndex == cycle.Index).ToList();
                pointCount = tempPoints.Count();
                var first = tempPoints.First();
                var last = tempPoints.Last();
                double denominator = 1;
                if ((double)(last.Capacity.Value - first.Capacity.Value) > 0)
                {
                    denominator = (double)(last.Capacity.Value - first.Capacity.Value);
                }

                var pointsCC = Plot(allPoints, firstCyscleIndex, pointCount,
                               p => Math.Abs((double)((p.Capacity - first.Capacity) / denominator) * 100),
                               p => p.Voltage, parameters).ToList();
                AddSeries(chart, project, parameters, pointsCC, $"{Title.Charge} {cycle.Index}", cycle.Index);
            }
        }

        private void GetChartBySODC(Chart chart,
           Project project,
           Parameters parameters,
           List<Cycle> cycles,
           StateOfCharge stateOfCharge,         
           List<DataPoint> allPoints)
        {
            var firstCyscleIndex = 0;
            var pointCount = 0;
            foreach (var cycle in FilterCycles(cycles, parameters))
            {
                firstCyscleIndex = firstCyscleIndex + pointCount;
                var tempPoints = allPoints.Where(cycle1 => cycle1.CycleIndex == cycle.Index).ToList();
                pointCount = tempPoints.Count();
                var first = tempPoints.First();
                var last = tempPoints.Last();
                double denominator = 1;
                if ((double)(last.Capacity.Value - first.Capacity.Value) > 0)
                {
                    denominator = (double)(last.Capacity.Value - first.Capacity.Value);
                }
                var pointsDC = Plot(allPoints, firstCyscleIndex, pointCount,                             
                             p => Math.Abs((double)((1 - ((p.Capacity - first.Capacity) / denominator))) * 100),
                             p => p.Voltage, parameters).ToList();
                AddSeries(chart, project, parameters, pointsDC, $"{Title.Discharge} {cycle.Index}", cycle.Index);

            }
        }

        private static IEnumerable<Cycle> FilterSOCCycles(List<Cycle> cycles, Parameters parameters)
        {
            var query = cycles.AsEnumerable();

            if (parameters.FromCycle != null)
                query = query.Where(c => c.Index >= parameters.FromCycle.Value);

            if (parameters.ToCycle != null)
                query = query.Where(c => c.Index <= parameters.ToCycle.Value);

            if (parameters.CustomCycleFilter != null)
                query = query.Where(c => parameters.CustomCycleFilter.Contains(c.Index));

            return query;
        }

        private static IEnumerable<Point> Plot(List<DataPoint> points, int firstPoint, int pointCount, Func<DataPoint, double?> getX, Func<DataPoint, double?> getY, Parameters parameters)
        {
            return ApplyYFilter(Plot(points, firstPoint, pointCount, getX, getY).ToList(), parameters);
        }
    }
}
