using NHibernate.Mapping;
using NHibernate.Proxy.DynamicProxy;

namespace NHibernate.ProxyGenerators.Castle
{
	using System;
	using System.Collections;
	using System.Reflection;
	using Engine;
	using Iesi.Collections.Generic;
	using Proxy;
	using Type;

	public class CastleProxyFactory :  IProxyFactory
	{
        readonly ProxyFactory factory;
		private readonly IDictionary proxies;

        public CastleProxyFactory(ProxyFactory factory, IDictionary proxies)
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

	    public  void PostInstantiate(string entityName, Type persistentClass, ISet<Type> interfaces, MethodInfo getIdentifierMethod, MethodInfo setIdentifierMethod, IAbstractComponentType componentIdType)
		{
	        if (persistentClass.IsGenericType) return;

            var interfacesCount = interfaces.Count;
	        var ifaces = new Type[interfacesCount];
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