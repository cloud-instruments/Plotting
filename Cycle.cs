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
namespace Plotting
{
    public class Cycle
    {
        public int Index { get; set; }
        public int FirstPointIndex { get; set; }
        public int PointCount { get; set; }
        public double? EndCurrent { get; set; }
        public double? DischargeEndCurrent { get; set; }
        public double? MidVoltage { get; set; }
        public double? EndVoltage { get; set; }
        public double? DischargeEndVoltage { get; set; }
        public double? ChargeCapacity { get; set; }
        public double? DischargeCapacity { get; set; }
        public double? ChargeCapacityRetention { get; set; }
        public double? DischargeCapacityRetention { get; set; }
        public double? Power { get; set; }
        public double? DischargePower { get; set; }
        public double? ChargeEnergy { get; set; }
        public double? DischargeEnergy { get; set; }
        public StatisticMetaData StatisticMetaData { get; set; }
        public double? Temperature { get; set; }
        public double? EndRestVoltage { get; set; }
        public double? EndRestDischargeVoltage { get; set; }
        public double? StartCurrent { get; set; }
        public double? StartDischargeCurrent { get; set; }
        public double? StartChargeVoltage { get; set; }
        public double? StartDischargeVoltage { get; set; }
        public double? StartResistanceCurrent { get; set; }
        public double? StartResistanceVoltage { get; set; }
        public double? StartResistanceDischargeCurrent { get; set; }
        public double? StartResistanceDischargeVoltage { get; set; }
        public double? ResistanceOhms { get; set; }
        public double? DischargeResistance { get; set; }
        public double? Time { get; set; }
    }

    public class StatisticMetaData
    {
        public double? EndCurrentStdDev { get; set; }
        public double? DischargeEndCurrentStdDev { get; set; }
        public double? MidVoltageStdDev { get; set; }
        public double? EndVoltageStdDev { get; set; }
        public double? DischargeEndVoltageStdDev { get; set; }
        public double? ChargeCapacityStdDev { get; set; }
        public double? DischargeCapacityStdDev { get; set; }
        public double? DischargeEnergyStdDev { get; set; }
        public double? ChargeEnergyStdDev { get; set; }
        public double? PowerStdDev { get; set; }
        public double? DischargePowerStdDev { get; set; }
        public double? ResistanceOhmsStdDev { get; set; }
        public double? DischargeResistanceStdDev { get; set; }
        public double? CoulombicEfficiencyStdDev { get; set; }
        public double? CoulombicEfficiencyAverage { get; set; }
        public double? ChargeCapacityRetentionStdDev { get; set; }
        public double? DischargeCapacityRetentionStdDev { get; set; }
    }
}
