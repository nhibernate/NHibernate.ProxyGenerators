using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using Iesi.Collections.Generic;
using NHibernate;
using NHibernate.Bytecode;
using NHibernate.Engine;
using NHibernate.Proxy;
using NHibernate.Proxy.DynamicProxy;
using NHibernate.Type;
using NHibernate.Util;
using IInterceptor = NHibernate.Proxy.DynamicProxy.IInterceptor;
//[assembly: AssemblyVersion("{VERSION}")]
//[assembly: System.Security.AllowPartiallyTrustedCallers]
//[assembly: SecurityRules(SecurityRuleSet.Level1)]

public class StaticProxyFactory : IProxyFactory
{
	private static readonly INHibernateLogger _log;
	private static readonly Dictionary<string, System.Type> _proxies;

	private string _entityName;
	private System.Type _persistentClass;
	private System.Type[] _interfaces;
	private MethodInfo _getIdentifierMethod;
	private MethodInfo _setIdentifierMethod;
	private IAbstractComponentType _componentIdType;
	private bool _isClassProxy;
	private string _proxyKey;
	private System.Type _proxyType;
	bool _overridesEquals;

	static StaticProxyFactory()
	{
		_log = NHibernateLogger.For(typeof(StaticProxyFactory));
		_proxies = new Dictionary<string, System.Type>();
		//{PROXIES}
	}

	public void PostInstantiate(string entityName, System.Type persistentClass, ISet<System.Type> interfaces, MethodInfo getIdentifierMethod, MethodInfo setIdentifierMethod, IAbstractComponentType componentIdType)
	{
		_entityName = entityName;
		_persistentClass = persistentClass;
		_interfaces = new System.Type[interfaces.Count];
		interfaces.CopyTo(_interfaces, 0);
		_getIdentifierMethod = getIdentifierMethod;
		_setIdentifierMethod = setIdentifierMethod;
		_componentIdType = componentIdType;
		_isClassProxy = _interfaces.Length == 1;
		_overridesEquals = ReflectHelper.OverridesEquals(persistentClass);
		_proxyKey = entityName;

		if (!_proxies.TryGetValue(_proxyKey, out _proxyType))
		{
			var message = string.Format("No proxy type found for persistent class '{0}' using proxy key '{1}'", _persistentClass.FullName, _proxyKey);
			_log.Error(message);
			throw new HibernateException(message);
		}
		
		_log.Debug("Using proxy type '{0}' for persistent class '{1}'", _proxyType.Name, _persistentClass.FullName);
	}

	public INHibernateProxy GetProxy(object id, ISessionImplementor session)
	{
		INHibernateProxy proxy;
		try
		{
			var initializer = new  DefaultLazyInitializer(_entityName, _persistentClass, id, _getIdentifierMethod, _setIdentifierMethod, _componentIdType, session, _overridesEquals);
			var generatedProxy = (IProxy) Activator.CreateInstance(_proxyType);
			generatedProxy.Interceptor = initializer;
			proxy = (INHibernateProxy) generatedProxy;
		}
		catch (Exception e)
		{
			const string message = "Creating a proxy instance failed";
			_log.Error(e, message);
			throw new HibernateException(message, e);
		}

		return proxy;
	}

	public object GetFieldInterceptionProxy(object instanceToWrap)
	{
		throw new NotSupportedException();
	}
}
	
public class StaticProxyFactoryFactory : IProxyFactoryFactory
{
	public IProxyFactory BuildProxyFactory()
	{
		return new StaticProxyFactory();
	}

	public bool IsInstrumented(System.Type entityClass)
	{
		return true;
	}

	public bool IsProxy(object entity)
	{
		return entity is INHibernateProxy;
	}

	public IProxyValidator ProxyValidator
	{
		get { return new DynProxyTypeValidator(); }
	}
}
