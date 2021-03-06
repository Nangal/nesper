///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Reflection;

using com.espertech.esper.client;
using com.espertech.esper.compat.collections;
using com.espertech.esper.core.context.util;
using com.espertech.esper.core.service;
using com.espertech.esper.core.start;
using com.espertech.esper.epl.db;
using com.espertech.esper.epl.expression.core;
using com.espertech.esper.epl.spec;
using com.espertech.esper.epl.variable;
using com.espertech.esper.events;
using com.espertech.esper.schedule;
using com.espertech.esper.util;
using com.espertech.esper.view;

using XLR8.CGLib;

namespace com.espertech.esper.epl.core
{
	/// <summary>
	/// Factory for method-invocation data provider streams.
	/// </summary>
	public class MethodPollingViewableFactory
	{
	    /// <summary>
	    /// Creates a method-invocation polling view for use as a stream that calls a method, or pulls results from cache.
	    /// </summary>
	    /// <param name="streamNumber">the stream number</param>
	    /// <param name="methodStreamSpec">defines the class and method to call</param>
	    /// <param name="eventAdapterService">for creating event types and events</param>
	    /// <param name="epStatementAgentInstanceHandle">for time-based callbacks</param>
	    /// <param name="methodResolutionService">for resolving classes and imports</param>
	    /// <param name="engineImportService">for resolving configurations</param>
	    /// <param name="schedulingService">for scheduling callbacks in expiry-time based caches</param>
	    /// <param name="scheduleBucket">for schedules within the statement</param>
	    /// <param name="exprEvaluatorContext">expression evaluation context</param>
	    /// <param name="variableService">The variable service.</param>
	    /// <param name="contextName">Name of the context.</param>
	    /// <param name="dataCacheFactory">The data cache factory.</param>
	    /// <param name="statementContext">The statement context.</param>
	    /// <returns>
	    /// pollable view
	    /// </returns>
	    /// <exception cref="ExprValidationException">
	    /// Variable by name ' + variableMetaData.VariableName + ' has been declared for context ' + variableMetaData.ContextPartitionName + ' and can only be used within the same context
	    /// or
	    /// or
	    /// Invalid return type for static method ' + methodFastClass.Name + ' of class ' + methodStreamSpec.ClassName + ', expecting a type
	    /// </exception>
	    /// <throws>ExprValidationException if the expressions cannot be validated or the method descriptorhas incorrect class and method names, or parameter number and types don't match
	    /// </throws>
	    public static HistoricalEventViewable CreatePollMethodView(
	        int streamNumber,
	        MethodStreamSpec methodStreamSpec,
	        EventAdapterService eventAdapterService,
	        EPStatementAgentInstanceHandle epStatementAgentInstanceHandle,
	        MethodResolutionService methodResolutionService,
	        EngineImportService engineImportService,
	        SchedulingService schedulingService,
	        ScheduleBucket scheduleBucket,
	        ExprEvaluatorContext exprEvaluatorContext,
	        VariableService variableService,
	        string contextName,
	        DataCacheFactory dataCacheFactory,
	        StatementContext statementContext)
	    {
	        var variableMetaData = variableService.GetVariableMetaData(methodStreamSpec.ClassName);
	        MethodPollingExecStrategyEnum strategy;
	        VariableReader variableReader;
	        string variableName;

	        // Try to resolve the method
	        MethodInfo methodReflection;
	        FastMethod methodFastClass;
	        Type declaringClass;
	        object invocationTarget;

	        try
			{
			    if (variableMetaData != null)
			    {
			        variableName = variableMetaData.VariableName;
			        if (variableMetaData.ContextPartitionName != null)
			        {
			            if (contextName == null || !contextName.Equals(variableMetaData.ContextPartitionName))
			            {
			                throw new ExprValidationException(
			                    "Variable by name '" + variableMetaData.VariableName + "' has been declared for context '" +
			                    variableMetaData.ContextPartitionName + "' and can only be used within the same context");
			            }
			            strategy = MethodPollingExecStrategyEnum.TARGET_VAR_CONTEXT;
			            variableReader = null;
			            invocationTarget = null;
			        }
			        else
			        {
			            variableReader = variableService.GetReader(methodStreamSpec.ClassName, EPStatementStartMethodConst.DEFAULT_AGENT_INSTANCE_ID);
			            if (variableMetaData.IsConstant)
			            {
			                invocationTarget = variableReader.Value;
			                if (invocationTarget is EventBean)
			                {
			                    invocationTarget = ((EventBean) invocationTarget).Underlying;
			                }
			                strategy = MethodPollingExecStrategyEnum.TARGET_CONST;
			            }
			            else
			            {
			                invocationTarget = null;
			                strategy = MethodPollingExecStrategyEnum.TARGET_VAR;
			            }
			        }
			        methodReflection = methodResolutionService.ResolveNonStaticMethod(
			            variableMetaData.VariableType, methodStreamSpec.MethodName);
			    }
			    else
			    {
			        methodReflection = methodResolutionService.ResolveMethod(
			            methodStreamSpec.ClassName, methodStreamSpec.MethodName);
			        invocationTarget = null;
			        variableReader = null;
			        variableName = null;
			        strategy = MethodPollingExecStrategyEnum.TARGET_CONST;
			    }

			    declaringClass = methodReflection.DeclaringType;
			    methodFastClass = FastClass.CreateMethod(methodReflection);
			}
	        catch(ExprValidationException)
	        {
	            throw;
	        }
			catch(Exception e)
			{
				throw new ExprValidationException(e.Message, e);
			}

	        // Determine object type returned by method
	        var beanClass = methodFastClass.ReturnType;
	        if ((beanClass == typeof(void)) || (beanClass.IsBuiltinDataType()))
	        {
                throw new ExprValidationException("Invalid return type for static method '" + methodFastClass.Name + "' of class '" + methodStreamSpec.ClassName + "', expecting a class");
	        }

            var isCollection = false;
            var isIterator = false;
            Type collectionClass = null;
            Type iteratorClass = null;

	        if (methodFastClass.ReturnType.IsArray)
	        {
	            beanClass = methodFastClass.ReturnType.GetElementType();
            }
            else if (!methodFastClass.ReturnType.IsGenericStringDictionary())
            {
                isCollection = methodFastClass.ReturnType.IsGenericCollection();
                if (isCollection)
                {
                    collectionClass = beanClass.GetGenericType(0);
                    beanClass = collectionClass;
                }

                var beanEnumerable = methodFastClass.ReturnType.FindGenericInterface(typeof(IEnumerable<>));
                if (beanEnumerable != null)
                {
                    isIterator = true;
                    iteratorClass = beanEnumerable.GetGenericType(0);
                    beanClass = iteratorClass;
                }
                else
                {
                    var beanEnumerator = methodFastClass.ReturnType.FindGenericInterface(typeof(IEnumerator<>));
                    if (beanEnumerator != null)
                    {
                        isIterator = true;
                        iteratorClass = beanEnumerator.GetGenericType(0);
                        beanClass = iteratorClass;
                    }
                }
            }

            // If the method returns a Map, look up the map type
            IDictionary<string, object> mapType = null;
            String mapTypeName = null;

            if ((methodFastClass.ReturnType.IsGenericStringDictionary()) ||
                (methodFastClass.ReturnType.IsArray && methodFastClass.ReturnType.GetElementType().IsGenericStringDictionary()) ||
                (methodFastClass.ReturnType.IsGenericCollection() && methodFastClass.ReturnType.GetGenericType(0).IsGenericStringDictionary()) ||
                (methodFastClass.ReturnType.IsGenericEnumerator() && methodFastClass.ReturnType.GetGenericType(0).IsGenericStringDictionary()) ||
                (methodFastClass.ReturnType.IsGenericEnumerable() && methodFastClass.ReturnType.GetGenericType(0).IsGenericStringDictionary()))
            {
                var metadata =
                    (variableMetaData != null) ?
                    (GetCheckMetadataVariable(methodStreamSpec.MethodName, variableMetaData, variableReader, methodResolutionService, typeof(IDictionary<string, object>))) :
                    (GetCheckMetadataNonVariable(methodStreamSpec.MethodName, methodStreamSpec.ClassName, methodResolutionService, typeof(IDictionary<string, object>)));
                mapTypeName = metadata.TypeName;
                mapType = (IDictionary<string, object>)metadata.TypeMetadata;
            }


	        // If the method returns an Object[] or Object[][], look up the type information
            IDictionary<string, object> oaType = null;
            String oaTypeName = null;

            if ((methodFastClass.ReturnType == typeof(object[])) ||
                (methodFastClass.ReturnType == typeof(object[][])) ||
                (methodFastClass.ReturnType.IsGenericCollection() && methodFastClass.ReturnType.GetGenericType(0) == typeof(object[])) ||
                (methodFastClass.ReturnType.IsGenericEnumerator() && methodFastClass.ReturnType.GetGenericType(0) == typeof(object[])) ||
                (methodFastClass.ReturnType.IsGenericEnumerable() && methodFastClass.ReturnType.GetGenericType(0) == typeof(object[])))
            {
                var metadata =
                    (variableMetaData != null) ?
                    (GetCheckMetadataVariable(methodStreamSpec.MethodName, variableMetaData, variableReader, methodResolutionService, typeof(IDictionary<string, object>))) :
                    (GetCheckMetadataNonVariable(methodStreamSpec.MethodName, methodStreamSpec.ClassName, methodResolutionService, typeof(IDictionary<string, object>)));
                oaTypeName = metadata.TypeName;
                oaType = (IDictionary<String, Object>)metadata.TypeMetadata;
            }

	        // Determine event type from class and method name
	        EventType eventType;
	        if (mapType != null) {
	            eventType = eventAdapterService.AddNestableMapType(mapTypeName, mapType, null, false, true, true, false, false);
	        }
	        else if (oaType != null) {
	            eventType = eventAdapterService.AddNestableObjectArrayType(oaTypeName, oaType, null, false, true, true, false, false, false, null);
	        }
	        else {
	            eventType = eventAdapterService.AddBeanType(beanClass.FullName, beanClass, false, true, true);
	        }

	        // Construct polling strategy as a method invocation
	        var configCache = engineImportService.GetConfigurationMethodRef(declaringClass.FullName);
	        if (configCache == null)
	        {
	            configCache = engineImportService.GetConfigurationMethodRef(declaringClass.FullName);
	        }

	        var dataCacheDesc = (configCache != null) ? configCache.DataCacheDesc : null;
	        var dataCache = dataCacheFactory.GetDataCache(dataCacheDesc, statementContext, epStatementAgentInstanceHandle, schedulingService, scheduleBucket, streamNumber);

	        PollExecStrategy methodPollStrategy;

	        if (mapType != null)
            {
	            if (methodFastClass.ReturnType.IsArray)
                {
	                methodPollStrategy = new MethodPollingExecStrategyMapArray(eventAdapterService, methodFastClass, eventType, invocationTarget, strategy, variableReader, variableName, variableService);
	            }
	            else if (isCollection)
                {
	                methodPollStrategy = new MethodPollingExecStrategyMapCollection(eventAdapterService, methodFastClass, eventType, invocationTarget, strategy, variableReader, variableName, variableService);
	            }
	            else if (isIterator)
                {
	                methodPollStrategy = new MethodPollingExecStrategyMapIterator(eventAdapterService, methodFastClass, eventType, invocationTarget, strategy, variableReader, variableName, variableService);
	            }
	            else
                {
	                methodPollStrategy = new MethodPollingExecStrategyMapPlain(eventAdapterService, methodFastClass, eventType, invocationTarget, strategy, variableReader, variableName, variableService);
	            }
	        }
	        else if (oaType != null)
            {
	            if (methodFastClass.ReturnType == typeof(object[][]))
                {
	                methodPollStrategy = new MethodPollingExecStrategyOAArray(eventAdapterService, methodFastClass, eventType, invocationTarget, strategy, variableReader, variableName, variableService);
	            }
	            else if (isCollection)
                {
	                methodPollStrategy = new MethodPollingExecStrategyOACollection(eventAdapterService, methodFastClass, eventType, invocationTarget, strategy, variableReader, variableName, variableService);
	            }
	            else if (isIterator)
                {
	                methodPollStrategy = new MethodPollingExecStrategyOAIterator(eventAdapterService, methodFastClass, eventType, invocationTarget, strategy, variableReader, variableName, variableService);
	            }
	            else
                {
	                methodPollStrategy = new MethodPollingExecStrategyOAPlain(eventAdapterService, methodFastClass, eventType, invocationTarget, strategy, variableReader, variableName, variableService);
	            }
	        }
	        else
            {
	            if (methodFastClass.ReturnType.IsArray) 
                {
	                methodPollStrategy = new MethodPollingExecStrategyPOCOArray(eventAdapterService, methodFastClass, eventType, invocationTarget, strategy, variableReader, variableName, variableService);
	            }
	            else if (isCollection)
                {
	                methodPollStrategy = new MethodPollingExecStrategyPOCOCollection(eventAdapterService, methodFastClass, eventType, invocationTarget, strategy, variableReader, variableName, variableService);
	            }
	            else if (isIterator)
                {
	                methodPollStrategy = new MethodPollingExecStrategyPOCOIterator(eventAdapterService, methodFastClass, eventType, invocationTarget, strategy, variableReader, variableName, variableService);
	            }
	            else
                {
	                methodPollStrategy = new MethodPollingExecStrategyPOCOPlain(eventAdapterService, methodFastClass, eventType, invocationTarget, strategy, variableReader, variableName, variableService);
	            }
	        }

	        return new MethodPollingViewable(variableMetaData == null, methodReflection.DeclaringType, methodStreamSpec, streamNumber, methodStreamSpec.Expressions, methodPollStrategy, dataCache, eventType, exprEvaluatorContext);
	    }

	    private static MethodMetadataDesc GetCheckMetadataVariable(string methodName, VariableMetaData variableMetaData, VariableReader variableReader, MethodResolutionService methodResolutionService, Type metadataClass)
	    {
	        var typeGetterMethod = GetRequiredTypeGetterMethodCanNonStatic(methodName, null, variableMetaData.VariableType, methodResolutionService, metadataClass);
	        if (typeGetterMethod.IsStatic) {
	            return InvokeMetadataMethod(null, variableMetaData.GetType().FullName, typeGetterMethod);
	        }

	        // if the metadata is not a static method and we don't have an instance this is a problem
	        const string messagePrefix = "Failed to access variable method invocation metadata: ";
	        if (variableReader == null) {
	            throw new ExprValidationException(messagePrefix + "The metadata method is an instance method however the variable is contextual, please declare the metadata method as static or remove the context declaration for the variable");
	        }

	        var value = variableReader.Value;
	        if (value == null) {
	            throw new ExprValidationException(messagePrefix + "The variable value is null and the metadata method is an instance method");
	        }

	        if (value is EventBean) {
	            value = ((EventBean) value).Underlying;
	        }
	        return InvokeMetadataMethod(value, variableMetaData.GetType().FullName, typeGetterMethod);
	    }

	    private static MethodMetadataDesc GetCheckMetadataNonVariable(string methodName, string className, MethodResolutionService methodResolutionService, Type metadataClass)
        {
	        var typeGetterMethod = GetRequiredTypeGetterMethodCanNonStatic(methodName, className, null, methodResolutionService, metadataClass);
	        return InvokeMetadataMethod(null, className, typeGetterMethod);
	    }

	    private static MethodInfo GetRequiredTypeGetterMethodCanNonStatic(string methodName, string classNameWhenNoClass, Type clazzWhenAvailable, MethodResolutionService methodResolutionService, Type metadataClass)
	    {
	        MethodInfo typeGetterMethod;
	        var getterMethodName = methodName + "Metadata";
	        try {
	            if (clazzWhenAvailable != null) {
	                typeGetterMethod = methodResolutionService.ResolveMethod(clazzWhenAvailable, getterMethodName, new Type[0], new bool[0], new bool[0]);
	            }
	            else {
	                typeGetterMethod = methodResolutionService.ResolveMethod(classNameWhenNoClass, getterMethodName, new Type[0], new bool[0], new bool[0]);
	            }
	        }
	        catch(Exception e) {
	            throw new ExprValidationException("Could not find getter method for method invocation, expected a method by name '" + getterMethodName + "' accepting no parameters");
	        }

	        bool fail;
	        if (metadataClass.IsInterface) {
	            fail = !typeGetterMethod.ReturnType.IsImplementsInterface(metadataClass);
	        }
	        else {
	            fail = typeGetterMethod.ReturnType != metadataClass;
	        }
	        if (fail) {
                throw new ExprValidationException("Getter method '" + typeGetterMethod.Name + "' does not return " + metadataClass.GetTypeNameFullyQualPretty());
	        }

	        return typeGetterMethod;
	    }

	    private static MethodMetadataDesc InvokeMetadataMethod(object target, string className, MethodInfo typeGetterMethod)
	    {
	        object resultType;
	        try {
	            resultType = typeGetterMethod.Invoke(target, new object[0]);
	        }
	        catch (Exception e) {
                throw new ExprValidationException("Error invoking metadata getter method for method invocation, for method by name '" + typeGetterMethod.Name + "' accepting no parameters: " + e.Message, e);
	        }
	        if (resultType == null) {
	            throw new ExprValidationException("Error invoking metadata getter method for method invocation, method returned a null value");
	        }

	        return new MethodMetadataDesc(className + "." + typeGetterMethod.Name, resultType);
	    }

	    public class MethodMetadataDesc
        {
            public string TypeName;
            public object TypeMetadata;
            
            public MethodMetadataDesc(string typeName, object typeMetadata)
            {
	            TypeName = typeName;
	            TypeMetadata = typeMetadata;
	        }
        }
	}
} // end of namespace
