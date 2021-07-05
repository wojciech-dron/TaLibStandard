using TechnicalAnalysis.Abstractions;
using static TechnicalAnalysis.CandleSettingType;

namespace TechnicalAnalysis.Candle
{
    public class Candle2Crows : CandleIndicator
    {
        public Candle2Crows(in double[] open, in double[] high, in double[] low, in double[] close)
            : base(open, high, low, close)
        {
        }

        public RetCode Cdl2Crows(
            int startIdx,
            int endIdx,
            ref int outBegIdx,
            ref int outNBElement,
            ref int[] outInteger)
        {
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
            if (open == null || high == null || low == null || close == null)
            {
                return RetCode.BadParam;
            }

            if (outInteger == null)
            {
                return RetCode.BadParam;
            }

            // Identify the minimum number of price bar needed to calculate at least one output.
            int lookbackTotal = Cdl2CrowsLookback();

            // Move up the start index if there is not enough initial data.
            if (startIdx < lookbackTotal)
            {
                startIdx = lookbackTotal;
            }

            // Make sure there is still something to evaluate.
            if (startIdx > endIdx)
            {
                outBegIdx = 0;
                outNBElement = 0;
                return RetCode.Success;
            }

            // Do the calculation using tight loops.
            // Add-up the initial period, except for the last value.
            double bodyLongPeriodTotal = 0.0;
            int bodyLongTrailingIdx = startIdx - 2 - GetCandleAvgPeriod(BodyLong);

            int i = bodyLongTrailingIdx;
            while (i < startIdx - 2)
            {
                bodyLongPeriodTotal += GetCandleRange(BodyLong, i, open, high, low, close);
                i++;
            }

            i = startIdx;

            /* Proceed with the calculation for the requested range.
             * Must have:
             * - first candle: long white candle
             * - second candle: black real body
             * - gap between the first and the second candle's real bodies
             * - third candle: black candle that opens within the second real body and closes within the first real body
             * The meaning of "long" is specified with TA_SetCandleSettings
             * outInteger is negative (-1 to -100): two crows is always bearish; 
             * the user should consider that two crows is significant when it appears in an uptrend, while this function 
             * does not consider the trend
             */

            int outIdx = 0;
            do
            {
                bool is2Crows =
                    // 1st: white
                    GetCandleColor(i - 2, open, close) == 1 &&
                    // long
                    GetRealBody(i - 2, open, close) >
                    GetCandleAverage(BodyLong, bodyLongPeriodTotal, i - 2, open, high, low, close) &&
                    // 2nd: black
                    GetCandleColor(i - 1, open, close) == -1 &&
                    // gapping up
                    GetRealBodyGapUp(i - 1, i - 2, open, close) &&
                    // 3rd: black
                    GetCandleColor(i, open, close) == -1 &&
                    // opening within 2nd rb
                    open[i] < open[i - 1] &&
                    open[i] > close[i - 1] &&
                    // closing within 1st rb
                    close[i] > open[i - 2] &&
                    close[i] < close[i - 2];

                outInteger[outIdx++] = is2Crows ? -100 : 0;

                /* add the current range and subtract the first range: this is done after the pattern recognition 
                 * when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)
                 */
                bodyLongPeriodTotal +=
                    GetCandleRange(BodyLong, i - 2, open, high, low, close) -
                    GetCandleRange(BodyLong, bodyLongTrailingIdx, open, high, low, close);

                i++;
                bodyLongTrailingIdx++;
            } while (i <= endIdx);

            // All done. Indicate the output limits and return.
            outNBElement = outIdx;
            outBegIdx = startIdx;

            return RetCode.Success;
        }

        public int Cdl2CrowsLookback()
        {
            return GetCandleAvgPeriod(BodyLong) + 2;
        }
    }
}