using NUnit.Framework;

using UnityEngine;
using UnityEngine.Rendering;

public sealed class VisionVisualizer : MonoBehaviour
{
    private readonly int segments = 32;
    [SerializeField] float yOffset = 0.5f;
    [SerializeField] float lineWidth = 0.5f;
    [SerializeField] Color lineColor = new Color(0f, 1f, 1f, 5f / 255f);
    public NpcVisionHearing npc;
    
    LineRenderer lr;
    Vector3[] pts;
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        if (npc == null) npc = GetComponentInParent<NpcVisionHearing>();
        Assert.IsNotNull(npc, "npc not assigned nor parent, please assign it in inspector.");

        lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.widthMultiplier = lineWidth;
        lr.numCornerVertices = 2;
        lr.numCapVertices = 2;
        lr.shadowCastingMode = ShadowCastingMode.Off;
        lr.receiveShadows = false;

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        var mat = new Material(shader);
        
        ;
        ;
        ;
        ;

        // URP transparent setup - mirrors Inspector Transparent + Alpha blend
        mat.SetFloat("_Surface", 1f);                  // Transparent
        mat.SetFloat("_AlphaClip", 0f);                // Off
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAMODULATE_ON");


        mat.SetFloat("_Blend", 0f);                     // Alpha mode
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");      // URP uses this for Alpha mode
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);                       // Off
        mat.renderQueue = 3000;                          // Transparent queue

        if (mat.HasProperty("_BaseColor")) mat.SetColor(BaseColorPropertyId, lineColor);
        if (mat.HasProperty("_Color")) mat.SetColor(ColorPropertyId, lineColor);

        lr.material = mat;

        pts = new Vector3[segments + 3];
        lr.positionCount = pts.Length;
    }

    void LateUpdate()
    {
        float sideVisionAngle = npc.sideVisionAngle;
        float visionLength = npc.visionLength;

        Vector3 origin = npc.transform.position + new Vector3(0f, yOffset, 0f);
        float total = sideVisionAngle * 2f;
        float step = total / segments;

        pts[0] = origin;

        for (int i = 0; i <= segments; i++)
        {
            float ang = -sideVisionAngle + step * i;
            Quaternion rot = Quaternion.AngleAxis(ang, Vector3.up);
            Vector3 dir = rot * transform.forward;
            pts[1 + i] = origin + dir * visionLength;
        }

        pts[pts.Length - 1] = origin;

        // Multiply material tint by the same faint vertex color
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.widthMultiplier = lineWidth;

        lr.positionCount = pts.Length;
        lr.SetPositions(pts);
    }
}
