using NUnit.Framework;

namespace ReSharperPlugin.RimworldDev.Tests
{
    [TestFixture]
    public class FirstTest
    {
        [Test]
        public void Something()
        {
            Assert.That(1 + 1,  Is.EqualTo(2));
        }
    
        // protected override void DoTest(Lifetime lifetime, IProject testProject)
        // {
        //     ExecuteWithGold()
        // }
    }
}