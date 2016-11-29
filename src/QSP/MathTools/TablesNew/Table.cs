﻿using System.Collections.Generic;
using QSP.LibraryExtension;
using static QSP.MathTools.Interpolation.Common;
using static QSP.MathTools.Interpolation.Interpolate1D;
using static QSP.Utilities.ExceptionHelpers;
using System;
using System.Linq;

namespace QSP.MathTools.TablesNew
{
    public class Table : ITableView
    {
        public int Dimension { get; }
        public IReadOnlyList<double> XValues { get; }
        public IReadOnlyList<ITable> FValues { get; }

        /// <exception cref="ArgumentException"></exception>
        public Table(IReadOnlyList<double> XValues, IReadOnlyList<ITable> FValues)
        {
            this.XValues = XValues;
            this.FValues = FValues;
            Dimension = FValues[0].Dimension + 1;

            Validate();
        }
        
        private void Validate()
        {
            Ensure<ArgumentException>(
                FValues.Count == XValues.Count &&
                (XValues.IsStrictlyDecreasing() || XValues.IsStrictlyIncreasing()) &&
                FValues.All(v => v.Dimension == Dimension));
        }

        public double ValueAt(IReadOnlyList<double> X)
        {
            var slice = new Slice<double>(X, 1);
            var index = GetIndex(XValues, X[0]);
            var f0 = FValues[index].ValueAt(slice);
            var f1 = FValues[index + 1].ValueAt(slice);
            return Interpolate(XValues[index], XValues[index + 1], f0, f1, X[0]);
        }
    }
}