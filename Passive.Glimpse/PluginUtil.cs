// Copyright (c) 2011 Tall Ambitions, LLC
// See included LICENSE for details.
namespace Passive.Glimpse
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    public static class PluginUtil
    {
        internal static object[] AddHeader(this IEnumerable<object> data, params string[] headers)
        {
            return new object[] { headers }.Concat(data).ToArray();
        }

        internal static IDictionary<string, object> ToOrdinalDictionary(this IEnumerable<object> data)
        {
            if(data == null || !data.Any())
            {
                return null;
            }
            return data.Select((o, i) => new {Index = i.ToString(CultureInfo.CurrentCulture), Object = o}).ToDictionary(x => x.Index, x => x.Object);
        }
    }
}