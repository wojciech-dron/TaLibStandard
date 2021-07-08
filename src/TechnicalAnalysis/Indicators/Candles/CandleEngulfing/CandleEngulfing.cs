using TechnicalAnalysis.Common;

namespace TechnicalAnalysis.Candles.CandleEngulfing
{
    public class CandleEngulfing : CandleIndicator
    {
        public CandleEngulfing(in double[] open, in double[] high, in double[] low, in double[] close)
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
            int i = startIdx;

            /* Proceed with the calculation for the requested range.
             * Must have:
             * - first: black (white) real body
             * - second: white (black) real body that engulfs the prior real body
             * outInteger is positive (1 to 100) when bullish or negative (-1 to -100) when bearish;
             * the user should consider that an engulfing must appear in a downtrend if bullish or in an uptrend if bearish,
             * while this function does not consider it
             */
            int outIdx = 0;
            do
            {
                bool isEngulfing = GetPatternRecognition(i);

                outInteger[outIdx++] = isEngulfing ? GetCandleColor(i) * 100 : 0;

                i++;
            } while (i <= endIdx);

            // All done. Indicate the output limits and return.
            outNBElement = outIdx;
            outBegIdx = startIdx;
            
            return RetCode.Success;
        }

        public override bool GetPatternRecognition(int i)
        {
            bool isEngulfing =
                (
                    // white engulfs black
                    GetCandleColor(i) == 1 &&
                    GetCandleColor(i - 1) == -1 &&
                    _close[i] > _open[i - 1] &&
                    _open[i] < _close[i - 1]
                )
                ||
                (
                    // black engulfs white
                    GetCandleColor(i) == -1 &&
                    GetCandleColor(i - 1) == 1 &&
                    _open[i] > _close[i - 1] &&
                    _close[i] < _open[i - 1]
                );
            
            return isEngulfing;
        }

        public override int GetLookback()
        {
            return 2;
        }
    }
}