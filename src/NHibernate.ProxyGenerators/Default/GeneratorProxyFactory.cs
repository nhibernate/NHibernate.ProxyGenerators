using System;
using System.Collections.Generic;
using System.Reflection;
using Iesi.Collections.Generic;
using NHibernate.Engine;
using NHibernate.Proxy;
using NHibernate.Proxy.DynamicProxy;
using NHibernate.Type;

namespace NHibernate.ProxyGenerators.Default
{
	public class GeneratorProxyFactory : IProxyFactory
	{
		readonly ProxyFactory factory;
		readonly IDictionary<string, System.Type> proxies;

		public GeneratorProxyFactory(ProxyFactory factory, IDictionary<string, System.Type> proxies)
		{
			if (factory == null)
			{
				throw new ArgumentNullException("factory");
			}
			if (proxies == null)
			{
				throw new ArgumentNullException("proxies");
			}

			this.factory = factory;
			this.proxies = proxies;
		}

		public void PostInstantiate(string entityName, System.Type persistentClass, ISet<System.Type> interfaces, MethodInfo getIdentifierMethod, MethodInfo setIdentifierMethod, IAbstractComponentType componentIdType)
		{
			if (persistentClass.IsGenericType) return;

			var interfacesCount = interfaces.Count;
			var ifaces = new System.Type[interfacesCount];
			if (interfacesCount > 0)
				interfaces.CopyTo(ifaces, 0);

			var proxyType = ifaces.Length == 1
								? factory.CreateProxyType(persistentClass, ifaces)
								: factory.CreateProxyType(ifaces[0], ifaces);

			proxies[entityName] = proxyType;
		}

		public INHibernateProxy GetProxy(object id, ISessionImplementor session)
		{
			throw new NotImplementedException();
		}

		public object GetFieldInterceptionProxy(object instanceToWrap)
		{
			throw new NotImplementedException();
		}
	}
}
