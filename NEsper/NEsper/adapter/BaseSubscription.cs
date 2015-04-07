///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

using com.espertech.esper.client;
using com.espertech.esper.compat.threading;
using com.espertech.esper.core.context.util;
using com.espertech.esper.core.service;
using com.espertech.esper.core.service.multimatch;
using com.espertech.esper.filter;

namespace com.espertech.esper.adapter
{
    /// <summary>
    /// Subscription is a concept for selecting events for processing out of all 
    /// events available from an engine instance.
    /// </summary>
    public abstract class BaseSubscription : Subscription, FilterHandleCallback
    {
        /// <summary>The event type of the events we are subscribing for. </summary>
        public String EventTypeName { get; set; }
    
        /// <summary>The name of the subscription. </summary>
        public String SubscriptionName { get; set; }

        public OutputAdapter Adapter { get; private set; }

        public abstract string StatementId { get; }

        public abstract bool IsSubSelect { get; }

        public abstract void MatchFound(EventBean theEvent, ICollection<FilterHandleCallback> allStmtMatches);

        /// <summary>Ctor, assigns default name. </summary>
        protected BaseSubscription()
        {
            SubscriptionName = "default";
        }

        public void RegisterAdapter(OutputAdapter adapter)
        {
            Adapter = adapter;
            RegisterAdapter(((AdapterSPI) adapter).EPServiceProvider);
        }
    
        /// <summary>Register an adapter. </summary>
        /// <param name="epService">engine</param>
        public void RegisterAdapter(EPServiceProvider epService)
        {
            var spi = (EPServiceProviderSPI) epService;
            var eventType = spi.EventAdapterService.GetEventTypeByName(EventTypeName);
            var fvs = new FilterSpecCompiled(eventType, null, new IList<FilterSpecParam>[0], null).GetValueSet(null, null, null);
    
            var name = "subscription:" + SubscriptionName;
            var metricsHandle = spi.MetricReportingService.GetStatementHandle(name, name);
            var statementHandle = new EPStatementHandle(name, name, name, StatementType.ESPERIO, name, false, metricsHandle, 0, false, false, MultiMatchHandlerFactory.DefaultHandler);
            var agentHandle = new EPStatementAgentInstanceHandle(statementHandle, ReaderWriterLockManager.CreateDefaultLock(), -1, new StatementAgentInstanceFilterVersion());
            var registerHandle = new EPStatementHandleCallback(agentHandle, this);
            spi.FilterService.Add(fvs, registerHandle);
        }
    }
}