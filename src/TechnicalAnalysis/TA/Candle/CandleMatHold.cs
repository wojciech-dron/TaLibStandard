using TechnicalAnalysis.Abstractions;
using static System.Math;
using static TechnicalAnalysis.CandleSettingType;

namespace TechnicalAnalysis.Candle
{
    public class CandleMatHold : CandleIndicator
    {
        private double[] _bodyPeriodTotal = new double[5];
        private double _penetration;

        public CandleMatHold(in double[] open, in double[] high, in double[] low, in double[] close)
            : base(open, high, low, close)
        {
        }

        public RetCode TryCompute(
            int startIdx,
            int endIdx,
            in double optInPenetration,
            out int outBegIdx,
            out int outNBElement,
            out int[] outInteger)
        {
            _penetration = optInPenetration;
            
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
            if (open == null || high == null || low == null || close == null)
            {
                return RetCode.BadParam;
            }

            if (optInPenetration < 0.0)
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
            _bodyPeriodTotal[4] = 0.0;
            _bodyPeriodTotal[3] = 0.0;
            _bodyPeriodTotal[2] = 0.0;
            _bodyPeriodTotal[1] = 0.0;
            _bodyPeriodTotal[0] = 0.0;
            int bodyShortTrailingIdx = startIdx - GetCandleAvgPeriod(BodyShort);
            int bodyLongTrailingIdx = startIdx - GetCandleAvgPeriod(BodyLong);
            
            int i = bodyShortTrailingIdx;
            while (i < startIdx)
            {
                _bodyPeriodTotal[3] += GetCandleRange(BodyShort, i - 3);
                _bodyPeriodTotal[2] += GetCandleRange(BodyShort, i - 2);
                _bodyPeriodTotal[1] += GetCandleRange(BodyShort, i - 1);
                i++;
            }

            i = bodyLongTrailingIdx;
            while (i < startIdx)
            {
                _bodyPeriodTotal[4] += GetCandleRange(BodyLong, i - 4);
                i++;
            }

            i = startIdx;

            /* Proceed with the calculation for the requested range.
             * Must have:
             * - first candle: long white candle
             * - upside gap between the first and the second bodies
             * - second candle: small black candle
             * - third and fourth candles: falling small real body candlesticks (commonly black) that hold within the long 
             *   white candle's body and are higher than the reaction days of the rising three methods
             * - fifth candle: white candle that opens above the previous small candle's close and closes higher than the 
             *   high of the highest reaction day
             * The meaning of "short" and "long" is specified with TA_SetCandleSettings; 
             * "hold within" means "a part of the real body must be within";
             * optInPenetration is the maximum percentage of the first white body the reaction days can penetrate (it is 
             * to specify how much the reaction days should be "higher than the reaction days of the rising three methods")
             * outInteger is positive (1 to 100): mat hold is always bullish
             */
            int outIdx = 0;
            do
            {
                outInteger[outIdx++] = GetPatternRecognition(i) ? 100 : 0;

                /* add the current range and subtract the first range: this is done after the pattern recognition 
                 * when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)
                 */
                _bodyPeriodTotal[4] +=
                    GetCandleRange(BodyLong, i - 4) -
                    GetCandleRange(BodyLong, bodyLongTrailingIdx - 4);

                int totIdx;
                for (totIdx = 3; totIdx >= 1; --totIdx)
                {
                    _bodyPeriodTotal[totIdx] +=
                        GetCandleRange(BodyShort, i - totIdx) -
                        GetCandleRange(BodyShort, bodyShortTrailingIdx - totIdx);
                }

                i++;
                bodyShortTrailingIdx++;
                bodyLongTrailingIdx++;
            } while (i <= endIdx);

            // All done. Indicate the output limits and return.
            outNBElement = outIdx;
            outBegIdx = startIdx;
            
            return RetCode.Success;
        }

        public override bool GetPatternRecognition(int i)
        {
            bool isMatHold =
                // 1st long, then 3 small
                GetRealBody(i - 4) > GetCandleAverage(BodyLong, _bodyPeriodTotal[4], i - 4) &&
                GetRealBody(i - 3) < GetCandleAverage(BodyShort, _bodyPeriodTotal[3], i - 3) &&
                GetRealBody(i - 2) < GetCandleAverage(BodyShort, _bodyPeriodTotal[2], i - 2) &&
                GetRealBody(i - 1) < GetCandleAverage(BodyShort, _bodyPeriodTotal[1], i - 1) &&
                // white, black, 2 black or white, white
                GetCandleColor(i - 4) == 1 &&
                GetCandleColor(i - 3) == -1 &&
                GetCandleColor(i) == 1 &&
                // upside gap 1st to 2nd
                GetRealBodyGapUp(i - 3, i - 4) &&
                // 3rd to 4th hold within 1st: a part of the real body must be within 1st real body
                Min(open[i - 2], close[i - 2]) < close[i - 4] &&
                Min(open[i - 1], close[i - 1]) < close[i - 4] &&
                // reaction days penetrate first body less than optInPenetration percent
                Min(open[i - 2], close[i - 2]) > close[i - 4] - GetRealBody(i - 4) * _penetration &&
                Min(open[i - 1], close[i - 1]) > close[i - 4] - GetRealBody(i - 4) * _penetration &&
                // 2nd to 4th are falling
                Max(close[i - 2], open[i - 2]) < open[i - 3] &&
                Max(close[i - 1], open[i - 1]) < Max(close[i - 2], open[i - 2]) &&
                // 5th opens above the prior close
                open[i] > close[i - 1] &&
                // 5th closes above the highest high of the reaction days
                close[i] > Max(Max(high[i - 3], high[i - 2]), high[i - 1]);
            
            return isMatHold;
        }

        public override int GetLookback()
        {
            return Max(GetCandleAvgPeriod(BodyShort), GetCandleAvgPeriod(BodyLong)) + 4;
        }
    }
}
