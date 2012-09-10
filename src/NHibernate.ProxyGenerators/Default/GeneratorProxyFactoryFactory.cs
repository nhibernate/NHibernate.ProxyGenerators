using NHibernate.Bytecode;
using NHibernate.Proxy;

namespace NHibernate.ProxyGenerators.Default
{
	public class GeneratorProxyFactoryFactory : IProxyFactoryFactory
	{
		public static IProxyFactory ProxyFactory { get; set; }

		public IProxyFactory BuildProxyFactory()
		{
			return ProxyFactory;
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
}
