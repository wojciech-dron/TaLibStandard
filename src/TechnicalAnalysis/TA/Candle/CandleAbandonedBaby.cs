using System;
using TechnicalAnalysis.Abstractions;
using static TechnicalAnalysis.CandleSettingType;

namespace TechnicalAnalysis.Candle
{
    public class CandleAbandonedBaby : CandleIndicator
    {
        public CandleAbandonedBaby(in double[] open, in double[] high, in double[] low, in double[] close)
            : base(open, high, low, close)
        {
        }

        public RetCode CdlAbandonedBaby(
            int startIdx,
            int endIdx,
            in double optInPenetration,
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
            if (this.open == null || this.high == null || this.low == null || this.close == null)
            {
                return RetCode.BadParam;
            }

            if (optInPenetration < 0.0)
            {
                return RetCode.BadParam;
            }

            if (outInteger == null)
            {
                return RetCode.BadParam;
            }

            // Identify the minimum number of price bar needed to calculate at least one output.
            int lookbackTotal = this.CdlAbandonedBabyLookback();

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
            double bodyDojiPeriodTotal = 0.0;
            double bodyShortPeriodTotal = 0.0;
            int bodyLongTrailingIdx = startIdx - 2 - this.GetCandleAvgPeriod(BodyLong);
            int bodyDojiTrailingIdx = startIdx - 1 - this.GetCandleAvgPeriod(BodyDoji);
            int bodyShortTrailingIdx = startIdx - this.GetCandleAvgPeriod(BodyShort);
            
            int i = bodyLongTrailingIdx;
            while (i < startIdx - 2)
            {
                bodyLongPeriodTotal += this.GetCandleRange(BodyLong, i, this.open, this.high, this.low, this.close);
                i++;
            }

            i = bodyDojiTrailingIdx;
            while (i < startIdx - 1)
            {
                bodyDojiPeriodTotal += this.GetCandleRange(BodyDoji, i, this.open, this.high, this.low, this.close);
                i++;
            }

            i = bodyShortTrailingIdx;
            while (i < startIdx)
            {
                bodyShortPeriodTotal += this.GetCandleRange(BodyShort, i, this.open, this.high, this.low, this.close);
                i++;
            }

            i = startIdx;
            
            /* Proceed with the calculation for the requested range.
             * Must have:
             * - first candle: long white (black) real body
             * - second candle: doji
             * - third candle: black (white) real body that moves well within the first candle's real body
             * - upside (downside) gap between the first candle and the doji (the shadows of the two candles don't touch)
             * - downside (upside) gap between the doji and the third candle (the shadows of the two candles don't touch)
             * The meaning of "doji" and "long" is specified with TA_SetCandleSettings
             * The meaning of "moves well within" is specified with optInPenetration and "moves" should mean the real body should
             * not be short ("short" is specified with TA_SetCandleSettings) - Greg Morris wants it to be long, someone else want
             * it to be relatively long
             * outInteger is positive (1 to 100) when it's an abandoned baby bottom or negative (-1 to -100) when it's 
             * an abandoned baby top; the user should consider that an abandoned baby is significant when it appears in 
             * an uptrend or downtrend, while this function does not consider the trend
             */
            int outIdx = 0;
            do
            {
                bool isAbandonedBaby =
                    // 1st: long
                    this.GetRealBody(i - 2, this.open, this.close) >
                    this.GetCandleAverage(BodyLong, bodyLongPeriodTotal, i - 2, this.open, this.high, this.low, this.close) &&
                    // 2nd: doji
                    this.GetRealBody(i - 1, this.open, this.close) <=
                    this.GetCandleAverage(BodyDoji, bodyDojiPeriodTotal, i - 1, this.open, this.high, this.low, this.close) &&
                    // 3rd: longer than short
                    this.GetRealBody(i, this.open, this.close) >
                    this.GetCandleAverage(BodyShort, bodyShortPeriodTotal, i, this.open, this.high, this.low, this.close) &&
                    (
                        (
                            // 1st white
                            this.GetCandleColor(i - 2, this.open, this.close) == 1 &&
                            // 3rd black
                            this.GetCandleColor(i, this.open, this.close) == -1 &&
                            // 3rd closes well within 1st rb
                            this.close[i] < this.close[i - 2] - this.GetRealBody(i - 2, this.open, this.close) * optInPenetration &&
                            // upside gap between 1st and 2nd
                            this.GetCandleGapUp(i - 1, i - 2, this.high, this.low) &&
                            // downside gap between 2nd and 3rd
                            this.GetCandleGapDown(i, i - 1, this.high, this.low)
                        )
                        ||
                        (
                            // 1st black
                            this.GetCandleColor(i - 2, this.open, this.close) == -1 &&
                            // 3rd white
                            this.GetCandleColor(i, this.open, this.close) == 1 &&
                            // 3rd closes well within 1st rb
                            this.close[i] > this.close[i - 2] + this.GetRealBody(i - 2, this.open, this.close) * optInPenetration &&
                            // downside gap between 1st and 2nd
                            this.GetCandleGapDown(i - 1, i - 2, this.high, this.low) &&
                            // upside gap between 2nd and 3rd
                            this.GetCandleGapUp(i, i - 1, this.high, this.low)
                        )
                    );

                outInteger[outIdx++] = isAbandonedBaby ? this.GetCandleColor(i, this.open, this.close) * 100 : 0;

                /* add the current range and subtract the first range: this is done after the pattern recognition 
                 * when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)
                 */
                bodyLongPeriodTotal +=
                    this.GetCandleRange(BodyLong, i - 2, this.open, this.high, this.low, this.close) -
                    this.GetCandleRange(BodyLong, bodyLongTrailingIdx, this.open, this.high, this.low, this.close);

                bodyDojiPeriodTotal +=
                    this.GetCandleRange(BodyDoji, i - 1, this.open, this.high, this.low, this.close) -
                    this.GetCandleRange(BodyDoji, bodyDojiTrailingIdx, this.open, this.high, this.low, this.close);

                bodyShortPeriodTotal +=
                    this.GetCandleRange(BodyShort, i, this.open, this.high, this.low, this.close) -
                    this.GetCandleRange(BodyShort, bodyShortTrailingIdx, this.open, this.high, this.low, this.close);

                i++;
                bodyLongTrailingIdx++;
                bodyDojiTrailingIdx++;
                bodyShortTrailingIdx++;
            } while (i <= endIdx);

            // All done. Indicate the output limits and return.
            outNBElement = outIdx;
            outBegIdx = startIdx;
            
            return RetCode.Success;
        }

        public int CdlAbandonedBabyLookback()
        {
            return Math.Max(
                Math.Max(this.GetCandleAvgPeriod(BodyDoji), this.GetCandleAvgPeriod(BodyLong)),
                this.GetCandleAvgPeriod(BodyShort)
            ) + 2;
        }
    }
}