using System;
using NHibernate.ProxyGenerators.Default;
using NUnit.Framework;

namespace NHibernate.ProxyGenerators.Test.Default
{
	[TestFixture]
	[Serializable]
	public class DefaultProxyGeneratorTester : ProxyGeneratorTester
	{
		protected override IProxyGenerator CreateGenerator()
		{
			return new DefaultProxyGenerator();
		}

		protected override ProxyGeneratorOptions CreateOptions(string outputAssemblyPath, params string[] inputAssembilyPaths)
		{
			return new ProxyGeneratorOptions(outputAssemblyPath, inputAssembilyPaths);
		}

		[Test]
		public void OutputAssemblyPath_Is_Required()
		{
			Assert.Throws<ProxyGeneratorException>(() =>
			{
				_generator.Generate(null);
			});
		}

		[Test]
		public void OutputAssemblyPath_Must_Be_Rooted()
		{
			Assert.Throws<ProxyGeneratorException>(() =>
			{
				_generator.Generate(CreateOptions("OutputAssembly.dll"));
			});
		}

		[Test]
		public void InputAssemblies_Cannot_Be_Null()
		{
			Assert.Throws<ProxyGeneratorException>(() =>
			{
				_generator.Generate(CreateOptions("OutputAssembly.dll", null));
			});
		}

		[Test]
		public void At_Least_One_InputAssembly_Is_Required()
		{
			Assert.Throws<ProxyGeneratorException>(() =>
			{
				_generator.Generate(CreateOptions("OutputAssembly.dll", new string[0]));
			});
		}

		[Test]
		public void At_Least_One_ClassMapping_Is_Required()
		{
			string inputAssemblyLocation = typeof(string).Assembly.Location;

			var exc = Assert.Throws<ProxyGeneratorException>(() =>
			{
				_generator.Generate(CreateOptions("OutputAssembly.dll", inputAssemblyLocation));
			});

			Assert.IsNotNull(exc);
			Assert.Less(0, exc.Message.IndexOf(inputAssemblyLocation));
		}
	}
}