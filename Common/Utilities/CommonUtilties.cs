using System;

namespace PlayniteSounds.Common.Utilities
{
    internal static class CommonUtilties
    {
        public static void Try(Action action) { try { action(); } catch { } }
        public static void Try(Action action, Action<Exception> handler)
        { try { action(); } catch(Exception e) { handler(e);  } }
        public static void Try(Action action, Action final) { try { action(); } catch { } { final(); } }
        public static void Try(Action action, Action<Exception> handler, Action final)
        { try { action(); } catch (Exception e) { handler(e); } { final(); } }
    }
}
