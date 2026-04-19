using System;

namespace OmniWatch.Helpers
{
    public static class ExceptionExtensions
    {
        public static T? FindInner<T>(this Exception ex) where T : Exception
        {
            while (ex != null)
            {
                if (ex is T match)
                    return match;

                ex = ex.InnerException;
            }
            return null;
        }

        public static T? FindDeepestInner<T>(this Exception ex) where T : Exception
        {
            T? found = null;

            while (ex != null)
            {
                if (ex is T match)
                    found = match;

                ex = ex.InnerException;
            }

            return found;
        }
    }
}
