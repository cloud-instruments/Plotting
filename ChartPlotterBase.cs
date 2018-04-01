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
using Dqdv.Types.Plot;

namespace Plotting
{
    public abstract class ChartPlotterBase : PlotterBase
    {
        public Chart Plot(bool projectsSumCyclesGreaterThanMax, ChartPlotterContext ctx)
        {
            Context = ctx;

            var forcedEveryNthCycle = CalcForcedEveryNthCycle(projectsSumCyclesGreaterThanMax, ctx.ProjectIds, ctx.Parameters, ctx.Trace);
            ChartSettings = MakeParameters(ctx.Parameters, forcedEveryNthCycle);
            var chart = CreateChart();
            chart.ForcedEveryNthCycle = forcedEveryNthCycle;

            foreach (var pid in ctx.ProjectIds)
            {
                Plot(chart, pid, ctx.Trace);
            }

            return chart;
        }

        protected ChartPlotterContext Context { get; private set; }

        public virtual bool IsCalcEveryNthCycleForcedDisabled => true;
    
        protected void Plot(Chart chart, int projectId, string trace)
        {
            var project = ProjectDataRepository.GetProject(projectId, trace);
            chart.Projects.Add(project);

            Plot(chart, project, trace);
        }


        private Chart CreateChart()
        {
            Chart chart = new Chart
            {
                Projects = new List<Project>(),
                Series = new List<Series>(),
                Label = new Label { Font = new Font(FontFamily.GenericMonospace, 8) }
            };
            
            SetupChart(chart, ChartSettings);
            return chart;
        }

        protected abstract void SetupChart(Chart chart, Parameters parameters);

        protected abstract void Plot(Chart chart, Project project, string trace);

        private int? CalcForcedEveryNthCycle(bool projectsSumCyclesGreaterThanMax, 
            int[] projects, 
            PlotParameters parameters, 
            string trace)
        {
            if (!projectsSumCyclesGreaterThanMax || IsCalcEveryNthCycleForcedDisabled)
            {
                return null;
            }

            var maxCycles = parameters?.MaxCycles ?? 0;
            if (maxCycles <= 0)
                return null;

            var maxCyclesPerProject = Math.Max(maxCycles / projects.Length - 1, 1);
            var forcedEveryNthCycle = projects.Max(pid =>
            {
                int cycles = ProjectDataRepository.GetCycles(pid, trace).Count;

                if (parameters != null && string.IsNullOrEmpty(parameters.CustomCycleFilter))
                {
                    int fromCycle = Math.Max(parameters.FromCycle ?? 1, 1);
                    int toCycle = Math.Min(parameters.ToCycle ?? cycles, cycles);

                    cycles = toCycle - fromCycle + 1;
                    int result = cycles / maxCyclesPerProject;
                    if (cycles % maxCyclesPerProject != 0)
                    {
                        result += 1;
                    }

                    return result;
                }
                else
                {
                    if (parameters != null)
                    {
                        var rangeFilter = new IndexRangeFilter(parameters.CustomCycleFilter).RangesItems;
                        cycles = rangeFilter.Count;
                    }
                    int result = cycles / maxCyclesPerProject;
                    if (cycles % maxCyclesPerProject != 0)
                    {
                        result += 1;
                    }

                    return result;
                }
            });

            if (forcedEveryNthCycle < 2)
            {
                return null;
            }

            if (parameters?.EveryNthCycle != null &&
                parameters.EveryNthCycle.Value >= forcedEveryNthCycle)
            {
                return null;
            }

            return forcedEveryNthCycle;
        }

        protected ChartPlotterBase(IProjectDataRepository projectDataRepository) : base(projectDataRepository)
        {
        }
    }
}