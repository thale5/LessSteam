using UnityEngine;
using System.Linq;
using System.Reflection;
using System;

namespace LessSteam
{
    internal static class Util
    {
        internal static readonly System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
        internal static int Millis => (int) stopWatch.ElapsedMilliseconds;

        public static void DebugPrint(params object[] args)
        {
            string s = string.Format("[LessSteam] {0}: {1}", Millis.ToString(), " ".OnJoin(args));
            Debug.Log(s);
        }

        public static string OnJoin(this string delim, params object[] args)
        {
            return string.Join(delim, args.Select(o => o?.ToString() ?? "null").ToArray());
        }

        internal static object GetStatic(Type type, string field)
        {
            return type.GetField(field, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        }

        internal static void Set(object instance, string field, object value)
        {
            instance.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(instance, value);
        }

        internal static void SetProperty(object instance, string property, object value)
        {
            instance.GetType()
                .GetProperty(property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(instance, value, null);
        }
    }
}
