///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

namespace com.espertech.esper.epl.agg.service
{
    /// <summary>
    /// Implementation for handling aggregation with grouping by group-keys.
    /// </summary>
    public class AggSvcGroupByReclaimAgedEvalFuncConstant : AggSvcGroupByReclaimAgedEvalFunc
    {
        private readonly double _longValue;
    
        public AggSvcGroupByReclaimAgedEvalFuncConstant(double longValue)
        {
            _longValue = longValue;
        }

        public double? LongValue
        {
            get { return _longValue; }
        }
    }
}
