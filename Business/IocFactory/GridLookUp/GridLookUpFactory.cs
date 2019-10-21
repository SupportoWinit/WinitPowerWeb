using System;
using System.Collections.Generic;

namespace Business.IocFactory.GridLookUp
{
    public static class GridLookUpFactory
    {
        private static IDictionary<string, Type> container;

        static GridLookUpFactory()
        {
            container = new Dictionary<string, Type>();
        }
    }
}
