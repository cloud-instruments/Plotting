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
using System.Reflection;
using Dqdv.Types;
using log4net;

namespace Plotting
{
    public class Cycler
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        const int MaxStartCyclesForCalculation = 10;
        public static double Tolerance = .00001;

        #region Public methods

        public static List<Cycle> ExtractCycles(List<DataPoint> points)
        {
            Log.Info($"ExtractCycles started for '{points.Count}' points");

            //Cycles can be not in order, so we have to fix it first
            SortedList<int, List<DataPoint>> sortedList = new SortedList<int, List<DataPoint>>();
            foreach (var p in points)
            {
                if (sortedList.ContainsKey(p.CycleIndex))
                {
                    sortedList[p.CycleIndex].Add(p);
                }
                else
                {
                    sortedList.Add(p.CycleIndex, new List<DataPoint> { p });
                }
            }

            List<DataPoint> updatedPointsAfterSorting = new List<DataPoint>();
            List<Cycle> cycles = new List<Cycle>();
            double? lastRestVoltageFound = null;
            foreach (KeyValuePair<int, List<DataPoint>> sortedCycleKvp in sortedList)
            {
                var currentPoint = updatedPointsAfterSorting.Count;
                int cycleIndex = sortedCycleKvp.Key;
                List<DataPoint> cycleDataPoints = sortedCycleKvp.Value.OrderBy(c => c.Time).ToList();
                updatedPointsAfterSorting.AddRange(cycleDataPoints);

                var cycle = new Cycle
                {
                    Index = cycleIndex,
                    FirstPointIndex = currentPoint,
                    PointCount = cycleDataPoints.Count
                };

                foreach (DataPoint cyclePoint in cycleDataPoints)
                {
                    if (cycle.Time == null)
                    {
                        //Set first point time
                        cycle.Time = cyclePoint.Time;
                    }

                    if (cyclePoint.Energy != null)
                    {
                        cycle.ChargeEnergy = cyclePoint.Energy;
                    }

                    if (cyclePoint.DischargeEnergy != null)
                    {
                        cycle.DischargeEnergy = cyclePoint.DischargeEnergy;
                    }

                    //Only update if last point is not on the REST step, because it is 0 or useless
                    if (cyclePoint.Current != null && cyclePoint.CycleStep != CycleStep.Rest)
                    {
                        if (cyclePoint.CycleStep == CycleStep.ChargeCC ||
                            cyclePoint.CycleStep == CycleStep.ChargeCV)
                        {
                            cycle.EndCurrent = cyclePoint.Current;
                        }
                        else if (cyclePoint.CycleStep == CycleStep.Discharge)
                        {
                            cycle.DischargeEndCurrent = cyclePoint.Current;
                        }
                    }

                    // find last rest value for a given cycle before charge and discharge
                    if (cyclePoint.CycleStep == CycleStep.Rest)
                    {
                        lastRestVoltageFound = cyclePoint.Voltage.Value;
                    }

                    if (cyclePoint.Voltage != null && cyclePoint.CycleStep != CycleStep.Rest)
                    {
                        if (cyclePoint.CycleStep == CycleStep.ChargeCC ||
                            cyclePoint.CycleStep == CycleStep.ChargeCV)
                        {
                            cycle.EndVoltage = cyclePoint.Voltage;
                            if (cycle.EndRestVoltage == null)
                            {
                                cycle.EndRestVoltage = lastRestVoltageFound;
                            }
                        }
                        else if (cyclePoint.CycleStep == CycleStep.Discharge)
                        {
                            cycle.DischargeEndVoltage = cyclePoint.Voltage;
                            if (cycle.EndRestDischargeVoltage == null)
                            {
                                cycle.EndRestDischargeVoltage = lastRestVoltageFound;
                            }
                        }
                    }
                    if (cyclePoint.Temperature != null && cyclePoint.CycleStep != CycleStep.Rest)
                    {
                        cycle.Temperature = cyclePoint.Temperature;
                    }

                    // find first discharge value for a given cycle
                    if (cyclePoint.CycleStep == CycleStep.Discharge)
                    {
                        if (cycle.StartDischargeVoltage == null &&
                            cycle.StartDischargeCurrent == null)
                        {
                            cycle.StartDischargeCurrent = cyclePoint.Current;
                            cycle.StartDischargeVoltage = cyclePoint.Voltage;
                        }

                        if (cycle.EndRestDischargeVoltage != null &&
                            cycle.StartResistanceDischargeCurrent == null &&
                            cycle.StartResistanceDischargeVoltage == null)
                        {
                            cycle.StartResistanceDischargeCurrent = cyclePoint.Current;
                            cycle.StartResistanceDischargeVoltage = cyclePoint.Voltage;
                        }
                    }

                    // find first charge value for a given cycle
                    if ((cyclePoint.CycleStep == CycleStep.ChargeCC || 
                        cyclePoint.CycleStep == CycleStep.ChargeCV))
                    {
                        if (cycle.StartChargeVoltage == null &&
                            cycle.StartCurrent == null)
                        {
                            cycle.StartCurrent = cyclePoint.Current;
                            cycle.StartChargeVoltage = cyclePoint.Voltage;
                        }

                        if (cycle.EndRestVoltage != null && 
                            cycle.StartResistanceVoltage == null &&
                            cycle.StartResistanceCurrent == null)
                        {
                            cycle.StartResistanceCurrent = cyclePoint.Current;
                            cycle.StartResistanceVoltage = cyclePoint.Voltage;
                        }
                    }

                    currentPoint++;

                    //Update cycle time on the last step
                    if (currentPoint == cycleDataPoints.Count)
                    {
                        cycle.Time = cyclePoint.Time - cycle.Time;
                    }
                }

                if (cycle.EndRestVoltage == null)
                {
                    cycle.EndRestVoltage = lastRestVoltageFound;
                }

                if (cycle.EndRestDischargeVoltage == null)
                {
                    cycle.EndRestDischargeVoltage = lastRestVoltageFound;
                }

                cycles.Add(cycle);
            }

            double maxChargeCapacity = double.MinValue;
            int indexMaxChargeCapacity = int.MinValue;
            double maxDischargeCapacity = double.MinValue;
            int indexDischargeCapacity = int.MinValue;
            for (var i = 0; i < cycles.Count; i++)
            {
                var cycle = cycles[i];

                cycle.MidVoltage = GetMidVoltage(updatedPointsAfterSorting, cycle.FirstPointIndex, cycle.PointCount);
                cycle.ChargeCapacity = GetChargeCapacity(updatedPointsAfterSorting, cycle.FirstPointIndex, cycle.PointCount);
                cycle.DischargeCapacity = GetDischargeCapacity(updatedPointsAfterSorting, cycle.FirstPointIndex, cycle.PointCount);

                PopulateResistance(cycle, cycles, i);

                if (cycle.ChargeCapacity.HasValue)
                {
                    // Update value first time when we found charge capacity
                    if (Math.Abs(maxChargeCapacity - double.MinValue) < Tolerance)
                    {
                        maxChargeCapacity = cycle.ChargeCapacity.Value;
                        indexMaxChargeCapacity = i;
                    }
                    // Only pick max value from the first maxCyclesForCalculation cycles.
                    else if (cycle.ChargeCapacity.Value > maxChargeCapacity &&
                        i < MaxStartCyclesForCalculation)
                    {
                        maxChargeCapacity = cycle.ChargeCapacity.Value;
                    }
                }
                if (cycle.DischargeCapacity.HasValue)
                {
                    // Update value first time when we found discharge capacity
                    if (Math.Abs(maxDischargeCapacity - double.MinValue) < Tolerance)
                    {
                        maxDischargeCapacity = cycle.DischargeCapacity.Value;
                        indexDischargeCapacity = i;
                    }
                    // Only pick max value from the first maxCyclesForCalculation cycles.
                    else if (cycle.DischargeCapacity.Value > maxDischargeCapacity &&
                        i < MaxStartCyclesForCalculation)
                    {
                        maxDischargeCapacity = cycle.DischargeCapacity.Value;
                    }
                }

                cycle.Power = cycle.EndCurrent * cycle.EndVoltage;
                cycle.DischargePower = cycle.DischargeEndCurrent * cycle.EndVoltage;
            }

            Log.Info($"indexMaxChargeCapacity: '{indexMaxChargeCapacity}', indexDischargeCapacity: '{indexDischargeCapacity}' maxChargeCapacity: '{maxChargeCapacity}', maxDischargeCapacity: '{maxDischargeCapacity}'");

            UpdateCapacityRetention(cycles, indexMaxChargeCapacity, indexDischargeCapacity, maxChargeCapacity, maxDischargeCapacity);

            Log.Info($"ExtractCycles finished '{cycles.Count}' cycles extracted.");

            return cycles;
        }

        private static void PopulateResistance(Cycle cycle, List<Cycle> cycles, int i)
        {
            //check for division on zero
            if (cycle.StartResistanceCurrent != null)
            {
                if (cycle.StartCurrent != 0)
                {
                    double? chargeResistance = (cycle.StartResistanceVoltage - cycle.EndRestVoltage) / cycle.StartResistanceCurrent;
                    if (chargeResistance != null)
                    {
                        //Resistance is only positive, so we need to take absolute value
                        //Resistance is calculated in Ohms so we have to multiply by 1000
                        cycle.ResistanceOhms = Math.Abs(chargeResistance.Value) * 1000;
                    }
                }
                else
                {
                    cycles[i].ResistanceOhms = 0;
                }
            }
            else
            {
                //pick values from the next cycle if it exists
                if (i > 0 && i < cycles.Count)
                {
                    Cycle previousCycle = cycles[i - 1];
                    if (previousCycle != null &&
                        previousCycle.ResistanceOhms == null && 
                        cycle.StartResistanceCurrent != null && 
                        cycle.StartResistanceCurrent != 0 &&
                        previousCycle.EndRestVoltage != null)
                    {
                        double? chargeResistance = (cycle.StartResistanceVoltage - previousCycle.EndRestVoltage) / cycle.StartResistanceCurrent;
                        previousCycle.ResistanceOhms = Math.Abs(chargeResistance.Value) * 1000;
                    }
                }
            }

            //check for division on zero
            if (cycle.StartResistanceDischargeCurrent != null)
            {
                if (cycle.StartResistanceDischargeCurrent != 0)
                {
                    double? dischargeResistance = (cycle.StartResistanceDischargeVoltage - cycle.EndRestDischargeVoltage) / cycle.StartResistanceDischargeCurrent;
                    if (dischargeResistance != null)
                    {
                        //Resistance is only positive, so we need to take absolute value
                        //Resistance is calculated in Ohms so we have to multiply by 1000
                        cycle.DischargeResistance = Math.Abs(dischargeResistance.Value) * 1000;
                    }
                }
                else
                {
                    cycle.DischargeResistance = 0;
                }
            }
            else
            {
                //pick values from the next cycle if it exists
                if (i > 0 && i < cycles.Count)
                {
                    Cycle previousCycle = cycles[i - 1];
                    if (previousCycle != null &&
                        previousCycle.DischargeResistance == null &&
                        cycle.StartResistanceDischargeCurrent != null &&
                        cycle.StartResistanceDischargeCurrent != 0 &&
                        previousCycle.EndRestVoltage != null)
                    {
                        double? dischargeResistance = (cycle.StartResistanceDischargeVoltage - previousCycle.EndRestVoltage) / cycle.StartResistanceDischargeCurrent;
                        previousCycle.DischargeResistance = Math.Abs(dischargeResistance.Value) * 1000;
                    }
                }
            }
        }

        public Dictionary<int, List<Cycle>> GetChannelCycles(Dictionary<int, List<DataPoint>> channelDictionary)
        {
            Dictionary<int, List<Cycle>> channelCycles = new Dictionary<int, List<Cycle>>();
            foreach (KeyValuePair<int, List<DataPoint>> channelKvp in channelDictionary)
            {
                int channel = channelKvp.Key;
                List<DataPoint> points = channelKvp.Value;
                List<Cycle> cycles = ExtractCycles(points);
                channelCycles.Add(channel, cycles);
            }

            return channelCycles;
        }

        #endregion

        #region Private methods

        private static double? GetMidVoltage(List<DataPoint> points, int offset, int count)
        {
            if (count < 1)
            {
                return 0;
            }

            var curr = points[offset].CycleStep;
            var end = offset + count;

            for (var i = offset + 1; i < end; i++)
            {
                if (points[i].CycleStep == curr)
                {
                    continue;
                }

                curr = points[i].CycleStep;
                offset = i;
            }

            return points[offset + (end - offset - 1) / 2].Voltage;
        }

        private static double? GetChargeCapacity(List<DataPoint> points, int offset, int count)
        {
            var value = (double?)null;

            var end = offset + count;
            for (var i = offset; i < end; i++)
            {
                if ((points[i].CycleStep == CycleStep.ChargeCC || points[i].CycleStep == CycleStep.ChargeCV) &&
                    points[i].Capacity != null)
                {
                    value = points[i].Capacity;
                }
            }

            return value;
        }

        public static List<Cycle> UpdateCapacityRetention(List<Cycle> cycles,
            int indexMaxChargeCapacity,
            int indexDischargeCapacity,
            double maxChargeCapacity,
            double maxDischargeCapacity)
        {
            // Iterate again to update percentage values 
            for (var i = 0; i < cycles.Count; i++)
            {
                var cycle = cycles[i];
                if (i == indexMaxChargeCapacity)
                {
                    cycle.ChargeCapacityRetention = 100;
                }
                if (i == indexDischargeCapacity)
                {
                    cycle.DischargeCapacityRetention = 100;
                }
                if (cycle.ChargeCapacity.HasValue && i != indexMaxChargeCapacity)
                {
                    cycle.ChargeCapacityRetention = (cycle.ChargeCapacity.Value / maxChargeCapacity) * 100;
                }
                if (cycle.DischargeCapacity.HasValue && i != indexDischargeCapacity)
                {
                    cycle.DischargeCapacityRetention = (cycle.DischargeCapacity.Value / maxDischargeCapacity) * 100;
                }
            }

            return cycles;
        }

        private static double? GetDischargeCapacity(List<DataPoint> points, int offset, int count)
        {
            var value = (double?)null;

            var end = offset + count;
            for (var i = offset; i < end; i++)
            {
                if (points[i].CycleStep == CycleStep.Discharge && points[i].Capacity != null)
                {
                    value = points[i].Capacity;
                }
            }

            return value;
        }

        #endregion
    }
}
