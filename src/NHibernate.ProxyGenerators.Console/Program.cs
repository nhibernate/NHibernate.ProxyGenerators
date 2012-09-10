namespace NHibernate.ProxyGenerators.Console
{
	using System;
	using System.IO;

	public class Program
	{
		public IProxyGenerator ProxyGenerator { get; set; }

		public int Execute(TextWriter error, params string[] args )
		{
			var generatorOptions = new ProxyGeneratorOptions();
			if (Parser.ParseHelp(args))
			{
				Parser.ParseArguments(args, generatorOptions);
			}
			else if (Parser.ParseArguments(args, generatorOptions) == false)
			{
				error.WriteLine(Parser.ArgumentsUsage(generatorOptions.GetType()));
				return Error.InvalidArguments;
			}

			if (ProxyGenerator == null)
			{
				try
				{
					ProxyGenerator = CreateProxyGenerator(generatorOptions.Generator);
				}
				catch (Exception exc)
				{
					error.WriteLine(exc.Message);
					return Error.CreateProxyGenerator;
				}
			}

			generatorOptions = ProxyGenerator.GetOptions();
			if( generatorOptions == null )
			{
				error.WriteLine("{0}.GetOptions() returned null.  Please use a different Generator.", ProxyGenerator.GetType().FullName);
				return Error.InvalidGenerator;
			}

			if (Parser.ParseHelp(args))
			{
				error.WriteLine(Parser.ArgumentsUsage(generatorOptions.GetType()));
				return Error.None;
			}
			if (Parser.ParseArguments(args, generatorOptions) == false)
			{
				error.WriteLine(Parser.ArgumentsUsage(generatorOptions.GetType()));
				return Error.InvalidArguments;
			}

			try
			{
				ProxyGenerator.Generate(generatorOptions);
			}
			catch (Exception exc)
			{
				error.WriteLine(exc.Message);
				error.WriteLine(exc.StackTrace);
				return Error.Unknown;				
			}

			return Error.None;
		}

		public static void Main(string[] args)
		{
			var program = new Program();
			int exitCode = program.Execute(Console.Error, args);
			Environment.Exit(exitCode);
		}

		public static IProxyGenerator CreateProxyGenerator(string generator)
		{
			var assemblyQualifiedName = ProxyGeneratorAssemblyQualifiedName(generator);

			try
			{
				Type proxyGeneratorType = Type.GetType(assemblyQualifiedName, false, true);
				if( proxyGeneratorType == null )
				{
					throw new ProxyGeneratorException("Invalid Generator Type '{0}'", assemblyQualifiedName);
				}

				var proxyGenerator = Activator.CreateInstance(proxyGeneratorType) as IProxyGenerator;
				if( proxyGenerator == null )
				{
					throw new ProxyGeneratorException("Generator Type does not implement IProxyGenerator '{0}'", proxyGeneratorType.AssemblyQualifiedName);
				}

				return proxyGenerator;
			}
			catch(Exception exc)
			{
				throw new ProxyGeneratorException("Error Creating ProxyGenerator of type '{0}'.\n\t{1}", assemblyQualifiedName, exc.Message);
			}
		}

		static string ProxyGeneratorAssemblyQualifiedName(string generator)
		{
			switch (generator.ToLowerInvariant())
			{
				case "default":
					return "NHibernate.ProxyGenerators.Default.DefaultProxyGenerator, NHibernate.ProxyGenerators";
				default:
					return generator;
			}
		}
	}

	public static class Error
	{
		public const int None = 0;
		public const int Unknown = 1;
		public const int InvalidArguments = 2;
		public const int InputAssemblyFailedLoad = 3;
		public const int CreateProxyGenerator = 4;
		public const int InvalidGenerator = 5;
	}
}
