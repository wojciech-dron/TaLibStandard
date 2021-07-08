using TechnicalAnalysis.Common;
using static System.Math;
using static TechnicalAnalysis.Common.CandleSettingType;

namespace TechnicalAnalysis.Candles.CandleTasukiGap
{
    public class CandleTasukiGap : CandleIndicator
    {
        private double _nearPeriodTotal;

        public CandleTasukiGap(in double[] open, in double[] high, in double[] low, in double[] close)
            : base(open, high, low, close)
        {
        }

        public RetCode TryCompute(
            int startIdx,
            int endIdx,
            out int outBegIdx,
            out int outNBElement,
            out int[] outInteger)
        {
            // Initialize output variables 
            outBegIdx = default;
            outNBElement = default;
            outInteger = new int[endIdx - startIdx + 1];
            
            // Validate the requested output range.
            if (startIdx < 0)
            {
                return RetCode.OutOfRangeStartIndex;
            }

            if (endIdx < 0 || endIdx < startIdx)
            {
                return RetCode.OutOfRangeEndIndex;
            }

            // Verify required price component.
            if (_open == null || _high == null || _low == null || _close == null)
            {
                return RetCode.BadParam;
            }

            // Identify the minimum number of price bar needed to calculate at least one output.
            int lookbackTotal = GetLookback();

            // Move up the start index if there is not enough initial data.
            if (startIdx < lookbackTotal)
            {
                startIdx = lookbackTotal;
            }

            // Make sure there is still something to evaluate.
            if (startIdx > endIdx)
            {
                return RetCode.Success;
            }

            // Do the calculation using tight loops.
            // Add-up the initial period, except for the last value.
            int nearTrailingIdx = startIdx - GetCandleAvgPeriod(Near);
            
            int i = nearTrailingIdx;
            while (i < startIdx)
            {
                _nearPeriodTotal += GetCandleRange(Near, i - 1);
                i++;
            }

            i = startIdx;

            /* Proceed with the calculation for the requested range.
             * Must have:
             * - upside (downside) gap
             * - first candle after the window: white (black) candlestick
             * - second candle: black (white) candlestick that opens within the previous real body and closes under (above)
             *   the previous real body inside the gap
             * - the size of two real bodies should be near the same
             * The meaning of "near" is specified with TA_SetCandleSettings
             * outInteger is positive (1 to 100) when bullish or negative (-1 to -100) when bearish;
             * the user should consider that tasuki gap is significant when it appears in a trend, while this function does 
             * not consider it
             */
            int outIdx = 0;
            do
            {
                outInteger[outIdx++] = GetPatternRecognition(i) ? GetCandleColor(i - 1) * 100 : 0;

                /* add the current range and subtract the first range: this is done after the pattern recognition 
                 * when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)
                 */
                _nearPeriodTotal +=
                    GetCandleRange(Near, i - 1) -
                    GetCandleRange(Near, nearTrailingIdx - 1);

                i++;
                nearTrailingIdx++;
            } while (i <= endIdx);

            // All done. Indicate the output limits and return.
            outNBElement = outIdx;
            outBegIdx = startIdx;
            
            return RetCode.Success;
        }

        public override bool GetPatternRecognition(int i)
        {
            bool isTasukiGap =
                (
                    // upside gap
                    GetRealBodyGapUp(i - 1, i - 2) &&
                    // 1st: white
                    GetCandleColor(i - 1) == 1 &&
                    // 2nd: black
                    GetCandleColor(i) == -1 &&
                    // that opens within the white rb
                    _open[i] < _close[i - 1] && _open[i] > _open[i - 1] &&
                    // and closes under the white rb
                    _close[i] < _open[i - 1] &&
                    // inside the gap
                    _close[i] > Max(_close[i - 2], _open[i - 2]) &&
                    // size of 2 rb near the same
                    Abs(GetRealBody(i - 1) - GetRealBody(i)) < GetCandleAverage(Near, _nearPeriodTotal, i - 1)
                ) ||
                (
                    // downside gap
                    GetRealBodyGapDown(i - 1, i - 2) &&
                    // 1st: black
                    GetCandleColor(i - 1) == -1 &&
                    // 2nd: white
                    GetCandleColor(i) == 1 &&
                    // that opens within the black rb
                    _open[i] < _open[i - 1] && _open[i] > _close[i - 1] &&
                    // and closes above the black rb
                    _close[i] > _open[i - 1] &&
                    // inside the gap
                    _close[i] < Min(_close[i - 2], _open[i - 2]) &&
                    // size of 2 rb near the same
                    Abs(GetRealBody(i - 1) - GetRealBody(i)) < GetCandleAverage(Near, _nearPeriodTotal, i - 1)
                );
            
            return isTasukiGap;
        }

        public override int GetLookback()
        {
            return GetCandleAvgPeriod(Near) + 2;
        }
    }
}