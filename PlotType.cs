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
    public enum PlotType
    {
        EndCapacity = 0,
        EndVoltage = 1,
        EndCurrent = 2,
        MidVoltage = 3,
        VoltageCapacity = 4,
        CoulombicEfficiency = 8,
        CyclicVoltammetry = 10,
        DifferentialVoltage = 11,
        DifferentialCapacity = 12,
        EndTimeEndCurrent = 13,
        EndTimeEndVoltage = 14,
        ResistanceOhms = 15,
        CapacityRetention = 16,
        Energy = 17,
        Power = 18,
        Template = -1,
        View = -2,
        Soc = -3,
    }
}
