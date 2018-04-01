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
using System.Linq;
using Dqdv.Types;

namespace Plotting.Plotters
{
    public class EndTimeEndVoltageChartPlotter : ChartPlotterBase
    {
        public EndTimeEndVoltageChartPlotter(IProjectDataRepository projectDataRepository) : base(projectDataRepository)
        {
        }

        ////////////////////////////////////////////////////////////
        // Protected Methods/Atributes
        ////////////////////////////////////////////////////////////

        protected override void SetupChart(Chart chart, Parameters parameters)
        {
            chart.XAxisText = FormatAxisTitle(MultiplierType.Time, parameters);
            chart.YAxisText = new [] { FormatAxisTitle(MultiplierType.Voltage, parameters) };
        }

        protected override void Plot(Chart chart, Project project, string trace)
        {
            var cycles = ProjectDataRepository.GetCycles(project.Id, trace);
            var allPoints = ProjectDataRepository.GetPoints(project.Id, trace);
            var dataContext = GetDataNormalizedContext(project, MultiplierType.Time);

            var points = FilterCycles(cycles, ChartSettings)
                .SelectMany(c => Plot(allPoints, 
                c, 
                p => dataContext.Apply(p.Time), 
                p => p.Voltage,
                ChartSettings))
                .ToList();
            AddSeries(chart, project, ChartSettings, points, Title.TimeVsVoltage);
        }
    }
}
