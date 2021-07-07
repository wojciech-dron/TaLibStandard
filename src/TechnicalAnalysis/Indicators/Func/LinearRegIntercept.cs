﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LinearRegIntercept.cs" company="GLPM">
//   Copyright (c) GLPM. All rights reserved.
// </copyright>
// <summary>
//   Defines LinearRegIntercept.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using TechnicalAnalysis.Abstractions;

namespace TechnicalAnalysis
{
    public static partial class TAMath
    {
        public static LinearRegIntercept LinearRegIntercept(int startIdx, int endIdx, double[] real, int timePeriod)
        {
            int outBegIdx = default;
            int outNBElement = default;
            double[] outReal = new double[endIdx - startIdx + 1];

            RetCode retCode = TACore.LinearRegIntercept(
                startIdx,
                endIdx,
                real,
                timePeriod,
                ref outBegIdx,
                ref outNBElement,
                ref outReal);
            
            return new LinearRegIntercept(retCode, outBegIdx, outNBElement, outReal);
        }

        public static LinearRegIntercept LinearRegIntercept(int startIdx, int endIdx, double[] real)
            => LinearRegIntercept(startIdx, endIdx, real, 14);

        public static LinearRegIntercept LinearRegIntercept(int startIdx, int endIdx, float[] real, int timePeriod)
            => LinearRegIntercept(startIdx, endIdx, real.ToDouble(), timePeriod);

        public static LinearRegIntercept LinearRegIntercept(int startIdx, int endIdx, float[] real)
            => LinearRegIntercept(startIdx, endIdx, real, 14);
    }

    public record LinearRegIntercept : IndicatorBase
    {
        public LinearRegIntercept(RetCode retCode, int begIdx, int nbElement, double[] real)
            : base(retCode, begIdx, nbElement)
        {
            Real = real;
        }

        public double[] Real { get; }
    }
}
