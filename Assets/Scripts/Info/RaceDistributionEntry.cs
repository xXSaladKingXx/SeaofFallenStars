using Newtonsoft.Json;

namespace Zana.WorldAuthoring
{
    [System.Serializable]
    public class RaceDistributionEntry
    {
        [JsonProperty("raceId")]
        public string raceId;

        [JsonProperty("percentage")]
        public float percentage;
    }
}