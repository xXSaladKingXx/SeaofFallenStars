using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class TextBackgroundAutoFit : MonoBehaviour
{
    public enum FitMode
    {
        /// <summary>
        /// Sizes the background based on the text component's preferred values.
        /// Best when you want the background to "hug" the text contents.
        /// </summary>
        PreferredValues,

        /// <summary>
        /// Sizes the background to match the text RectTransform's current size.
        /// Best when the text RectTransform is driven by layout components (ContentSizeFitter/LayoutGroup).
        /// </summary>
        TextRectSize
    }

    [Header("References")]
    [SerializeField] private RectTransform backgroundRect;   // The Image (background) RectTransform
    [SerializeField] private RectTransform textRect;         // The text RectTransform (TMP or UI.Text)

    [Header("Text Component (one of these)")]
    [SerializeField] private TMP_Text tmpText;
    [SerializeField] private Text uiText;

    [Header("Sizing")]
    [SerializeField] private FitMode fitMode = FitMode.PreferredValues;
    [SerializeField] private Vector2 padding = new Vector2(16f, 8f); // total padding added (x=left+right, y=top+bottom)
    [SerializeField] private Vector2 minSize = Vector2.zero;

    [Tooltip("If > 0, forces a width constraint when calculating preferred size (useful for word-wrap).")]
    [SerializeField] private float fixedWidthConstraint = -1f;

    [Header("Alignment / Ordering")]
    [SerializeField] private bool matchAnchorsAndPivot = true;
    [SerializeField] private bool matchAnchoredPosition = true;
    [SerializeField] private bool sendBackgroundToBack = true;

    private void Reset()
    {
        // Try auto-wire common setups
        if (backgroundRect == null)
        {
            var img = GetComponentInChildren<Image>(true);
            if (img != null) backgroundRect = img.rectTransform;
        }

        if (tmpText == null) tmpText = GetComponentInChildren<TMP_Text>(true);
        if (uiText == null) uiText = GetComponentInChildren<Text>(true);

        if (textRect == null)
        {
            if (tmpText != null) textRect = tmpText.rectTransform;
            else if (uiText != null) textRect = uiText.rectTransform;
        }
    }

    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTmpTextChanged);
        Refresh();
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTmpTextChanged);
    }

    private void LateUpdate()
    {
#if UNITY_EDITOR
        // In Edit Mode, keep it responsive when you tweak values in the Inspector.
        if (!Application.isPlaying)
            Refresh();
#endif
    }

    private void OnRectTransformDimensionsChange()
    {
        // Handles cases where width changes (wrapping), parent resizes, etc.
        Refresh();
    }

    private void OnValidate()
    {
        Refresh();
    }

    private void OnTmpTextChanged(Object changed)
    {
        if (changed == null) return;
        if (tmpText != null && ReferenceEquals(changed, tmpText))
            Refresh();
    }

    public void Refresh()
    {
        if (backgroundRect == null || textRect == null)
            return;

        // Keep background aligned with the text
        if (matchAnchorsAndPivot)
        {
            backgroundRect.anchorMin = textRect.anchorMin;
            backgroundRect.anchorMax = textRect.anchorMax;
            backgroundRect.pivot = textRect.pivot;
        }

        if (matchAnchoredPosition)
            backgroundRect.anchoredPosition = textRect.anchoredPosition;

        if (sendBackgroundToBack)
            backgroundRect.SetAsFirstSibling();

        Vector2 targetSize;

        if (fitMode == FitMode.TextRectSize)
        {
            // Use the current rect size (assumes textRect is already being sized by layout components)
            targetSize = textRect.rect.size;
        }
        else
        {
            // Compute preferred size from text content
            float widthConstraint = GetWidthConstraint();
            targetSize = GetPreferredTextSize(widthConstraint);
        }

        // Apply padding (padding is total; split per side)
        targetSize.x += padding.x;
        targetSize.y += padding.y;

        // Apply minimum size
        targetSize.x = Mathf.Max(targetSize.x, minSize.x);
        targetSize.y = Mathf.Max(targetSize.y, minSize.y);

        // Set size respecting anchors
        backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetSize.x);
        backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetSize.y);
    }

    private float GetWidthConstraint()
    {
        if (fixedWidthConstraint > 0f)
            return fixedWidthConstraint;

        // If wrapping, many setups want to treat current rect width as the constraint.
        // If textRect stretches, rect.width will reflect the available width.
        return textRect.rect.width > 0f ? textRect.rect.width : 0f;
    }

    private Vector2 GetPreferredTextSize(float widthConstraint)
    {
        if (tmpText != null)
        {
            // Ensure TMP has up-to-date layout info
            tmpText.ForceMeshUpdate();

            // GetPreferredValues handles wrapping when you pass a width constraint.
            // If widthConstraint is 0, TMP treats it as unconstrained.
            return tmpText.GetPreferredValues(tmpText.text, widthConstraint, Mathf.Infinity);
        }

        if (uiText != null)
        {
            // Legacy UI.Text preferred sizing
            var settings = uiText.GetGenerationSettings(new Vector2(
                widthConstraint > 0f ? widthConstraint : Mathf.Infinity,
                Mathf.Infinity
            ));

            float prefW = uiText.cachedTextGeneratorForLayout.GetPreferredWidth(uiText.text, settings) / uiText.pixelsPerUnit;
            float prefH = uiText.cachedTextGeneratorForLayout.GetPreferredHeight(uiText.text, settings) / uiText.pixelsPerUnit;

            // If unconstrained width, prefW can be huge depending on settings; clamp by current width if needed.
            if (widthConstraint > 0f) prefW = Mathf.Min(prefW, widthConstraint);

            return new Vector2(prefW, prefH);
        }

        // Fallback: use rect size
        return textRect.rect.size;
    }
}
