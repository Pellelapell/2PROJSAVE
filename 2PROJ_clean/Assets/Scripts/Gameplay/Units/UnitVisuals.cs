using UnityEngine;
using SupKonQuest;

[RequireComponent(typeof(UnitStats))]
public class UnitVisuals : MonoBehaviour
{
    private UnitStats stats;

    private void Awake()
    {
        stats = GetComponent<UnitStats>();
    }

    public void ApplyRaceVisuals()
    {
        if (stats == null) return;

        RaceDefinition def = RaceRegistry.Get(stats.race);
        if (def == null) return;

        var skin = def.GetUnitSkin(stats.unitType);
        if (!skin.HasValue) return;

        if (skin.Value.modelPrefab == null)
        {
            if (skin.Value.mesh == null) return;

            Renderer placeholder = GetComponent<Renderer>();
            if (placeholder != null) placeholder.enabled = false;

            GameObject model = new GameObject("Model");
            model.transform.SetParent(transform, false);
            model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            model.transform.localScale = Vector3.one;

            MeshFilter mf = model.AddComponent<MeshFilter>();
            mf.sharedMesh = skin.Value.mesh;

            MeshRenderer mr = model.AddComponent<MeshRenderer>();
            if (skin.Value.material != null) mr.material = skin.Value.material;
            return;
        }

        Renderer placeholder = GetComponent<Renderer>();
        if (placeholder != null) placeholder.enabled = false;

        GameObject model = Instantiate(skin.Value.modelPrefab, transform, false);
        model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        model.transform.localScale = Vector3.one;

        Animator wrapperAnim = model.GetComponent<Animator>();
        if (wrapperAnim != null && model.transform.childCount > 0)
        {
            Transform fbxRoot = model.transform.GetChild(0);
            RuntimeAnimatorController ctrl = wrapperAnim.runtimeAnimatorController;
            Destroy(wrapperAnim);
            Animator fbxAnim = fbxRoot.gameObject.AddComponent<Animator>();
            fbxAnim.runtimeAnimatorController = ctrl;
            fbxAnim.applyRootMotion = false;
        }
    }
}
