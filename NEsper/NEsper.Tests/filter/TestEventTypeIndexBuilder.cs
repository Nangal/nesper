///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;

using com.espertech.esper.client;
using com.espertech.esper.compat;
using com.espertech.esper.support.bean;
using com.espertech.esper.support.events;
using com.espertech.esper.support.filter;

using NUnit.Framework;

namespace com.espertech.esper.filter
{
    [TestFixture]
    public class TestEventTypeIndexBuilder 
    {
        private EventTypeIndex _eventTypeIndex;
        private EventTypeIndexBuilder _indexBuilder;
    
        private EventType _typeOne;
        private EventType _typeTwo;
    
        private FilterValueSet _valueSetOne;
        private FilterValueSet _valueSetTwo;
    
        private FilterHandle _callbackOne;
        private FilterHandle _callbackTwo;

        private readonly FilterServiceGranularLockFactoryReentrant _lockFactory =
            new FilterServiceGranularLockFactoryReentrant();
    
        [SetUp]
        public void SetUp()
        {
            _eventTypeIndex = new EventTypeIndex(_lockFactory);
            _indexBuilder = new EventTypeIndexBuilder(_eventTypeIndex);
    
            _typeOne = SupportEventTypeFactory.CreateBeanType(typeof(SupportBean));
            _typeTwo = SupportEventTypeFactory.CreateBeanType(typeof(SupportBeanSimple));
    
            _valueSetOne = SupportFilterSpecBuilder.Build(_typeOne, new Object[0]).GetValueSet(null, null, null);
            _valueSetTwo = SupportFilterSpecBuilder.Build(_typeTwo, new Object[0]).GetValueSet(null, null, null);
    
            _callbackOne = new SupportFilterHandle();
            _callbackTwo = new SupportFilterHandle();
        }
    
        [Test]
        public void TestAddRemove()
        {
            Assert.IsNull(_eventTypeIndex.Get(_typeOne));
            Assert.IsNull(_eventTypeIndex.Get(_typeTwo));
    
            _indexBuilder.Add(_valueSetOne, _callbackOne, _lockFactory);
            _indexBuilder.Add(_valueSetTwo, _callbackTwo, _lockFactory);
    
            Assert.IsTrue(_eventTypeIndex.Get(_typeOne) != null);
            Assert.IsTrue(_eventTypeIndex.Get(_typeTwo) != null);
    
            try
            {
                _indexBuilder.Add(_valueSetOne, _callbackOne, _lockFactory);
                Assert.IsTrue(false);
            }
            catch (IllegalStateException ex)
            {
                // Expected exception
            }
    
            _indexBuilder.Remove(_callbackOne);
            _indexBuilder.Add(_valueSetOne, _callbackOne, _lockFactory);
            _indexBuilder.Remove(_callbackOne);
        }
    }
}