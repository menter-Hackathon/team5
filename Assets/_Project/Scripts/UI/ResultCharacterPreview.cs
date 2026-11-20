using System.Collections;
using Team5.Data;
using Team5.Items;
using Team5.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // ゲームのPrefabを専用カメラで描画。移動・入力・ゲーム進行は実行しない。
    public class ResultCharacterPreview : MonoBehaviour
    {
        private readonly System.Collections.Generic.List<Material> previewMaterials = new System.Collections.Generic.List<Material>();
        private GameObject stage;
        private RenderTexture texture;
        private Camera previewCamera;
        private Camera sceneCamera;
        private int originalMask;
        private const int PreviewLayer = 31;

        public void Initialize(GameObject prefab, RoleDefinition role, Shader portraitShader)
        {
            if (prefab == null) return;
            stage = new GameObject("ResultCharacterStage");
            stage.SetActive(false);
            stage.transform.position = Vector3.zero;
            var actor = Instantiate(prefab, stage.transform, false);
            actor.name = "ResultPlayer";
            actor.tag = "Untagged";
            foreach (var script in actor.GetComponentsInChildren<MonoBehaviour>(true))
                script.enabled = script is PlayerAnimator || script is OutfitController;
            foreach (var collider in actor.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var body in actor.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
            foreach (var canvas in actor.GetComponentsInChildren<Canvas>(true)) canvas.gameObject.SetActive(false);
            foreach (string placeholder in new[] { "Body", "FrontMarker" })
                if (actor.transform.Find(placeholder) is Transform child) child.gameObject.SetActive(false);
            stage.SetActive(true);
            var outfits = actor.GetComponent<OutfitController>();
            if (outfits != null && role != null && role.outfits != null)
                foreach (var outfit in role.outfits) outfits.WearForEnding(outfit);
            // Appearanceのゲーム用更新を停止し、クリア時は色が戻った姿で表示する。
            foreach (var renderer in actor.GetComponentsInChildren<Renderer>(true))
            {
                // 透過するゲーム用シェーダーでは近接表示にちらつきが出るため、
                // 本体のテクスチャを保った不透明Litをこの複製だけに使用する。
                var sourceMaterials = renderer.sharedMaterials;
                for (int i = 0; i < sourceMaterials.Length; i++)
                {
                    var source = sourceMaterials[i];
                    if (source == null || source.shader.name != "Team5/PlayerIndividuality") continue;
                    var shader = portraitShader;
                    if (shader == null) continue;
                    var material = new Material(shader);
                    material.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
                    material.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                    material.SetFloat("_Smoothness", 0.25f);
                    previewMaterials.Add(material);
                    sourceMaterials[i] = material;
                }
                renderer.sharedMaterials = sourceMaterials;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetFloat("_Individuality", 1f);
                block.SetColor("_RoleColor", role != null ? role.roleColor : Color.white);
                renderer.SetPropertyBlock(block);
                // スロット構成を保って輪郭パスだけを非表示にする。
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || materials[i].shader.name != "Team5/Outline") continue;
                    var hiddenOutline = new MaterialPropertyBlock();
                    hiddenOutline.SetFloat("_Individuality", 0f);
                    renderer.SetPropertyBlock(hiddenOutline, i);
                }
            }
            foreach (var node in stage.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = PreviewLayer;
            texture = new RenderTexture(768, 960, 24, RenderTextureFormat.ARGB32);
            texture.name = "ClearCharacterTexture";
            texture.Create();
            GetComponent<RawImage>().texture = texture;
            var cameraObject = new GameObject("ResultPortraitCamera", typeof(Camera));
            cameraObject.transform.SetParent(stage.transform, false);
            previewCamera = cameraObject.GetComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = Color.clear;
            previewCamera.cullingMask = 1 << PreviewLayer;
            previewCamera.orthographic = true;
            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = 30f;
            previewCamera.allowHDR = false;
            previewCamera.targetTexture = texture;
            previewCamera.depth = -10;
            sceneCamera = Camera.main;
            if (sceneCamera != null)
            {
                originalMask = sceneCamera.cullingMask;
                sceneCamera.cullingMask &= ~(1 << PreviewLayer);
            }
            var lightObject = new GameObject("ResultPortraitLight", typeof(Light));
            lightObject.transform.SetParent(stage.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(35, -145, 0);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.cullingMask = 1 << PreviewLayer;
            StartCoroutine(FrameActor(actor));
        }

        private IEnumerator FrameActor(GameObject actor)
        {
            // Animatorが待機姿勢へ遷移してから、衣装を含む全身を収める。
            yield return new WaitForSecondsRealtime(0.5f);
            bool found = false;
            Bounds bounds = default;
            foreach (var renderer in actor.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (!found) yield break;
            previewCamera.transform.position = bounds.center + new Vector3(0, 0.05f, 5);
            previewCamera.transform.LookAt(bounds.center);
            previewCamera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x / previewCamera.aspect) * 1.18f;
        }

        private void OnDestroy()
        {
            if (sceneCamera != null) sceneCamera.cullingMask = originalMask;
            if (previewCamera != null) previewCamera.targetTexture = null;
            if (stage != null) Destroy(stage);
            foreach (var material in previewMaterials) Destroy(material);
            if (texture != null) { texture.Release(); Destroy(texture); }
        }
    }
}
