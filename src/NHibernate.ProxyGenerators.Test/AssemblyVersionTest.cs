using System;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace NHibernate.ProxyGenerators.Test
{
    [TestFixture]
    public class AssemblyVersionTest
    {
       
        [Test]
        public void TestVersion()
        {
            Assert.AreEqual(Regex.Match("4.0.0-alpha1", @"\d+\.\d+\.\d+").ToString(), "4.0.0");
        }
    }
}