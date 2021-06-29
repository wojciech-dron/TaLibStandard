namespace TechnicalAnalysis
{
    internal static partial class TACore
    {
        public static RetCode AdOsc(
            int startIdx,
            int endIdx,
            double[] inHigh,
            double[] inLow,
            double[] inClose,
            double[] inVolume,
            int optInFastPeriod,
            int optInSlowPeriod,
            ref int outBegIdx,
            ref int outNBElement,
            double[] outReal)
        {
            if (startIdx < 0)
            {
                return RetCode.OutOfRangeStartIndex;
            }

            if (endIdx < 0 || endIdx < startIdx)
            {
                return RetCode.OutOfRangeEndIndex;
            }

            if (inHigh == null || inLow == null || inClose == null || inVolume == null)
            {
                return RetCode.BadParam;
            }

            if (optInFastPeriod is < 2 or > 100000)
            {
                return RetCode.BadParam;
            }

            if (optInSlowPeriod is < 2 or > 100000)
            {
                return RetCode.BadParam;
            }

            if (outReal == null)
            {
                return RetCode.BadParam;
            }

            int slowestPeriod = optInFastPeriod < optInSlowPeriod
                ? optInSlowPeriod
                : optInFastPeriod;

            int lookbackTotal = EmaLookback(slowestPeriod);
            if (startIdx < lookbackTotal)
            {
                startIdx = lookbackTotal;
            }

            if (startIdx > endIdx)
            {
                outBegIdx = 0;
                outNBElement = 0;
                return RetCode.Success;
            }

            outBegIdx = startIdx;
            int today = startIdx - lookbackTotal;
            double ad = 0.0;
            double fastk = 2.0 / (optInFastPeriod + 1);
            double one_minus_fastk = 1.0 - fastk;
            double slowk = 2.0 / (optInSlowPeriod + 1);
            double one_minus_slowk = 1.0 - slowk;
            double high = inHigh[today];
            double low = inLow[today];
            double tmp = high - low;
            double close = inClose[today];
            if (tmp > 0.0)
            {
                ad += (close - low - (high - close)) / tmp * inVolume[today];
            }

            today++;
            double fastEMA = ad;
            double slowEMA = ad;
            while (true)
            {
                if (today >= startIdx)
                {
                    break;
                }

                high = inHigh[today];
                low = inLow[today];
                tmp = high - low;
                close = inClose[today];
                if (tmp > 0.0)
                {
                    ad += (close - low - (high - close)) / tmp * inVolume[today];
                }

                today++;
                fastEMA = fastk * ad + one_minus_fastk * fastEMA;
                slowEMA = slowk * ad + one_minus_slowk * slowEMA;
            }

            int outIdx = 0;
            while (true)
            {
                if (today > endIdx)
                {
                    break;
                }

                high = inHigh[today];
                low = inLow[today];
                tmp = high - low;
                close = inClose[today];
                if (tmp > 0.0)
                {
                    ad += (close - low - (high - close)) / tmp * inVolume[today];
                }

                today++;
                fastEMA = fastk * ad + one_minus_fastk * fastEMA;
                slowEMA = slowk * ad + one_minus_slowk * slowEMA;
                outReal[outIdx] = fastEMA - slowEMA;
                outIdx++;
            }

            outNBElement = outIdx;
            return RetCode.Success;
        }

        public static int AdOscLookback(int optInFastPeriod, int optInSlowPeriod)
        {
            if (optInFastPeriod is < 2 or > 100000)
            {
                return -1;
            }

            if (optInSlowPeriod is < 2 or > 100000)
            {
                return -1;
            }

            int slowestPeriod = optInFastPeriod < optInSlowPeriod
                ? optInSlowPeriod
                : optInFastPeriod;

            return EmaLookback(slowestPeriod);
        }
    }
}
