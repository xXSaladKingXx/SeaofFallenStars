using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TMP_CurvedTextUGUI : MonoBehaviour
{
    [Tooltip("How much vertical arc to apply (in local units). 0 disables curvature.")]
    public float curveAmount = 12f;

    private TMP_Text _text;
    private bool _dirty = true;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    public void ForceRefresh()
    {
        _dirty = true;
        if (_text != null) _text.SetVerticesDirty();
    }

    private void LateUpdate()
    {
        if (_text == null || curveAmount <= 0.001f) return;
        if (!_dirty && !_text.havePropertiesChanged) return;

        _text.ForceMeshUpdate();
        var textInfo = _text.textInfo;

        if (textInfo == null || textInfo.characterCount == 0)
        {
            _dirty = false;
            return;
        }

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var ch = textInfo.characterInfo[i];
            if (!ch.isVisible) continue;

            minX = Mathf.Min(minX, ch.bottomLeft.x);
            maxX = Mathf.Max(maxX, ch.topRight.x);
        }

        float width = Mathf.Max(1f, maxX - minX);

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var ch = textInfo.characterInfo[i];
            if (!ch.isVisible) continue;

            int matIndex = ch.materialReferenceIndex;
            int vertIndex = ch.vertexIndex;

            var verts = textInfo.meshInfo[matIndex].vertices;

            float cx = (verts[vertIndex + 0].x + verts[vertIndex + 2].x) * 0.5f;
            float t = (cx - minX) / width;     // 0..1
            float xNorm = (t * 2f) - 1f;       // -1..1

            float yOffset = curveAmount * (1f - (xNorm * xNorm));

            for (int v = 0; v < 4; v++)
            {
                var p = verts[vertIndex + v];
                p.y += yOffset;
                verts[vertIndex + v] = p;
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            _text.UpdateGeometry(meshInfo.mesh, i);
        }

        _dirty = false;
        _text.havePropertiesChanged = false;
    }
}
