namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic.Extensions;

internal static class CollectionExtensions
{
    extension(Dictionary<string, string> baseDict)
    {
        /// <summary>
        /// Merges the base dictionary with another dictionary, where values in the other dictionary can override or remove entries from the base dictionary. If a value in the other dictionary is null, the corresponding key will be removed from the merged result.
        /// </summary>
        /// <param name="otherDict"></param>
        /// <returns></returns>
        public Dictionary<string, string> Merge(Dictionary<string, string?> otherDict)
        {
            var mergedDict = new Dictionary<string, string>(baseDict);
            foreach (var kvp in otherDict)
            {
                if (kvp.Value != null)
                {
                    mergedDict[kvp.Key] = kvp.Value;
                }
                else
                {
                    mergedDict.Remove(kvp.Key);
                }
            }
            return mergedDict;
        }
    }
    
}