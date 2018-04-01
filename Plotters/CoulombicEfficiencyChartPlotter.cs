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
using Dqdv.Types;

namespace Plotting.Plotters
{
    public class CoulombicEfficiencyChartPlotter : ChartPlotterBase
    {
        public CoulombicEfficiencyChartPlotter(IProjectDataRepository projectDataRepository) : base(projectDataRepository)
        {
        }

        ////////////////////////////////////////////////////////////
        // Protected Methods/Atributes
        ////////////////////////////////////////////////////////////
        protected override void SetupChart(Chart chart, Parameters parameters)
        {
            chart.XAxisText = Title.CycleNumber;
            chart.XAxisIsInteger = true;
            chart.YAxisText = new [] { Title.CoulombicEfficiency };
        }

        protected override void Plot(Chart chart, Project project, string trace)
        {
            var cycles = ProjectDataRepository.GetCycles(project.Id, trace);
            var points = Plot(cycles, c =>
                {
                    if (project.IsAveragePlot)
                    {
                        return c.StatisticMetaData?.CoulombicEfficiencyAverage;
                    }

                    return c.ChargeCapacity != null && Math.Abs(c.ChargeCapacity.Value) > 0.000000001 ? 
                        100.0f * (c.DischargeCapacity / c.ChargeCapacity) : 0;

                }, 
                c => c.StatisticMetaData?.CoulombicEfficiencyStdDev);
            AddSeries(chart, project, ChartSettings, points, Title.CoulombicEfficiency);
        }
    }
}
