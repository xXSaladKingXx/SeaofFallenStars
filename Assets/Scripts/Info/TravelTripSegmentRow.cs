using System.Globalization;
using TMPro;
using UnityEngine;

public class TravelTripSegmentRow : MonoBehaviour
{
    [SerializeField] private TMP_Text segmentLabel;
    [SerializeField] private TMP_InputField milesPerDayInput;

    public void SetLabel(string text)
    {
        if (segmentLabel != null) segmentLabel.text = text;
    }

    public float GetMilesPerDay(float fallback)
    {
        if (milesPerDayInput == null) return fallback;

        if (float.TryParse(milesPerDayInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v > 0f)
            return v;

        return fallback;
    }

    public void SetMilesPerDay(float value)
    {
        if (milesPerDayInput != null)
            milesPerDayInput.text = value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
