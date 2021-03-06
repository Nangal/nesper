///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;

using com.espertech.esper.epl.agg.aggregator;

namespace com.espertech.esper.support.epl
{
    [Serializable]
    public class SupportPluginAggregationMethodTwo : AggregationMethod
    {
        public void Clear()
        {
        }
    
        public void Enter(Object value)
        {
        }
    
        public void Leave(Object value)
        {
        }

        public object Value
        {
            get { return null; }
        }

        public Type ValueType
        {
            get { return null; }
        }
    }
}
