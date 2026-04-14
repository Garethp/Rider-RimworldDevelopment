// ${COMPLETE_ITEM:a}
using System;

namespace ReSharperPlugin.RimworldDev.Tests.test.data.F
{
    public class Test1
    {
        public void DoNamedTest()
        {
            var abc = 1 + 1;
            Console.WriteLine({caret});
        }
    }
}