using SPTarkov.Server.Core.Models.Spt.Config;

namespace LateToTheParty.Helpers
{
    public static class CloningHelpers
    {
        public static Dictionary<TKey, TValue> Clone<TKey, TValue>(this Dictionary<TKey, TValue> original) where TKey : notnull
        {
            Dictionary<TKey, TValue> clone = new Dictionary<TKey, TValue>();
            foreach (KeyValuePair<TKey, TValue> kvp in original)
            {
                clone.Add(kvp.Key, kvp.Value);
            }
            return clone;
        }
    }
}
