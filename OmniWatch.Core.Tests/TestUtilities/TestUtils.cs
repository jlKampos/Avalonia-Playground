using System.Reflection;

namespace OmniWatch.Core.Tests.TestUtilities
{
    public static class TestUtils
    {
        public static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType()
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            field!.SetValue(obj, value);
        }
    }
}
