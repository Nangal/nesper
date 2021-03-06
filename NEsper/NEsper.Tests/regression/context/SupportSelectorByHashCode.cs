///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;

using com.espertech.esper.client.context;
using com.espertech.esper.compat.collections;

namespace com.espertech.esper.regression.context
{
    public class SupportSelectorByHashCode : ContextPartitionSelectorHash
    {
        private readonly ICollection<int> _hashes;

        public SupportSelectorByHashCode(ICollection<int> hashes)
        {
            _hashes = hashes;
        }
    
        public SupportSelectorByHashCode(int single)
        {
            _hashes = Collections.SingletonList(single);
        }

        public ICollection<int> Hashes
        {
            get { return _hashes; }
        }
    }
}
