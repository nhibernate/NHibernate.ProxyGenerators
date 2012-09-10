using System;
using System.Reflection;
using System.Reflection.Emit;
using NHibernate.Proxy.DynamicProxy;

namespace NHibernate.ProxyGenerators.Default
{
	public class SavingProxyAssemblyBuilder : IProxyAssemblyBuilder
	{
		readonly string assemblyFileName;
		readonly string assemblyName;
		AssemblyBuilder assembly;
		ModuleBuilder module;

		public SavingProxyAssemblyBuilder(string assemblyName, string assemblyFileName)
		{
			this.assemblyName = assemblyName;
			this.assemblyFileName = assemblyFileName;
		}

		public AssemblyBuilder DefineDynamicAssembly(AppDomain appDomain, AssemblyName name)
		{
			return assembly ?? (assembly = appDomain.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.RunAndSave));
		}

		public ModuleBuilder DefineDynamicModule(AssemblyBuilder assemblyBuilder, string moduleName)
		{
			return module ?? (module = assemblyBuilder.DefineDynamicModule(assemblyName, assemblyFileName, true));
		}

		public void Save(AssemblyBuilder assemblyBuilder)
		{
		}

		public void Save()
		{
			assembly.Save(assemblyFileName);
		}
	}
}
