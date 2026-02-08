using Newtonsoft.Json;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Represents a single garrison entry for a settlement. Each entry records
    /// the men-at-arms unit identifier and the number of units present. This
    /// mirrors the menAtArmsStacks structure expected by the editor UI.
    /// </summary>
    [System.Serializable]
    public class MenAtArmsQuantityEntry
    {
        [JsonProperty("menAtArmsId")]
        public string menAtArmsId;

        [JsonProperty("units")]
        public int units;
    }
}