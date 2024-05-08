using System;
using NUnit.Framework;

namespace NUnitTest_PM
{
    public class Tests
    {
        Simulator sim;
        [SetUp]
        public void Setup()
        {
            sim = new Simulator();
            sim.Run(TimeSpan.FromMinutes(100));
        }

        [Test]
        public void Test1()
        {
            bool r1 = false, r2 = false, r3 = false, r4 = false, r5 = false;
            if (sim.Result4UnitTest.Contains("AGV-1 exit from: F")) r1 = true;
            if (sim.Result4UnitTest.Contains("AGV-2 exit from: F")) r2 = true;
            if (sim.Result4UnitTest.Contains("AGV-3 exit from: F")) r3 = true;
            if (sim.Result4UnitTest.Contains("AGV-4 exit from: F")) r4 = true;
            if (sim.Result4UnitTest.Contains("AGV-5 exit from: F")) r5 = true;
            if (r1 && r2 && r3 && r4 && r5)
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail();
            }
        }
    }
}