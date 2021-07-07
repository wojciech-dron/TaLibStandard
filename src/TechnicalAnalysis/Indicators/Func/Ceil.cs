﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Ceil.cs" company="GLPM">
//   Copyright (c) GLPM. All rights reserved.
// </copyright>
// <summary>
//   Defines Ceil.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using TechnicalAnalysis.Abstractions;

namespace TechnicalAnalysis
{
    public static partial class TAMath
    {
        public static Ceil Ceil(int startIdx, int endIdx, double[] real)
        {
            int outBegIdx = default;
            int outNBElement = default;
            double[] outReal = new double[endIdx - startIdx + 1];

            RetCode retCode = TACore.Ceil(startIdx, endIdx, real, ref outBegIdx, ref outNBElement, ref outReal);
            
            return new Ceil(retCode, outBegIdx, outNBElement, outReal);
        }

        public static Ceil Ceil(int startIdx, int endIdx, float[] real)
            => Ceil(startIdx, endIdx, real.ToDouble());
    }

    public record Ceil : IndicatorBase
    {
        public Ceil(RetCode retCode, int begIdx, int nbElement, double[] real)
            : base(retCode, begIdx, nbElement)
        {
            Real = real;
        }

        public double[] Real { get; }
    }
}
