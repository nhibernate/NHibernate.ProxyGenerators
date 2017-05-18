using System;
using System.Reflection;

namespace NHibernate.ProxyGenerators
{
	[Serializable]
	public class ProxyGeneratorOptions
	{
		[Argument(ArgumentType.AtMostOnce, HelpText = "The dialect to use. Only needed when you use dialect specific mapping options (like sequences in Oracle).", DefaultValue = "NHibernate.Dialect.MsSql2008Dialect", ShortName = "d")]
		public string Dialect = "NHibernate.Dialect.MsSql2008Dialect";

		[Argument(ArgumentType.AtMostOnce, HelpText = "The Proxy Generator to use default or Assembly Qualified Name", DefaultValue = "default", ShortName = "g")]
		public string Generator = "default";

		[DefaultArgument(ArgumentType.MultipleUnique, HelpText = "Path to assembly(ies) containing NHibernate Class Mappings")]
		public string[] InputAssemblyPaths;

		[Argument(ArgumentType.AtMostOnce, HelpText = "Path to the intermediate file used to generate the proxies", DefaultValue = "GeneratedProxies.dll", ShortName = "ip")]
		public string IntermediateProxyAssemblyPath = "GeneratedProxies.dll";

		[Argument(ArgumentType.AtMostOnce, HelpText = "Path to the intermediate file used to generate the Proxy Factory", DefaultValue = "StaticProxyFactory.dll", ShortName = "if")]
		public string IntermediateStaticProxyFactoryAssemblyPath = "StaticProxyFactory.dll";

		[Argument(ArgumentType.Required, HelpText = "Path to output assembly for generated proxies.  e.g. .\\OutputAssembly.dll", ShortName = "o")]
		public string OutputAssemblyPath;

		[Argument(ArgumentType.MultipleUnique, HelpText = "Full Type name for fluent conventions.  e.g. My.Fluent.Conventions, MyAssembly", ShortName = "c")]
		public string[] FluentConventions;

		public ProxyGeneratorOptions()
		{
		}

		public ProxyGeneratorOptions(string outputAssemblyPath, params string[] inputAssemblyPaths)
		{
			OutputAssemblyPath = outputAssemblyPath;
			InputAssemblyPaths = inputAssemblyPaths;
		}

		public Assembly[] InputAssemblies { get; set; }
	}
}
