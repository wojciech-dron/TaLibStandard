using TechnicalAnalysis.Candles.CandleSeparatingLines;
using TechnicalAnalysis.Common;

// ReSharper disable once CheckNamespace
namespace TechnicalAnalysis
{
    public static partial class TAMath
    {
        public static CandleSeparatingLinesResult CdlSeparatingLines(
            int startIdx, int endIdx, double[] open, double[] high, double[] low, double[] close)
        {
            RetCode retCode = new CandleSeparatingLines(open, high, low, close)
                .TryCompute(startIdx, endIdx, out int begIdx, out int nbElement, out int[] ints);
            
            return new CandleSeparatingLinesResult(retCode, begIdx, nbElement, ints);
        }

        public static CandleSeparatingLinesResult CdlSeparatingLines(
            int startIdx, int endIdx, float[] open, float[] high, float[] low, float[] close)
        {
            return CdlSeparatingLines(startIdx, endIdx,
                open.ToDouble(), high.ToDouble(), low.ToDouble(), close.ToDouble());
        }
    }
}