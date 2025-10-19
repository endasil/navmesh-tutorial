using NUnit.Framework;
using UnityEngine;
using System;
using System.Reflection;

public class HearingVisualizer : MonoBehaviour
{
    [SerializeField] private int segments = 64;
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private float yOffset = 0.5f;
    [SerializeField] private Color lineColor = Color.darkRed;
    // public NpcScript npc;
    public NpcVisionHearing npc;
    private LineRenderer lr;

    void Awake()
    {
        if (npc == null)
            npc = GetComponentInParent<NpcVisionHearing>();

        Assert.IsNotNull(npc, "npc not assigned nor parent, please assign it in inspector.");

        lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.widthMultiplier = lineWidth;

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        var mat = new Material(shader);

        // URP: make it actually Transparent
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);     // Alpha
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHAMODULATE_ON");
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");

        // Explicit blend/depth for transparency
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        // Set alpha 8/255 on both material and LR vertex colors
        Color transparentColor = new Color(lineColor.r, lineColor.g, lineColor.b, 15f / 255f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", transparentColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", transparentColor);
        lr.material = mat;

        // add these two so LR’s vertex colors carry the same alpha
        lr.startColor = transparentColor;
        lr.endColor = transparentColor;

        lr.positionCount = segments;
    }


    void LateUpdate()
    {
        float radius = npc.hearingRange;
        Vector3 center = npc.transform.position + Vector3.up * yOffset;
        float step = 2f * Mathf.PI / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * step;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, center + new Vector3(x, 0f, z));
        }
    }

}
