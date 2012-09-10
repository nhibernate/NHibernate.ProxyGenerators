using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ILMerging;
using Microsoft.CSharp;
using NHibernate.Cache;
using NHibernate.Cfg;
using NHibernate.Proxy.DynamicProxy;

namespace NHibernate.ProxyGenerators.Default
{
	[Serializable]
	public class DefaultProxyGenerator : IProxyGenerator
	{
		public Assembly Generate(ProxyGeneratorOptions options)
		{
			var castleOptions = ValidateOptions(options);
			try
			{
				CrossAppDomainCaller.RunInOtherAppDomain(Generate, this, castleOptions);
			}
			finally
			{
				CleanUpIntermediateFiles(castleOptions);
			}

			return Assembly.LoadFrom(options.OutputAssemblyPath);
		}

		public virtual ProxyGeneratorOptions GetOptions()
		{
			return new ProxyGeneratorOptions();
		}

		protected virtual void CleanUpIntermediateFiles(ProxyGeneratorOptions castleOptions)
		{
			if (File.Exists(castleOptions.IntermediateProxyAssemblyPath))
			{
				File.Delete(castleOptions.IntermediateProxyAssemblyPath);
			}

			var intermediateProxyAssemblyPdbPath = Path.ChangeExtension(castleOptions.IntermediateProxyAssemblyPath, "pdb");
			if (File.Exists(intermediateProxyAssemblyPdbPath))
			{
				File.Delete(intermediateProxyAssemblyPdbPath);
			}

			if (File.Exists(castleOptions.IntermediateStaticProxyFactoryAssemblyPath))
			{
				File.Delete(castleOptions.IntermediateStaticProxyFactoryAssemblyPath);
			}

			var intermediateCastleStaticProxyFactoryAssemblyPdbPath = Path.ChangeExtension(castleOptions.IntermediateStaticProxyFactoryAssemblyPath, "pdb");
			if (File.Exists(intermediateCastleStaticProxyFactoryAssemblyPdbPath))
			{
				File.Delete(intermediateCastleStaticProxyFactoryAssemblyPdbPath);
			}
		}

		protected virtual CompilerResults CompileStaticProxyFactory(Configuration nhibernateConfiguration, Assembly proxyAssembly, string sourceCode, string outputAssembly)
		{
			var parameters = new CompilerParameters
				{
					OutputAssembly = outputAssembly,
					WarningLevel = 4,
					TreatWarningsAsErrors = true,
					CompilerOptions = "/debug:pdbonly /optimize+"
				};

			var references = new List<Assembly>
				{
					Assembly.Load("NHibernate"),
					Assembly.Load("Iesi.Collections"),
					proxyAssembly
				};

			var proxyReferencedAssemblyNames = proxyAssembly.GetReferencedAssemblies();
			foreach (var proxyReferencedAssemblyName in proxyReferencedAssemblyNames)
			{
				var proxyReferencedAssembly = Assembly.Load(proxyReferencedAssemblyName);
				if (!references.Contains(proxyReferencedAssembly))
				{
					references.Add(proxyReferencedAssembly);
				}
			}

			foreach (var cls in nhibernateConfiguration.ClassMappings)
			{
				if (!references.Contains(cls.MappedClass.Assembly))
				{
					references.Add(cls.MappedClass.Assembly);
				}
			}

			foreach (var assembly in references)
			{
				parameters.ReferencedAssemblies.Add(assembly.Location);
			}

			var compiler = new CSharpCodeProvider(new Dictionary<String, String> { { "CompilerVersion", "v3.5" } });
			return compiler.CompileAssemblyFromSource(parameters, sourceCode);
		}

		protected virtual Configuration CreateNHibernateConfiguration(IEnumerable<Assembly> inputAssemblies, ProxyGeneratorOptions options)
		{
			var nhibernateConfiguration = new Configuration();

			nhibernateConfiguration.SetProperties(GetDefaultNHibernateProperties(options));

			foreach (var inputAssembly in inputAssemblies)
			{
				nhibernateConfiguration.AddAssembly(inputAssembly);
			}

			return nhibernateConfiguration;
		}

		protected virtual void FailNoClassMappings(Assembly[] inputAssemblies)
		{
			var builder = new StringBuilder("No NHibernate Class Mappings found in inputAssemblies { ");
			for (var i = 0; i < inputAssemblies.Length; i++)
			{
				builder.Append("\"");
				builder.Append(inputAssemblies[i].Location);
				builder.Append("\"");
				if (i != inputAssemblies.Length - 1) builder.Append(" , ");
			}
			builder.Append(" }");
			throw new ProxyGeneratorException(builder.ToString());
		}

		protected virtual void GenerateImpl(ProxyGeneratorOptions options)
		{
			var outputAssemblyPath = options.OutputAssemblyPath;

			var inputAssemblies = options.InputAssemblies;

			var nhibernateConfiguration = CreateNHibernateConfiguration(inputAssemblies, options);
			if (nhibernateConfiguration.ClassMappings.Count == 0) FailNoClassMappings(inputAssemblies);

			var proxyResult = GenerateProxies(nhibernateConfiguration, options.IntermediateProxyAssemblyPath);

			var staticProxyFactorySourceCode = GenerateStaticProxyFactorySourceCode(inputAssemblies[0].GetName().Version, BuildProxyTable(proxyResult.Proxies));

			var result = CompileStaticProxyFactory(nhibernateConfiguration, proxyResult.Assembly, staticProxyFactorySourceCode, options.IntermediateStaticProxyFactoryAssemblyPath);

			if (result.Errors.HasErrors)
			{
				var errors = new StringBuilder();
				foreach (var error in result.Errors)
				{
					errors.AppendLine(error.ToString());
				}
				throw new ProxyGeneratorException(errors.ToString());
			}

			MergeStaticProxyFactoryWithProxies(result.CompiledAssembly, proxyResult.Assembly, inputAssemblies, outputAssemblyPath);
		}

		protected virtual GenerateProxiesResult GenerateProxies(Configuration nhibernateConfiguration, string modulePath)
		{
			const string assemblyName = "GeneratedAssembly";
			var proxies = new Dictionary<string, System.Type>();

			var assemblyBuilder = new SavingProxyAssemblyBuilder(assemblyName);
			try
			{
				GeneratorProxyFactoryFactory.ProxyFactory = new GeneratorProxyFactory(new ProxyFactory(assemblyBuilder), proxies);
				using (nhibernateConfiguration.BuildSessionFactory())
				{
				}
			}
			finally
			{
				GeneratorProxyFactoryFactory.ProxyFactory = null;
			}
			assemblyBuilder.Save();

			var proxyAssemblyName = new AssemblyName(assemblyName) { CodeBase = modulePath };

			var proxyAssembly = Assembly.Load(proxyAssemblyName);

			return new GenerateProxiesResult(proxies, proxyAssembly);
		}

		protected virtual string GenerateStaticProxyFactorySourceCode(Version sourceVersion, string proxyCode)
		{
			return Source()
				.Replace("//", string.Empty)
				.Replace("{VERSION}", sourceVersion.ToString())
				.Replace("{PROXIES}", proxyCode);
		}

		protected virtual IDictionary<string, string> GetDefaultNHibernateProperties(ProxyGeneratorOptions options)
		{
			var properties = new Dictionary<string, string>();
			properties["cache.provider_class"] = typeof (HashtableCacheProvider).AssemblyQualifiedName;
			properties["dialect"] = options.Dialect;
			properties["proxyfactory.factory_class"] = typeof (GeneratorProxyFactoryFactory).AssemblyQualifiedName;
			properties["hbm2ddl.keywords"] = "none";
			return properties;
		}

		protected virtual void MergeStaticProxyFactoryWithProxies(Assembly staticProxyAssembly, Assembly proxyAssembly, Assembly[] referenceAssemblies, string outputPath)
		{
			var merger = new ILMerge();

			var searchDirectories = new List<string>(referenceAssemblies.Length);
			foreach (var referenceAssembly in referenceAssemblies)
			{
				var searchDirectory = Path.GetDirectoryName(referenceAssembly.Location);
				if (!searchDirectories.Contains(searchDirectory))
				{
					searchDirectories.Add(searchDirectory);
				}
			}

			merger.SetSearchDirectories(searchDirectories.ToArray());
			merger.SetInputAssemblies(new[] { staticProxyAssembly.Location, proxyAssembly.Location });
			merger.OutputFile = outputPath;
			merger.Merge();
		}

		static string BuildProxyTable(IEnumerable<KeyValuePair<string, System.Type>> proxies)
		{
			var proxyCode = new StringBuilder();
			foreach (var entry in proxies)
			{
				proxyCode.AppendFormat("\t\t_proxies[\"{0}\"] = typeof({1});\r\n", entry.Key, entry.Value.Name);
			}
			return proxyCode.ToString();
		}

		static void Generate(object[] args)
		{
			var proxyGenerator = (DefaultProxyGenerator) args[0];
			var generatorOptions = (ProxyGeneratorOptions) args[1];

			using (var resolver = new AssemblyResolver(generatorOptions.InputAssemblyPaths))
			{
				generatorOptions.InputAssemblies = resolver.LoadFrom(generatorOptions.InputAssemblyPaths);
				proxyGenerator.GenerateImpl(generatorOptions);
			}
		}

		static string Source()
		{
			using (var stream = typeof (DefaultProxyGenerator).Assembly.GetManifestResourceStream("NHibernate.ProxyGenerators.Default.StaticProxyFactory.cs"))
			using (TextReader reader = new StreamReader(stream))
			{
				return reader.ReadToEnd();
			}
		}

		static ProxyGeneratorOptions ValidateOptions(ProxyGeneratorOptions options)
		{
			var castleOptions = options;

			if (castleOptions == null) throw new ProxyGeneratorException("options must be of type {0}", typeof (ProxyGeneratorOptions).Name);

			if (string.IsNullOrEmpty(castleOptions.OutputAssemblyPath)) throw new ProxyGeneratorException("options.OutputAssemblyPath is Required");

			if (!Path.IsPathRooted(castleOptions.OutputAssemblyPath))
			{
				castleOptions.OutputAssemblyPath = Path.GetFullPath(castleOptions.OutputAssemblyPath);
			}

			if (castleOptions.InputAssemblyPaths == null || castleOptions.InputAssemblyPaths.Length == 0)
			{
				throw new ProxyGeneratorException("At least one input assembly is required");
			}

			return castleOptions;
		}

		protected class GenerateProxiesResult
		{
			public readonly Assembly Assembly;
			public readonly IDictionary<string, System.Type> Proxies;

			public GenerateProxiesResult(IDictionary<string, System.Type> proxies, Assembly assembly)
			{
				Proxies = proxies;
				Assembly = assembly;
			}
		}
	}
}
