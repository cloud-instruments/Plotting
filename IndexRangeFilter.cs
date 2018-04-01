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
using System.Globalization;
using System.Linq;

namespace Plotting
{
    public class IndexRangeFilter
    {
        #region Private fields

        private class Range
        {
            public int? First { get; set; }
            public int? Last { get; set; }

            public bool Contains(int value)
            {
                if (First != null && value < First.Value)
                    return false;

                if (Last != null && value > Last.Value)
                    return false;

                return true;
            }
        }

        #endregion

        #region Private fields

        private readonly List<Range> _ranges;

        #endregion

        #region Constructor

        public IndexRangeFilter(string filter)
        {
            _ranges = (filter ?? string.Empty)
                .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(TryParseRange)
                .Where(r => r != null)
                .ToList();
        }

        #endregion

        #region Public methods

        public List<int> RangesItems
        {
            get
            {
                int maxInt = 0;

                foreach (var range in _ranges)
                {
                    if (range.Last.HasValue && range.Last > maxInt)
                    { maxInt = range.Last.Value; }
                    else if (range.First.HasValue && range.First > maxInt)
                    {
                        maxInt = range.First.Value;
                    }
                }

                var fArray = Enumerable.Range(0, maxInt+1).ToList();

                List<int> result = new List<int>();

                fArray.ForEach(r =>
                {
                    if (Contains(r))
                    {
                        result.Add(r);
                    }
                });

                return result;
            }
        }

        public bool Contains(int value)
        {
            return _ranges.Any(r => r.Contains(value));
        }

        #endregion

        #region Private methods

        private bool TryParseInteger(string str, out int result)
        {
            return int.TryParse(str, NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingWhite, CultureInfo.InvariantCulture, out result);
        }

        private Range TryParseRange(string str)
        {
            var hyphenIndex = str.IndexOf('-');
            if (hyphenIndex == -1)
            {
                if (!TryParseInteger(str, out var value))
                    return null;

                return new Range
                {
                    First = value,
                    Last = value
                };
            }

            var range = new Range();

            var firstPart = str.Substring(0, hyphenIndex);
            if (!string.IsNullOrWhiteSpace(firstPart))
            {
                if (!TryParseInteger(firstPart, out var value))
                    return null;

                range.First = value;
            }

            var lastPart = str.Substring(hyphenIndex + 1);
            if (!string.IsNullOrWhiteSpace(lastPart))
            {
                if (!TryParseInteger(lastPart, out var value))
                    return null;

                range.Last = value;
            }

            return range;
        }

        #endregion
    }
}
