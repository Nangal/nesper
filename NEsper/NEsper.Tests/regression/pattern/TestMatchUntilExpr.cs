///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;

using com.espertech.esper.client;
using com.espertech.esper.client.scopetest;
using com.espertech.esper.client.time;
using com.espertech.esper.metrics.instrumentation;
using com.espertech.esper.regression.support;
using com.espertech.esper.support.bean;
using com.espertech.esper.support.client;
using com.espertech.esper.support.epl;

using NUnit.Framework;

namespace com.espertech.esper.regression.pattern
{
    [TestFixture]
    public class TestMatchUntilExpr : SupportBeanConstants
    {
        private Configuration _config;

        [SetUp]
        public void SetUp()
        {
            _config = SupportConfigFactory.GetConfiguration();
            _config.AddEventType("A", typeof(SupportBean_A).FullName);
            _config.AddEventType("B", typeof(SupportBean_B).FullName);
            _config.AddEventType("C", typeof(SupportBean_C).FullName);
            _config.AddEventType("SupportBean", typeof(SupportBean).FullName);
            _config.AddNamespaceImport<SupportStaticMethodLib>();
        }

        [Test]
        public void TestOp()
        {
            EventCollection events = EventCollectionFactory.GetEventSetOne(0, 1000);
            CaseList testCaseList = new CaseList();
            EventExpressionCase testCase;

            testCase = new EventExpressionCase("a=A(id='A2') until D");
            testCase.Add("D1", "a[0]", events.GetEvent("A2"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("a=A until D");
            testCase.Add("D1", "a[0]", events.GetEvent("A1"), "a[1]", events.GetEvent("A2"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("b=B until a=A");
            testCase.Add("A1", "b[0]", null, "a", events.GetEvent("A1"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("b=B until D(id='D3')");
            testCase.Add("D3", "b[0]", events.GetEvent("B1"), "b[1]", events.GetEvent("B2"), "b[2]", events.GetEvent("B3"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("(a=A or b=B) until d=D(id='D3')");
            testCase.Add("D3", new Object[][]{
                    new Object[] {"a[0]", events.GetEvent("A1")},
                    new Object[] {"a[1]", events.GetEvent("A2")},
                    new Object[] {"b[0]", events.GetEvent("B1")},
                    new Object[] {"b[1]", events.GetEvent("B2")},
                    new Object[] {"b[2]", events.GetEvent("B3")},
                    new Object[] {"d", events.GetEvent("D3")}
            });
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("(a=A or b=B) until (g=G or d=D)");
            testCase.Add("D1", new Object[][]{
                    new Object[] {"a[0]", events.GetEvent("A1")},
                    new Object[] {"a[1]", events.GetEvent("A2")},
                    new Object[] {"b[0]", events.GetEvent("B1")},
                    new Object[] {"b[1]", events.GetEvent("B2")},
                    new Object[] {"d", events.GetEvent("D1")}
            });
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("(d=D) until a=A(id='A1')");
            testCase.Add("A1");
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("a=A until G(id='GX')");
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[2] a=A");
            testCase.Add("A2", "a[0]", events.GetEvent("A1"), "a[1]", events.GetEvent("A2"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[2:2] a=A");
            testCase.Add("A2", "a[0]", events.GetEvent("A1"), "a[1]", events.GetEvent("A2"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[1] a=A");
            testCase.Add("A1", "a[0]", events.GetEvent("A1"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[1:1] a=A");
            testCase.Add("A1", "a[0]", events.GetEvent("A1"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[3] a=A");
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[3] b=B");
            testCase.Add("B3", "b[0]", events.GetEvent("B1"), "b[1]", events.GetEvent("B2"), "b[2]", events.GetEvent("B3"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[4] (a=A or b=B)");
            testCase.Add("A2", "b[0]", events.GetEvent("B1"), "b[1]", events.GetEvent("B2"), "a[0]", events.GetEvent("A1"), "a[1]", events.GetEvent("A2"));
            testCaseList.AddTest(testCase);

            // the until ends the matching returning permanently false
            testCase = new EventExpressionCase("[2] b=B until a=A(id='A1')");
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[2] b=B until c=C");
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[2:2] b=B until g=G(id='G1')");
            testCase.Add("B2", "b[0]", events.GetEvent("B1"), "b[1]", events.GetEvent("B2"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[:4] b=B until g=G(id='G1')");
            testCase.Add("G1", "b[0]", events.GetEvent("B1"), "b[1]", events.GetEvent("B2"), "b[2]", events.GetEvent("B3"), "g", events.GetEvent("G1"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[:3] b=B until g=G(id='G1')");
            testCase.Add("G1", "b[0]", events.GetEvent("B1"), "b[1]", events.GetEvent("B2"), "b[2]", events.GetEvent("B3"), "g", events.GetEvent("G1"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[:2] b=B until g=G(id='G1')");
            testCase.Add("G1", "b[0]", events.GetEvent("B1"), "b[1]", events.GetEvent("B2"), "g", events.GetEvent("G1"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[:1] b=B until g=G(id='G1')");
            testCase.Add("G1", "b[0]", events.GetEvent("B1"), "g", events.GetEvent("G1"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[:1] b=B until a=A(id='A1')");
            testCase.Add("A1", "b[0]", null, "a", events.GetEvent("A1"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[1:] b=B until g=G(id='G1')");
            testCase.Add("G1", "b[0]", events.GetEvent("B1"), "b[1]", events.GetEvent("B2"), "b[2]", events.GetEvent("B3"), "g", events.GetEvent("G1"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[1:] b=B until a=A");
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[2:] b=B until a=A(id='A2')");
            testCase.Add("A2", "b[0]", events.GetEvent("B1"), "b[1]", events.GetEvent("B2"), "a", events.GetEvent("A2"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[2:] b=B until c=C");
            testCaseList.AddTest(testCase);

            // same event triggering both clauses, until always wins, match does not count
            testCase = new EventExpressionCase("[2:] b=B until e=B(id='B2')");
            testCaseList.AddTest(testCase);

            // same event triggering both clauses, until always wins, match does not count
            testCase = new EventExpressionCase("[1:] b=B until e=B(id='B1')");
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[1:2] b=B until a=A(id='A2')");
            testCase.Add("A2", "b[0]", events.GetEvent("B1"), "b[1]", events.GetEvent("B2"), "b[2]", null, "a", events.GetEvent("A2"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[1:3] b=B until G");
            testCase.Add("G1", "b[0]", events.GetEvent("B1"), "b[1]", events.GetEvent("B2"), "b[2]", events.GetEvent("B3"), "b[3]", null);
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[1:2] b=B until G");
            testCase.Add("G1", "b[0]", events.GetEvent("B1"), "b[1]", events.GetEvent("B2"), "b[2]", null);
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[1:10] b=B until F");
            testCase.Add("F1", "b[0]", events.GetEvent("B1"), "b[1]", events.GetEvent("B2"), "b[2]", null);
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[1:10] b=B until C");
            testCase.Add("C1", "b[0]", events.GetEvent("B1"), "b[1]", null);
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[0:1] b=B until C");
            testCase.Add("C1", "b[0]", events.GetEvent("B1"), "b[1]", null);
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("c=C -> [2] b=B -> d=D");
            testCase.Add("D3", "c", events.GetEvent("C1"), "b[0]", events.GetEvent("B2"), "b[1]", events.GetEvent("B3"), "d", events.GetEvent("D3"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[3] d=D or [3] b=B");
            testCase.Add("B3", "b[0]", events.GetEvent("B1"), "b[1]", events.GetEvent("B2"), "b[2]", events.GetEvent("B3"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[3] d=D or [4] b=B");
            testCase.Add("D3", "d[0]", events.GetEvent("D1"), "d[1]", events.GetEvent("D2"), "d[2]", events.GetEvent("D3"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[2] d=D and [2] b=B");
            testCase.Add("D2", "d[0]", events.GetEvent("D1"), "d[1]", events.GetEvent("D2"), "b[0]", events.GetEvent("B1"), "b[1]", events.GetEvent("B2"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("d=D until timer:interval(7 sec)");
            testCase.Add("E1", "d[0]", events.GetEvent("D1"), "d[1]", null, "d[2]", null);
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("every (d=D until b=B)");
            testCase.Add("B1", "d[0]", null, "b", events.GetEvent("B1"));
            testCase.Add("B2", "d[0]", null, "b", events.GetEvent("B2"));
            testCase.Add("B3", "d[0]", events.GetEvent("D1"), "d[1]", events.GetEvent("D2"), "d[2]", null, "b", events.GetEvent("B3"));
            testCaseList.AddTest(testCase);

            // note precendence: every is higher then until
            testCase = new EventExpressionCase("every d=D until b=B");
            testCase.Add("B1", "d[0]", null, "b", events.GetEvent("B1"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("(every d=D) until b=B");
            testCase.Add("B1", "d[0]", null, "b", events.GetEvent("B1"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("a=A until (every (timer:interval(6 sec) and not A))");
            testCase.Add("G1", "a[0]", events.GetEvent("A1"), "a[1]", events.GetEvent("A2"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("A until (every (timer:interval(7 sec) and not A))");
            testCase.Add("D3");
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[2] (a=A or b=B)");
            testCase.Add("B1", "a[0]", events.GetEvent("A1"), "b[0]", events.GetEvent("B1"), "b[1]", null);
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("every [2] a=A");
            testCase.Add("A2", new Object[][]{
                    new Object[] {"a[0]", events.GetEvent("A1")},
                    new Object[] {"a[1]", events.GetEvent("A2")},
            });
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("every [2] a=A until d=D");  // every has Precedence; ESPER-339
            testCase.Add("D1", new Object[][]{
                    new Object[] {"a[0]", events.GetEvent("A1")},
                    new Object[] {"a[1]", events.GetEvent("A2")},
                    new Object[] {"d", events.GetEvent("D1")},
            });
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("[3] (a=A or b=B)");
            testCase.Add("B2", "a[0]", events.GetEvent("A1"), "b[0]", events.GetEvent("B1"), "b[1]", events.GetEvent("B2"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("(a=A until b=B) until c=C");
            testCase.Add("C1", "a[0]", events.GetEvent("A1"), "b[0]", events.GetEvent("B1"), "c", events.GetEvent("C1"));
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("(a=A until b=B) until g=G");
            testCase.Add("G1", new Object[][]{new Object[] {"a[0]", events.GetEvent("A1")}, new Object[] {"b[0]", events.GetEvent("B1")},
                    new Object[] {"a[1]", events.GetEvent("A2")}, new Object[] {"b[1]", events.GetEvent("B2")},
                    new Object[] {"b[2]", events.GetEvent("B3")},
                    new Object[] {"g", events.GetEvent("G1")}
            });
            testCaseList.AddTest(testCase);

            testCase = new EventExpressionCase("B until not B");
            testCaseList.AddTest(testCase);

            PatternTestHarness util = new PatternTestHarness(events, testCaseList, GetType(), GetType().FullName);
            util.RunTest();
        }

        [Test]
        public void TestSelectArray()
        {
            EPServiceProvider epService = EPServiceProviderManager.GetDefaultProvider(_config);
            epService.Initialize();
            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.StartTest(epService, GetType(), GetType().FullName); }

            String stmt = "select a, b, a[0] as a0, a[0].id as a0Id, a[1] as a1, a[1].id as a1Id, a[2] as a2, a[2].id as a2Id from pattern [a=A until b=B]";
            SupportUpdateListener listener = new SupportUpdateListener();
            EPStatement statement = epService.EPAdministrator.CreateEPL(stmt);
            statement.Events += listener.Update;

            Object eventA1 = new SupportBean_A("A1");
            epService.EPRuntime.SendEvent(eventA1);

            Object eventA2 = new SupportBean_A("A2");
            epService.EPRuntime.SendEvent(eventA2);
            Assert.IsFalse(listener.IsInvoked);

            Object eventB1 = new SupportBean_B("B1");
            epService.EPRuntime.SendEvent(eventB1);

            EventBean theEvent = listener.AssertOneGetNewAndReset();
            EPAssertionUtil.AssertEqualsExactOrder((Object[])theEvent.Get("a"), new Object[] { eventA1, eventA2 });
            Assert.AreSame(eventA1, theEvent.Get("a0"));
            Assert.AreSame(eventA2, theEvent.Get("a1"));
            Assert.IsNull(theEvent.Get("a2"));
            Assert.AreEqual("A1", theEvent.Get("a0Id"));
            Assert.AreEqual("A2", theEvent.Get("a1Id"));
            Assert.IsNull(theEvent.Get("a2Id"));
            Assert.AreSame(eventB1, theEvent.Get("b"));

            // try wildcard
            stmt = "select * from pattern [a=A until b=B]";
            statement = epService.EPAdministrator.CreateEPL(stmt);
            statement.Events += listener.Update;

            epService.EPRuntime.SendEvent(eventA1);
            epService.EPRuntime.SendEvent(eventA2);
            Assert.IsFalse(listener.IsInvoked);
            epService.EPRuntime.SendEvent(eventB1);

            theEvent = listener.AssertOneGetNewAndReset();
            EPAssertionUtil.AssertEqualsExactOrder((Object[])theEvent.Get("a"), new Object[] { eventA1, eventA2 });
            Assert.AreSame(eventA1, theEvent.Get("a[0]"));
            Assert.AreSame(eventA2, theEvent.Get("a[1]"));
            Assert.IsNull(theEvent.Get("a[2]"));
            Assert.AreEqual("A1", theEvent.Get("a[0].id"));
            Assert.AreEqual("A2", theEvent.Get("a[1].id"));
            Assert.IsNull(theEvent.Get("a[2].id"));
            Assert.AreSame(eventB1, theEvent.Get("b"));

            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.EndTest(); }
        }

        [Test]
        public void TestUseFilter()
        {
            EPServiceProvider epService = EPServiceProviderManager.GetDefaultProvider(_config);
            epService.Initialize();
            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.StartTest(epService, GetType(), GetType().FullName); }

            String stmt;
            SupportUpdateListener listener;
            EPStatement statement;
            EventBean theEvent;

            stmt = "select * from pattern [a=A until b=B -> c=C(id = ('C' || a[0].id || a[1].id || b.id))]";
            listener = new SupportUpdateListener();
            statement = epService.EPAdministrator.CreateEPL(stmt);
            statement.Events += listener.Update;

            Object eventA1 = new SupportBean_A("A1");
            epService.EPRuntime.SendEvent(eventA1);

            Object eventA2 = new SupportBean_A("A2");
            epService.EPRuntime.SendEvent(eventA2);

            Object eventB1 = new SupportBean_B("B1");
            epService.EPRuntime.SendEvent(eventB1);

            epService.EPRuntime.SendEvent(new SupportBean_C("C1"));
            Assert.IsFalse(listener.IsInvoked);

            Object eventC1 = new SupportBean_C("CA1A2B1");
            epService.EPRuntime.SendEvent(eventC1);
            theEvent = listener.AssertOneGetNewAndReset();
            Assert.AreSame(eventA1, theEvent.Get("a[0]"));
            Assert.AreSame(eventA2, theEvent.Get("a[1]"));
            Assert.IsNull(theEvent.Get("a[2]"));
            Assert.AreSame(eventB1, theEvent.Get("b"));
            Assert.AreSame(eventC1, theEvent.Get("c"));
            statement.Dispose();

            // Test equals-optimization with array event
            stmt = "select * from pattern [a=A until b=B -> c=SupportBean(TheString = a[1].id)]";
            listener = new SupportUpdateListener();
            statement = epService.EPAdministrator.CreateEPL(stmt);
            statement.Events += listener.Update;

            epService.EPRuntime.SendEvent(new SupportBean_A("A1"));
            epService.EPRuntime.SendEvent(new SupportBean_A("A2"));
            epService.EPRuntime.SendEvent(new SupportBean_B("B1"));

            epService.EPRuntime.SendEvent(new SupportBean("A3", 20));
            Assert.IsFalse(listener.IsInvoked);

            epService.EPRuntime.SendEvent(new SupportBean("A2", 10));
            theEvent = listener.AssertOneGetNewAndReset();
            Assert.AreEqual(10, theEvent.Get("c.IntPrimitive"));
            statement.Dispose();

            // Test in-optimization
            stmt = "select * from pattern [a=A until b=B -> c=SupportBean(TheString in(a[2].id))]";
            listener = new SupportUpdateListener();
            statement = epService.EPAdministrator.CreateEPL(stmt);
            statement.Events += listener.Update;

            epService.EPRuntime.SendEvent(new SupportBean_A("A1"));
            epService.EPRuntime.SendEvent(new SupportBean_A("A2"));
            epService.EPRuntime.SendEvent(new SupportBean_A("A3"));
            epService.EPRuntime.SendEvent(new SupportBean_B("B1"));

            epService.EPRuntime.SendEvent(new SupportBean("A2", 20));
            Assert.IsFalse(listener.IsInvoked);

            epService.EPRuntime.SendEvent(new SupportBean("A3", 5));
            theEvent = listener.AssertOneGetNewAndReset();
            Assert.AreEqual(5, theEvent.Get("c.IntPrimitive"));
            statement.Dispose();

            // Test not-in-optimization
            stmt = "select * from pattern [a=A until b=B -> c=SupportBean(TheString!=a[0].id and TheString!=a[1].id and TheString!=a[2].id)]";
            listener = new SupportUpdateListener();
            statement = epService.EPAdministrator.CreateEPL(stmt);
            statement.Events += listener.Update;

            epService.EPRuntime.SendEvent(new SupportBean_A("A1"));
            epService.EPRuntime.SendEvent(new SupportBean_A("A2"));
            epService.EPRuntime.SendEvent(new SupportBean_A("A3"));
            epService.EPRuntime.SendEvent(new SupportBean_B("B1"));

            epService.EPRuntime.SendEvent(new SupportBean("A2", 20));
            Assert.IsFalse(listener.IsInvoked);
            epService.EPRuntime.SendEvent(new SupportBean("A1", 20));
            Assert.IsFalse(listener.IsInvoked);

            epService.EPRuntime.SendEvent(new SupportBean("A6", 5));
            theEvent = listener.AssertOneGetNewAndReset();
            Assert.AreEqual(5, theEvent.Get("c.IntPrimitive"));
            statement.Dispose();

            // Test range-optimization
            stmt = "select * from pattern [a=SupportBean(TheString like 'A%') until b=SupportBean(TheString like 'B%') -> c=SupportBean(IntPrimitive between a[0].IntPrimitive and a[1].IntPrimitive)]";
            listener = new SupportUpdateListener();
            statement = epService.EPAdministrator.CreateEPL(stmt);
            statement.Events += listener.Update;

            epService.EPRuntime.SendEvent(new SupportBean("A1", 5));
            epService.EPRuntime.SendEvent(new SupportBean("A2", 8));
            epService.EPRuntime.SendEvent(new SupportBean("B1", -1));

            epService.EPRuntime.SendEvent(new SupportBean("E1", 20));
            Assert.IsFalse(listener.IsInvoked);
            epService.EPRuntime.SendEvent(new SupportBean("E2", 3));
            Assert.IsFalse(listener.IsInvoked);

            epService.EPRuntime.SendEvent(new SupportBean("E3", 5));
            theEvent = listener.AssertOneGetNewAndReset();
            Assert.AreEqual(5, theEvent.Get("c.IntPrimitive"));

            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.EndTest(); }
        }

        [Test]
        public void TestRepeatUseTags()
        {
            EPServiceProvider epService = EPServiceProviderManager.GetDefaultProvider(_config);
            epService.Initialize();
            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.StartTest(epService, GetType(), GetType().FullName); }

            String stmt = "select * from pattern [every [2] (a=A() -> b=B(id=a.id))]";
            SupportUpdateListener listener = new SupportUpdateListener();
            EPStatement statement = epService.EPAdministrator.CreateEPL(stmt);
            statement.Events += listener.Update;

            epService.EPRuntime.SendEvent(new SupportBean_A("A1"));
            epService.EPRuntime.SendEvent(new SupportBean_B("A1"));
            epService.EPRuntime.SendEvent(new SupportBean_A("A2"));
            epService.EPRuntime.SendEvent(new SupportBean_B("A2"));
            Assert.IsTrue(listener.IsInvoked);

            statement.Dispose();

            // test with timer:interval
            epService.EPAdministrator.Configuration.AddEventType<SupportBean>();
            epService.EPRuntime.SendEvent(new CurrentTimeEvent(0));
            String query = "select * from pattern [every ([2:]e1=SupportBean(TheString='2') until timer:interval(5))->([2:]e2=SupportBean(TheString='3') until timer:interval(2))]";

            statement = epService.EPAdministrator.CreateEPL(query);

            epService.EPRuntime.SendEvent(new SupportBean("2", 0));
            epService.EPRuntime.SendEvent(new SupportBean("2", 0));
            epService.EPRuntime.SendEvent(new CurrentTimeEvent(5000));

            epService.EPRuntime.SendEvent(new SupportBean("3", 0));
            epService.EPRuntime.SendEvent(new SupportBean("3", 0));
            epService.EPRuntime.SendEvent(new SupportBean("3", 0));
            epService.EPRuntime.SendEvent(new SupportBean("3", 0));
            epService.EPRuntime.SendEvent(new CurrentTimeEvent(10000));

            epService.EPRuntime.SendEvent(new SupportBean("2", 0));
            epService.EPRuntime.SendEvent(new SupportBean("2", 0));
            epService.EPRuntime.SendEvent(new CurrentTimeEvent(15000));

            // test followed by 3 streams
            epService.EPAdministrator.DestroyAllStatements();
            listener.Reset();
            String epl = "select * from pattern [ every [2] A=SupportBean(TheString='1') " +
                    "-> [2] B=SupportBean(TheString='2' and IntPrimitive=A[0].IntPrimitive)" +
                    "-> [2] C=SupportBean(TheString='3' and IntPrimitive=A[0].IntPrimitive)]";
            epService.EPAdministrator.CreateEPL(epl).Events += listener.Update;

            epService.EPRuntime.SendEvent(new SupportBean("1", 10));
            epService.EPRuntime.SendEvent(new SupportBean("1", 20));
            epService.EPRuntime.SendEvent(new SupportBean("2", 10));
            epService.EPRuntime.SendEvent(new SupportBean("2", 10));
            epService.EPRuntime.SendEvent(new SupportBean("3", 10));
            epService.EPRuntime.SendEvent(new SupportBean("3", 10));
            Assert.IsTrue(listener.IsInvoked);

            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.EndTest(); }
        }

        [Test]
        public void TestArrayFunctionRepeat()
        {
            EPServiceProvider epService = EPServiceProviderManager.GetDefaultProvider(_config);
            epService.Initialize();
            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.StartTest(epService, GetType(), GetType().FullName); }

            String stmt = "select SupportStaticMethodLib.ArrayLength(a) as length, FakeSystem.Array.GetLength(a) as l2 from pattern [[1:] a=A until B]";
            SupportUpdateListener listener = new SupportUpdateListener();
            EPStatement statement = epService.EPAdministrator.CreateEPL(stmt);
            statement.Events += listener.Update;

            epService.EPRuntime.SendEvent(new SupportBean_A("A1"));
            epService.EPRuntime.SendEvent(new SupportBean_A("A2"));
            epService.EPRuntime.SendEvent(new SupportBean_A("A3"));
            epService.EPRuntime.SendEvent(new SupportBean_B("A2"));
            EventBean theEvent = listener.AssertOneGetNewAndReset();
            Assert.AreEqual(3, theEvent.Get("length"));
            Assert.AreEqual(3, theEvent.Get("l2"));

            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.EndTest(); }
        }

        [Test]
        public void TestExpressionBounds()
        {
            EPServiceProvider epService = EPServiceProviderManager.GetDefaultProvider(_config);
            epService.Initialize();
            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.StartTest(epService, GetType(), GetType().FullName); }

            SupportUpdateListener listener = new SupportUpdateListener();

            epService.EPAdministrator.Configuration.AddVariable("lower", typeof(int), null);
            epService.EPAdministrator.Configuration.AddVariable("upper", typeof(int), null);
            epService.EPAdministrator.Configuration.AddEventType<SupportBean>();
            epService.EPAdministrator.Configuration.AddEventType<SupportBean_S0>();

            // test variables - closed bounds
            epService.EPRuntime.SetVariableValue("lower", 2);
            epService.EPRuntime.SetVariableValue("upper", 3);
            String stmtOne = "[lower:upper] a=SupportBean (TheString = 'A') until b=SupportBean (TheString = 'B')";
            ValidateStmt(epService, stmtOne, 0, false, null);
            ValidateStmt(epService, stmtOne, 1, false, null);
            ValidateStmt(epService, stmtOne, 2, true, 2);
            ValidateStmt(epService, stmtOne, 3, true, 3);
            ValidateStmt(epService, stmtOne, 4, true, 3);
            ValidateStmt(epService, stmtOne, 5, true, 3);

            // test variables - half open
            epService.EPRuntime.SetVariableValue("lower", 3);
            epService.EPRuntime.SetVariableValue("upper", null);
            String stmtTwo = "[lower:] a=SupportBean (TheString = 'A') until b=SupportBean (TheString = 'B')";
            ValidateStmt(epService, stmtTwo, 0, false, null);
            ValidateStmt(epService, stmtTwo, 1, false, null);
            ValidateStmt(epService, stmtTwo, 2, false, null);
            ValidateStmt(epService, stmtTwo, 3, true, 3);
            ValidateStmt(epService, stmtTwo, 4, true, 4);
            ValidateStmt(epService, stmtTwo, 5, true, 5);

            // test variables - half closed
            epService.EPRuntime.SetVariableValue("lower", null);
            epService.EPRuntime.SetVariableValue("upper", 2);
            String stmtThree = "[:upper] a=SupportBean (TheString = 'A') until b=SupportBean (TheString = 'B')";
            ValidateStmt(epService, stmtThree, 0, true, null);
            ValidateStmt(epService, stmtThree, 1, true, 1);
            ValidateStmt(epService, stmtThree, 2, true, 2);
            ValidateStmt(epService, stmtThree, 3, true, 2);
            ValidateStmt(epService, stmtThree, 4, true, 2);
            ValidateStmt(epService, stmtThree, 5, true, 2);

            // test followed-by - bounded
            epService.EPAdministrator.CreateEPL("@Name('S1') select * from pattern [s0=SupportBean_S0 -> [s0.id] b=SupportBean]").Events += listener.Update;
            epService.EPRuntime.SendEvent(new SupportBean_S0(2));
            epService.EPRuntime.SendEvent(new SupportBean("E1", 1));
            Assert.IsFalse(listener.IsInvoked);

            epService.EPRuntime.SendEvent(new SupportBean("E2", 2));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), "b[0].TheString,b[1].TheString".Split(','), new Object[] { "E1", "E2" });

            // test substitution parameter
            String epl = "select * from pattern[[?] SupportBean]";
            EPPreparedStatement prepared = epService.EPAdministrator.PrepareEPL(epl);
            prepared.SetObject(1, 2);
            EPStatement stmt = epService.EPAdministrator.Create(prepared);


            // test exactly-1
            epService.EPRuntime.SendEvent(new CurrentTimeEvent(0));
            String eplExact1 = "select * from pattern [a=A -> [1] every (timer:interval(10) and not B)]";
            EPStatement stmtExact1 = epService.EPAdministrator.CreateEPL(eplExact1);
            stmtExact1.Events += listener.Update;

            epService.EPRuntime.SendEvent(new CurrentTimeEvent(5000));
            epService.EPRuntime.SendEvent(new SupportBean_A("A1"));

            epService.EPRuntime.SendEvent(new CurrentTimeEvent(6000));
            epService.EPRuntime.SendEvent(new SupportBean_B("B1"));
            Assert.IsFalse(listener.IsInvoked);

            epService.EPRuntime.SendEvent(new CurrentTimeEvent(15999));
            Assert.IsFalse(listener.IsInvoked);
            epService.EPRuntime.SendEvent(new CurrentTimeEvent(16000));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), "a.Id".Split(','), new Object[] { "A1" });

            epService.EPRuntime.SendEvent(new CurrentTimeEvent(999999));
            Assert.IsFalse(listener.IsInvoked);
            stmtExact1.Dispose();

            // test until
            epService.EPRuntime.SendEvent(new CurrentTimeEvent(1000000));
            String eplUntilOne = "select * from pattern [a=A -> b=B until ([1] every (timer:interval(10) and not C))]";
            epService.EPAdministrator.CreateEPL(eplUntilOne).Events += listener.Update;

            epService.EPRuntime.SendEvent(new CurrentTimeEvent(1005000));
            epService.EPRuntime.SendEvent(new SupportBean_A("A1"));

            epService.EPRuntime.SendEvent(new CurrentTimeEvent(1006000));
            epService.EPRuntime.SendEvent(new SupportBean_B("B1"));
            epService.EPRuntime.SendEvent(new CurrentTimeEvent(1014999));
            epService.EPRuntime.SendEvent(new SupportBean_B("B2"));
            epService.EPRuntime.SendEvent(new SupportBean_C("C1"));
            epService.EPRuntime.SendEvent(new CurrentTimeEvent(1015000));
            Assert.IsFalse(listener.IsInvoked);

            epService.EPRuntime.SendEvent(new CurrentTimeEvent(1024998));
            Assert.IsFalse(listener.IsInvoked);
            epService.EPRuntime.SendEvent(new CurrentTimeEvent(1024999));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), "a.Id,b[0].Id,b[1].Id".Split(','), new Object[] { "A1", "B1", "B2" });

            epService.EPRuntime.SendEvent(new CurrentTimeEvent(1999999));
            Assert.IsFalse(listener.IsInvoked);

            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.EndTest(); }
        }

        public void TestBoundRepeatWithNot()
        {
            EPServiceProvider engine = EPServiceProviderManager.GetDefaultProvider(_config);
            engine.Initialize();

            String[] fields = "e[0].IntPrimitive,e[1].IntPrimitive".Split(',');
            String epl = "select * from pattern [every [2] (e = SupportBean(TheString='A') and not SupportBean(TheString='B'))]";
            EPStatement statement = engine.EPAdministrator.CreateEPL(epl);
            SupportUpdateListener listener = new SupportUpdateListener();
            statement.Events += listener.Update;

            engine.EPRuntime.SendEvent(new SupportBean("A", 1));
            engine.EPRuntime.SendEvent(new SupportBean("A", 2));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[] { 1, 2 });

            engine.EPRuntime.SendEvent(new SupportBean("A", 3));
            engine.EPRuntime.SendEvent(new SupportBean("B", 4));
            engine.EPRuntime.SendEvent(new SupportBean("A", 5));
            Assert.IsFalse(listener.IsInvoked);

            engine.EPRuntime.SendEvent(new SupportBean("A", 6));
            EPAssertionUtil.AssertProps(listener.AssertOneGetNewAndReset(), fields, new Object[] { 5, 6 });
        }

        private void ValidateStmt(EPServiceProvider engine, String stmtText, int numEventsA, bool match, int? matchCount)
        {
            SupportUpdateListener listener = new SupportUpdateListener();
            EPStatement stmt = engine.EPAdministrator.CreatePattern(stmtText);
            stmt.Events += listener.Update;

            for (int i = 0; i < numEventsA; i++)
            {
                engine.EPRuntime.SendEvent(new SupportBean("A", i));
            }
            Assert.IsFalse(listener.IsInvoked);
            engine.EPRuntime.SendEvent(new SupportBean("B", -1));

            Assert.AreEqual(match, listener.IsInvoked);
            if (!match)
            {
                return;
            }
            Object valueATag = listener.AssertOneGetNewAndReset().Get("a");
            if (matchCount == null)
            {
                Assert.IsNull(valueATag);
            }
            else
            {
                Array array = valueATag as Array;
                Assert.AreEqual((int)matchCount, array.Length);
            }
        }

        [Test]
        public void TestInvalid()
        {
            EPServiceProvider epService = EPServiceProviderManager.GetDefaultProvider(_config);
            epService.Initialize();
            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.StartTest(epService, GetType(), GetType().FullName); }

            epService.EPAdministrator.Configuration.AddEventType<SupportBean>();

            TryInvalid(epService, "[:0] A until B", "Incorrect range specification, a bounds value of zero or negative value is not allowed [[:0] A until B]");
            TryInvalid(epService, "[10:4] A", "Incorrect range specification, lower bounds value '10' is higher then higher bounds '4' [[10:4] A]");
            TryInvalid(epService, "[-1] A", "Incorrect range specification, a bounds value of zero or negative value is not allowed [[-1] A]");
            TryInvalid(epService, "[4:6] A", "Variable bounds repeat operator requires an until-expression [[4:6] A]");
            TryInvalid(epService, "[0:0] A", "Incorrect range specification, a bounds value of zero or negative value is not allowed [[0:0] A]");
            TryInvalid(epService, "[0] A", "Incorrect range specification, a bounds value of zero or negative value is not allowed [[0] A]");
            TryInvalid(epService, "[1] a=A(a[0].id='a')", "Failed to validate filter expression 'a[0].id=\"a\"': Property named 'a[0].id' is not valid in any stream [[1] a=A(a[0].id='a')]");
            TryInvalid(epService, "a=A -> B(a[0].id='a')", "Failed to validate filter expression 'a[0].id=\"a\"': Property named 'a[0].id' is not valid in any stream [a=A -> B(a[0].id='a')]");
            TryInvalid(epService, "(a=A until c=B) -> c=C", "Tag 'c' for event 'C' has already been declared for events of type com.espertech.esper.support.bean.SupportBean_B [(a=A until c=B) -> c=C]");
            TryInvalid(epService, "((a=A until b=B) until a=A)", "Tag 'a' for event 'A' used in the repeat-until operator cannot also appear in other filter expressions [((a=A until b=B) until a=A)]");
            TryInvalid(epService, "a=SupportBean -> [a.TheString] b=SupportBean", "Match-until bounds value expressions must return a numeric value [a=SupportBean -> [a.TheString] b=SupportBean]");
            TryInvalid(epService, "a=SupportBean -> [:a.TheString] b=SupportBean", "Match-until bounds value expressions must return a numeric value [a=SupportBean -> [:a.TheString] b=SupportBean]");
            TryInvalid(epService, "a=SupportBean -> [a.TheString:1] b=SupportBean", "Match-until bounds value expressions must return a numeric value [a=SupportBean -> [a.TheString:1] b=SupportBean]");

            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.EndTest(); }
        }

        private void TryInvalid(EPServiceProvider epService, String pattern, String message)
        {
            try
            {
                epService.EPAdministrator.CreatePattern(pattern);
                Assert.Fail();
            }
            catch (EPException ex)
            {
                Assert.AreEqual(message, ex.Message);
            }
        }
    }
}

namespace FakeSystem
{
    public static class Array
    {
        public static int GetLength(System.Array array)
        {
            return array.Length;
        }
    }
}
