using NSubstitute;

namespace NHibernate.ProxyGenerators.Console.Test
{
	using System.IO;
	using NUnit.Framework;

	[TestFixture]
	public class ProgramTester
	{
		private StringWriter _error;
		private Program _program;
		private IProxyGenerator _generator;

		[SetUp]
		public void SetUp()
		{
			_error = new StringWriter();

			_generator = Substitute.For<IProxyGenerator>();
			_generator.GetOptions().Returns(new ProxyGeneratorOptions());

			_program = new Program();
			_program.ProxyGenerator = _generator;
		}

		[Test]
		public void Invalid_Arguments_Display_Usage()
		{
			int exitCode = _program.Execute(_error);

			Assert.AreEqual(Error.InvalidArguments, exitCode);
			Assert.AreNotEqual(-1, GetErrors().IndexOf("/Generator:<string>"));
		}

		[Test]
		public void Invalid_Generator_Returns_Error()
		{
			_program.ProxyGenerator = null;

			int exitCode = _program.Execute(_error, "/g:invalid", "/o:Output.dll", "Input.dll");

			Assert.AreEqual(Error.CreateProxyGenerator, exitCode);
		}

		[Test]
		public void Valid_Options_Generates_Proxies()
		{
			int exitCode = _program.Execute(_error, "/o:Output.dll", typeof(string).Assembly.Location);

			Assert.AreEqual(Error.None, exitCode);

			_generator.ReceivedWithAnyArgs().Generate(null);
		}

		private string GetErrors()
		{
			return _error.ToString();
		}
	}
}