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
public class CandleDoji<T> : CandleIndicator<T>
    where T : IFloatingPoint<T>
{
    private T _bodyDojiPeriodTotal = T.Zero;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="open">An array of open prices.</param>
    /// <param name="high">An array of high prices.</param>
    /// <param name="low">An array of low prices.</param>
    /// <param name="close">An array of close prices.</param>
    public CandleDoji(in T[] open, in T[] high, in T[] low, in T[] close)
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
        int bodyDojiTrailingIdx = startIdx - GetCandleAvgPeriod(BodyDoji);

        int i = bodyDojiTrailingIdx;
        while (i < startIdx)
        {
            _bodyDojiPeriodTotal += GetCandleRange(BodyDoji, i);
            i++;
        }

        /* Proceed with the calculation for the requested range.
         *
         * Must have:
         * - open quite equal to close
         * How much can be the maximum distance between open and close is specified with TA_SetCandleSettings
         * outInteger is always positive (1 to 100) but this does not mean it is bullish: doji shows uncertainty and it is
         * neither bullish nor bearish when considered alone
         */
        int outIdx = 0;
        do
        {
            outInteger[outIdx++] = GetPatternRecognition(i) ? 100 : 0;

            /* add the current range and subtract the first range: this is done after the pattern recognition 
             * when avgPeriod is not 0, that means "compare with the previous candles" (it excludes the current candle)
             */
            _bodyDojiPeriodTotal +=
                GetCandleRange(BodyDoji, i) -
                GetCandleRange(BodyDoji, bodyDojiTrailingIdx);

            i++;
            bodyDojiTrailingIdx++;
        } while (i <= endIdx);

        // All done. Indicate the output limits and return.
        outNBElement = outIdx;
        outBegIdx = startIdx;

        return new CandleIndicatorResult(Success, outBegIdx, outNBElement, outInteger);
    }

    /// <inheritdoc />
    public override bool GetPatternRecognition(int i)
    {
        bool isDoji = GetRealBody(i) <= GetCandleAverage(BodyDoji, _bodyDojiPeriodTotal, i);
            
        return isDoji;
    }

    /// <inheritdoc />
    public override int GetLookback()
    {
        return GetCandleAvgPeriod(BodyDoji);
    }
}
