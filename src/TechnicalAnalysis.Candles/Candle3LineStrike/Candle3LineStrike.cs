// Copyright (c) 2023 Philippe Matray. All rights reserved.
// This file is part of TaLibStandard.
// TaLibStandard is licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for the full license text.
// For more information, visit https://github.com/phmatray/TaLibStandard.

namespace TechnicalAnalysis.Candles;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class Candle3LineStrike<T> : CandleIndicator<T>
    where T : IFloatingPoint<T>
{
    private readonly T[] _nearPeriodTotal = new T[4];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="open">An array of open prices.</param>
    /// <param name="high">An array of high prices.</param>
    /// <param name="low">An array of low prices.</param>
    /// <param name="close">An array of close prices.</param>
    public Candle3LineStrike(in T[] open, in T[] high, in T[] low, in T[] close)
        : base(open, high, low, close)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="startIdx"></param>
    /// <param name="endIdx"></param>
    /// <returns></returns>
    public CandleIndicatorResult Compute(int startIdx, int endIdx)
    {
        // Initialize output variables 
        int outBegIdx = default;
        int outNBElement = default;
        int[] outInteger = new int[int.Max(0, endIdx - startIdx + 1)];

        // Validate the requested output range.
        if (startIdx < 0)
        {
            return new CandleIndicatorResult(OutOfRangeStartIndex, outBegIdx, outNBElement, outInteger);
        }

        if (endIdx < 0 || endIdx < startIdx)
        {
            return new CandleIndicatorResult(OutOfRangeEndIndex, outBegIdx, outNBElement, outInteger);
        }

        // Verify required price component.
        if (Open == null! || High == null! || Low == null! || Close == null!)
        {
            return new CandleIndicatorResult(BadParam, outBegIdx, outNBElement, outInteger);
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
            return new CandleIndicatorResult(Success, outBegIdx, outNBElement, outInteger);
        }

        // Do the calculation using tight loops.
        // Add-up the initial period, except for the last value.
        int nearTrailingIdx = startIdx - GetCandleAvgPeriod(Near);
            
        int i = nearTrailingIdx;
        while (i < startIdx)
        {
            _nearPeriodTotal[3] += GetCandleRange(Near, i - 3);
            _nearPeriodTotal[2] += GetCandleRange(Near, i - 2);
            i++;
        }

        i = startIdx;

        /* Proceed with the calculation for the requested range.
         * Must have:
         * - three white soldiers (three black crows): three white (black) candlesticks with consecutively higher (lower) closes,
         * each opening within or near the previous real body
         * - fourth candle: black (white) candle that opens above (below) prior candle's close and closes below (above) 
         * the first candle's open
         * The meaning of "near" is specified with TA_SetCandleSettings;
         * outInteger is positive (1 to 100) when bullish or negative (-1 to -100) when bearish;
         * the user should consider that 3-line strike is significant when it appears in a trend in the same direction of
         * the first three candles, while this function does not consider it
         */
        int outIdx = 0;
        do
        {
            outInteger[outIdx++] = GetPatternRecognition(i) ? (int)GetCandleColor(i - 1) * 100 : 0;

            /* add the current range and subtract the first range: this is done after the pattern recognition 
             * when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)
             */
            for (int totIdx = 3; totIdx >= 2; --totIdx)
            {
                _nearPeriodTotal[totIdx] +=
                    GetCandleRange(Near, i - totIdx) -
                    GetCandleRange(Near, nearTrailingIdx - totIdx);
            }

            i++;
            nearTrailingIdx++;
        } while (i <= endIdx);

        // All done. Indicate the output limits and return.
        outNBElement = outIdx;
        outBegIdx = startIdx;
            
        return new CandleIndicatorResult(Success, outBegIdx, outNBElement, outInteger);
    }

    /// <inheritdoc />
    public override bool GetPatternRecognition(int i)
    {
        bool is3LineStrike =
            // three with same color
            IsColorSame(i - 3, i - 2) &&
            IsColorSame(i - 2, i - 1) &&
            // 4th opposite color
            IsColorOpposite(i - 1, i) &&
            // 2nd opens within/near 1st rb
            Open[i - 2] >= T.Min(Open[i - 3], Close[i - 3]) -
            GetCandleAverage(Near, _nearPeriodTotal[3], i - 3) &&
            Open[i - 2] <= T.Max(Open[i - 3], Close[i - 3]) +
            GetCandleAverage(Near, _nearPeriodTotal[3], i - 3) &&
            // 3rd opens within/near 2nd rb
            Open[i - 1] >= T.Min(Open[i - 2], Close[i - 2]) -
            GetCandleAverage(Near, _nearPeriodTotal[2], i - 2) &&
            Open[i - 1] <= T.Max(Open[i - 2], Close[i - 2]) +
            GetCandleAverage(Near, _nearPeriodTotal[2], i - 2) &&
            (
                ( // if three white
                    IsColorGreen(i - 1) &&
                    Close[i - 1] > Close[i - 2] &&
                    // consecutive higher closes
                    Close[i - 2] > Close[i - 3] &&
                    // 4th opens above prior close
                    Open[i] > Close[i - 1] &&
                    // 4th closes below 1st open
                    Close[i] < Open[i - 3]
                ) ||
                ( // if three black
                    IsColorRed(i - 1) &&
                    Close[i - 1] < Close[i - 2] &&
                    // consecutive lower closes
                    Close[i - 2] < Close[i - 3] &&
                    // 4th opens below prior close
                    Open[i] < Close[i - 1] &&
                    // 4th closes above 1st open
                    Close[i] > Open[i - 3]
                )
            );
            
        return is3LineStrike;
    }

    /// <inheritdoc />
    public override int GetLookback()
    {
        return GetCandleAvgPeriod(Near) + 3;
    }
}
