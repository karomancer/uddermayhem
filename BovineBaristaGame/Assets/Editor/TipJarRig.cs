using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// Writes a three-bone chain (base, belly, rim) and a weighted grid mesh into both tip jar sprites, the same
/// data the Skinning Editor would save, so the jar can squash and stretch at runtime with SpriteSkin.
/// Re-run from the menu after the art changes; weights can then be repainted in the Skinning Editor.
/// </summary>
public static class TipJarRig
{
    const string FrontPath = "Assets/Sprites/TipJar/tipjar-front.png";
    const string BackPath = "Assets/Sprites/TipJar/tipjar-back.png";
    const string CoinBodyPath = "Assets/Sprites/TipJar/tipjar-coin_body.png";
    const string CoinTop0Path = "Assets/Sprites/TipJar/tipjar-coin_top_0.png";
    const string CoinTop1Path = "Assets/Sprites/TipJar/tipjar-coin_top_1.png";

    // Sprite-rect pixels, origin bottom-left. The glass leans about 8 degrees, so the chain follows the body's axis.
    // The base sits where the jar meets the bottom of the screen (the art continues below it), so a squash
    // compresses the visible jar toward the screen edge instead of toward the hidden foot.
    static readonly Vector2 BasePosition = new Vector2(1042f, 718f);
    const float AxisDegrees = 98f;
    const float BaseLength = 215f;
    const float BellyLength = 190f;
    const float RimLength = 700f;
    const float BellyPeak = 215f;   // distance along the axis where the belly bone has full influence (the label)
    const float RimPeak = 410f;     // from here up the rim bone owns the vertices, so the rim rings stay rigid
    const int GridColumns = 16;
    const int GridRows = 14;

    [MenuItem("Udder Mayhem/Rig Tip Jar")]
    public static void RigAll()
    {
        Rig(FrontPath, glassChain: true);
        Rig(BackPath, glassChain: true);
        Rig(CoinBodyPath, glassChain: false);
        Rig(CoinTop0Path, glassChain: false);
        Rig(CoinTop1Path, glassChain: false);
        AssetDatabase.Refresh();
        Debug.Log("[TipJarRig] rigged front, back and coins");
    }

    // The glass gets the base/belly/rim chain; the coin pile gets one bone in the base bone's pose, so at runtime
    // it can hang under the base bone and slide along the jar's axis to show the fill level
    static void Rig(string path, bool glassChain)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { Debug.LogError($"[TipJarRig] no texture importer at {path}"); return; }

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        SpriteRect rect = provider.GetSpriteRects()[0];
        GUID id = rect.spriteID;

        var bones = glassChain
            ? new List<SpriteBone>
            {
                new SpriteBone { name = "base", guid = GUID.Generate().ToString(), position = BasePosition, rotation = Quaternion.Euler(0f, 0f, AxisDegrees), length = BaseLength, parentId = -1, color = Color.white },
                new SpriteBone { name = "belly", guid = GUID.Generate().ToString(), position = new Vector2(BaseLength, 0f), rotation = Quaternion.identity, length = BellyLength, parentId = 0, color = Color.white },
                new SpriteBone { name = "rim", guid = GUID.Generate().ToString(), position = new Vector2(BellyLength, 0f), rotation = Quaternion.identity, length = RimLength, parentId = 1, color = Color.white },
            }
            : new List<SpriteBone>
            {
                new SpriteBone { name = "coins", guid = GUID.Generate().ToString(), position = BasePosition, rotation = Quaternion.Euler(0f, 0f, AxisDegrees), length = BaseLength + BellyLength + RimLength, parentId = -1, color = Color.white },
            };
        provider.GetDataProvider<ISpriteBoneDataProvider>().SetBones(id, bones);

        var vertices = new List<Vertex2DMetaData>();
        var indices = new List<int>();
        var edges = new List<Vector2Int>();
        float width = rect.rect.width, height = rect.rect.height;
        Vector2 axis = new Vector2(Mathf.Cos(AxisDegrees * Mathf.Deg2Rad), Mathf.Sin(AxisDegrees * Mathf.Deg2Rad));
        for (int r = 0; r <= GridRows; r++)
        {
            for (int c = 0; c <= GridColumns; c++)
            {
                var p = new Vector2(width * c / GridColumns, height * r / GridRows);
                vertices.Add(new Vertex2DMetaData { position = p, boneWeight = glassChain ? WeightsFor(Vector2.Dot(p - BasePosition, axis)) : new BoneWeight { boneIndex0 = 0, weight0 = 1f } });
            }
        }
        int stride = GridColumns + 1;
        for (int r = 0; r < GridRows; r++)
        {
            for (int c = 0; c < GridColumns; c++)
            {
                int a = r * stride + c, b = a + 1, d = a + stride, e = d + 1;
                indices.AddRange(new[] { a, d, b, b, d, e });
            }
        }
        for (int c = 0; c < GridColumns; c++) { edges.Add(new Vector2Int(c, c + 1)); edges.Add(new Vector2Int(GridRows * stride + c, GridRows * stride + c + 1)); }
        for (int r = 0; r < GridRows; r++) { edges.Add(new Vector2Int(r * stride, (r + 1) * stride)); edges.Add(new Vector2Int(r * stride + GridColumns, (r + 1) * stride + GridColumns)); }

        var mesh = provider.GetDataProvider<ISpriteMeshDataProvider>();
        mesh.SetVertices(id, vertices.ToArray());
        mesh.SetIndices(id, indices.ToArray());
        mesh.SetEdges(id, edges.ToArray());

        provider.Apply();
        importer.SaveAndReimport();
    }

    // Piecewise blend along the axis: all base at the foot, all belly at BellyPeak, all rim from RimPeak up
    static BoneWeight WeightsFor(float u)
    {
        var w = new BoneWeight();
        if (u <= 0f) { w.boneIndex0 = 0; w.weight0 = 1f; return w; }
        if (u < BellyPeak)
        {
            float t = Mathf.SmoothStep(0f, 1f, u / BellyPeak);
            w.boneIndex0 = 0; w.weight0 = 1f - t; w.boneIndex1 = 1; w.weight1 = t; return w;
        }
        if (u < RimPeak)
        {
            float t = Mathf.SmoothStep(0f, 1f, (u - BellyPeak) / (RimPeak - BellyPeak));
            w.boneIndex0 = 1; w.weight0 = 1f - t; w.boneIndex1 = 2; w.weight1 = t; return w;
        }
        w.boneIndex0 = 2; w.weight0 = 1f; return w;
    }
}
