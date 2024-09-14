using UnityEngine;
using System.Linq;

namespace LessSteam
{
    internal static class Util
    {
        public static void DebugPrint(params object[] args)
        {
            string s = string.Format("[LessSteam] {0}", " ".OnJoin(args));
            Debug.Log(s);
        }

        public static string OnJoin(this string delim, params object[] args)
        {
            return string.Join(delim, args.Select(o => o?.ToString() ?? "null").ToArray());
        }
    }
}
