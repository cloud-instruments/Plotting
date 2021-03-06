﻿/*
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
using Dqdv.Types;

namespace Plotting.Plotters
{
    public class EndVoltageChartPlotter : ChartPlotterBase
    {
        public EndVoltageChartPlotter(IProjectDataRepository projectDataRepository) : base(projectDataRepository)
        {
        }

        protected override void SetupChart(Chart chart, Parameters parameters)
        {
            chart.XAxisText = Title.CycleNumber;
            chart.XAxisIsInteger = true;
            chart.YAxisText = new[] { FormatAxisTitle(MultiplierType.Voltage, parameters) };
        }

        protected override void Plot(Chart chart, Project project, string trace)
        {
            var cycles = ProjectDataRepository.GetCycles(project.Id, trace);
            if (ChartSettings.IsChargeEnabled)
            {
                var points = Plot(cycles, c => c.EndVoltage, c => c.StatisticMetaData?.EndVoltageStdDev);
                AddSeries(chart, project, ChartSettings, points, Title.Charge);
            }

            if (ChartSettings.IsDischargeEnabled)
            {
                var points = Plot(cycles, c => c.DischargeEndVoltage, c => c.StatisticMetaData?.DischargeEndVoltageStdDev);
                AddSeries(chart, project, ChartSettings, points, Title.Discharge);
            }
        }
    }
}