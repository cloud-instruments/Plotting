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
using System.Collections.Generic;
using Plotting.UnitTransformation.Capacity;
using Plotting.UnitTransformation.Current;
using Plotting.UnitTransformation.Energy;
using Plotting.UnitTransformation.Power;
using Plotting.UnitTransformation.Resistance;
using Plotting.UnitTransformation.Temperature;
using Plotting.UnitTransformation.Time;
using Plotting.UnitTransformation.Voltage;

namespace Plotting.UnitTransformation
{
    public class UnitMeasurementProvider
    {
        private readonly Dictionary<string, IUnitMeasurement> _timeTranformation = new Dictionary<string, IUnitMeasurement>();
        private readonly Dictionary<string, IUnitMeasurement> _currentTranformation = new Dictionary<string, IUnitMeasurement>();
        private readonly Dictionary<string, IUnitMeasurement> _capacityTranformation = new Dictionary<string, IUnitMeasurement>();

        private readonly Dictionary<string, IUnitMeasurement> _unitTranformation = new Dictionary<string, IUnitMeasurement>();


        public UnitMeasurementProvider()
        {
            Register(_unitTranformation, new TimeSecondsMeasurement());
            Register(_unitTranformation, new TimeMinutesMeasurement());
            Register(_unitTranformation, new TimeHoursMeasurement());
            Register(_unitTranformation, new TimeDaysMeasurement());

            Register(_unitTranformation, new CurrentAmperMeasurement());
            Register(_unitTranformation, new CurrentMilliamperMeasurement());
            Register(_unitTranformation, new CurrentMicroamperMeasurement());

            Register(_unitTranformation, new CapacityAmperHoursMeasurement());
            Register(_unitTranformation, new CapacityMilliamperHoursMeasurement());

            Register(_unitTranformation, new TemperatureCelciusMeasurement());

            Register(_unitTranformation, new PowerMicrowattMeasurement());
            Register(_unitTranformation, new PowerMilliwattMeasurement());
            Register(_unitTranformation, new PowerWattMeasurement());
            Register(_unitTranformation, new PowerKilowattMeasurement());
            Register(_unitTranformation, new PowerMegawattMeasurement());
            Register(_unitTranformation, new PowerGigawattMeasurement());

            Register(_unitTranformation, new EnergyMicrowattHoursMeasurement());
            Register(_unitTranformation, new EnergyMilliwattHoursMeasurement());
            Register(_unitTranformation, new EnergyWattHoursMeasurement());
            Register(_unitTranformation, new EnergyKilowattHoursMeasurement());
            Register(_unitTranformation, new EnergyMegawattHoursMeasurement());
            Register(_unitTranformation, new EnergyGigawattHoursMeasurement());

            Register(_unitTranformation, new VoltageMeasurement());
            Register(_unitTranformation, new VoltageMillivoltMeasurement());
            Register(_unitTranformation, new VoltageMicrovoltMeasurement());

            Register(_unitTranformation, new ResistanceMicroohmMeasurement());
            Register(_unitTranformation, new ResistanceMilliohmMeasurement());
            Register(_unitTranformation, new ResistanceOhmMeasurement());
            Register(_unitTranformation, new ResistanceKiloohmMeasurement());
            Register(_unitTranformation, new ResistanceMegaohmMeasurement());
            Register(_unitTranformation, new ResistanceGigaohmMeasurement());
        }

        public IUnitMeasurement Unit(string unitCode)
        {
            _unitTranformation.TryGetValue(unitCode, out var unit);
            return unit ?? new NullMeasurement();
        }

        public IUnitMeasurement TimeTransformation(string unitCode)
        {
            _timeTranformation.TryGetValue(unitCode, out var unitTransformation);
            return unitTransformation;
        }

        public IUnitMeasurement CurrentTransformation(string unitCode)
        {
            _currentTranformation.TryGetValue(unitCode, out var unitTransformation);
            return unitTransformation;
        }

        public IUnitMeasurement CapacityTransformation(string unitCode)
        {
            _capacityTranformation.TryGetValue(unitCode, out var unitTransformation);
            return unitTransformation;
        }

        private void Register(IDictionary<string, IUnitMeasurement> container, IUnitMeasurement unitMeasurement)
        {
            container[unitMeasurement.Id] = unitMeasurement;
        }
    }
}