///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;

namespace com.espertech.esper.filter
{
    /// <summary>Holds a range of double values with a minimum (start) value and a maximum (end) value. </summary>
    public sealed class DoubleRange : Range
    {
        private readonly int hashCode;

        /// <summary>Constructor - takes range endpoints. </summary>
        /// <param name="min">is the low endpoint</param>
        /// <param name="max">is the high endpoint</param>
        public DoubleRange(double? min, double? max)
        {
            this.Min = min;
            this.Max = max;

            if ((min != null) && (max != null))
            {
                if (min > max)
                {
                    this.Max = min;
                    this.Min = max;
                }
            }

            hashCode = 7;
            if (min != null)
            {
                hashCode = 31*hashCode;
                hashCode ^= min.GetHashCode();
            }
            if (max != null)
            {
                hashCode = 31*hashCode;
                hashCode ^= max.GetHashCode();
            }
        }

        /// <summary>Returns low endpoint. </summary>
        /// <value>low endpoint</value>
        public double? Min { get; private set; }

        public object HighEndpoint
        {
            get { return Max; }
        }

        public object LowEndpoint
        {
            get { return Min; }
        }

        /// <summary>Returns high endpoint. </summary>
        /// <value>high endpoint</value>
        public double? Max { get; private set; }

        public bool Equals(DoubleRange other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Min.Equals(Min) && other.Max.Equals(Max);
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
        /// </returns>
        /// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>. 
        ///                 </param><exception cref="T:System.NullReferenceException">The <paramref name="obj"/> parameter is null.
        ///                 </exception><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (DoubleRange)) return false;
            return Equals((DoubleRange) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Min.HasValue ? Min.Value.GetHashCode() : 0)*397) ^ (Max.HasValue ? Max.Value.GetHashCode() : 0);
            }
        }

        public override String ToString()
        {
            return "DoubleRange" +
                   " min=" + Min +
                   " max=" + Max;
        }
    }
}
