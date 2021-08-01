using System;
using System.Collections.Generic;
using System.Linq;


using MessagePack;

using Xunit;

namespace SquirrelayServer.Tests
{
    internal static class Utils
    {
        public static void CheckEquality<T>(T obj1, T obj2)
        {
            var props = typeof(T).GetProperties().Where(p => Attribute.IsDefined(p, typeof(KeyAttribute)));
            foreach (var p in props)
            {
                var value1 = p.GetValue(obj1);
                var value2 = p.GetValue(obj2);

                if ((value1 is null && value2 is null))
                {
                    Assert.True(true);
                }
                else if (value1 is IEnumerable<object> a && value2 is IEnumerable<object> b)
                {
                    Assert.True(Enumerable.SequenceEqual(a, b));
                }
                else
                {
                    Assert.Equal(value1, value2);
                }
            }
        }
    }
}
