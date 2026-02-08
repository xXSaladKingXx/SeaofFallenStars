using Newtonsoft.Json;

namespace SeaOfFallenStars.WorldData
{
    /// <summary>
    /// Backwards‑compatibility alias for <see cref="VassalContract"/>.  Older
    /// scripts refer to a type named VassalContractData; by deriving from
    /// VassalContract we provide the same fields without duplicating them.
    /// </summary>
    [System.Serializable]
    public class VassalContractData : VassalContract
    {
    }
}