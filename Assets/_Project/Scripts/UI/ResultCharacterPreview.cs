using System.Collections;
using System.Collections.Generic;
using Team5.Core;
using Team5.Data;
using Team5.Items;
using Team5.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // ゲームのPrefabを専用カメラで描画。移動・入力・ゲーム進行は実行しない。
    // 結果ごとに、ゲームの中のモデル（プレイヤー・村人・完成した物）を並べて結果の場面を作る
    public class ResultCharacterPreview : MonoBehaviour
    {
        // 村人のモデル（ゲームと同じ FBX・マテリアル・アニメーション）
        public struct Villager
        {
            public GameObject model;
            public Material material;
            public RuntimeAnimatorController controller;
        }

        private readonly List<Material> previewMaterials = new List<Material>();
        private readonly List<Object> created = new List<Object>();
        private GameObject stage;
        private RenderTexture texture;
        private Camera previewCamera;
        private Camera sceneCamera;
        private int originalMask;
        private const int PreviewLayer = 31;
        private static readonly Color SeeingColor = new Color(0.9f, 0.15f, 0.15f, 0.35f); // VisionSensor と同じ赤

        public void Initialize(GameObject prefab, RoleDefinition role, Shader portraitShader)
        {
            Initialize(prefab, role, portraitShader, GameResult.Clear, null);
        }

        public void Initialize(GameObject prefab, RoleDefinition role, Shader portraitShader, GameResult result, Villager[] villagers)
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

            bool clear = result == GameResult.Clear;
            // クリアはなりたい姿の服を着る。色が消えたときは服も色もない
            var outfits = actor.GetComponent<OutfitController>();
            if (clear && outfits != null && role != null && role.outfits != null)
                foreach (var outfit in role.outfits) outfits.WearForEnding(outfit);
            float individuality = result == GameResult.GameOverFaded ? 0f : clear ? 1f : 0.5f;
            PreparePlayerMaterials(actor, role, portraitShader, individuality);

            BuildScene(actor.transform, role, result, villagers);

            foreach (var node in stage.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = PreviewLayer;
            // 表示する枠と同じ縦横比にして、絵がゆがまないようにする
            var frame = ((RectTransform)transform).rect.size;
            float aspect = frame.x > 1f && frame.y > 1f ? frame.x / frame.y : 0.8f;
            int height = 960;
            int width = Mathf.Clamp(Mathf.RoundToInt(height * aspect), 256, 2048);
            texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            texture.name = "ResultCharacterTexture";
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
            previewCamera.farClipPlane = 40f;
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
            lightObject.transform.rotation = Quaternion.Euler(result == GameResult.GameOverTimeUp ? 15 : 35, -145, 0);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            // 時間切れは夕日の色
            if (result == GameResult.GameOverTimeUp) light.color = new Color(1f, 0.62f, 0.38f);
            light.cullingMask = 1 << PreviewLayer;
            StartCoroutine(FrameActor(stage, clear ? Vector3.zero : new Vector3(0f, 3.2f, 0f)));
        }

        private void PreparePlayerMaterials(GameObject actor, RoleDefinition role, Shader portraitShader, float individuality)
        {
            // Appearanceのゲーム用更新を停止し、結果に合わせた色で表示する。
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
                    var baseColor = source.GetColor("_BaseColor");
                    if (individuality <= 0f)
                    {
                        float gray = baseColor.grayscale * 0.8f;
                        baseColor = new Color(gray, gray, gray, baseColor.a);
                    }
                    material.SetColor("_BaseColor", baseColor);
                    material.SetFloat("_Smoothness", 0.25f);
                    previewMaterials.Add(material);
                    sourceMaterials[i] = material;
                }
                renderer.sharedMaterials = sourceMaterials;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetFloat("_Individuality", individuality);
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
        }

        // 結果ごとの並べ方
        private void BuildScene(Transform player, RoleDefinition role, GameResult result, Villager[] villagers)
        {
            switch (result)
            {
                case GameResult.Clear:
                    // 完成した物を手元に
                    var goal = role != null && role.goalItem != null ? role.goalItem.worldPrefab : null;
                    if (goal != null) AddProp(goal, new Vector3(0.55f, 0.85f, 0.45f), 160f);
                    break;
                case GameResult.GameOverPeace:
                    // 見つかった：村人 3 人が囲み、視界の扇形がプレイヤーに向く。頭の上に「！」
                    player.rotation = Quaternion.Euler(0f, 0f, 0f);
                    Vector3[] around = { new Vector3(-1.7f, 0f, -0.4f), new Vector3(1.7f, 0f, -0.4f), new Vector3(0f, 0f, -1.9f) };
                    for (int i = 0; i < around.Length; i++)
                    {
                        var npc = AddVillager(villagers, i, around[i], player.position);
                        if (npc == null) continue;
                        AddCone(npc, 2.3f, 50f);
                        AddMark(npc, 2.1f);
                    }
                    break;
                case GameResult.GameOverTimeUp:
                    // 時間切れ：夕日の中、村長が仕事を言いつけに来る
                    player.rotation = Quaternion.Euler(0f, 20f, 0f);
                    AddVillager(villagers, 0, new Vector3(1.3f, 0f, 0.3f), player.position);
                    break;
                case GameResult.GameOverFaded:
                    // 色が消えた：同じ向きで並ぶ村人の中にまぎれる
                    float[] xs = { -1.5f, 1.5f, -0.75f };
                    for (int i = 0; i < xs.Length; i++)
                    {
                        var npc = AddVillager(villagers, i, new Vector3(xs[i], 0f, i == 2 ? -1.2f : -0.3f), null);
                        if (npc != null) npc.rotation = Quaternion.identity;
                    }
                    break;
            }
        }

        private Transform AddVillager(Villager[] villagers, int index, Vector3 position, Vector3? lookAt)
        {
            if (villagers == null || villagers.Length == 0) return null;
            var data = villagers[index % villagers.Length];
            if (data.model == null) return null;
            var npc = Instantiate(data.model, stage.transform, false);
            npc.transform.localPosition = position;
            if (lookAt.HasValue)
            {
                Vector3 dir = lookAt.Value - position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f) npc.transform.rotation = Quaternion.LookRotation(dir);
            }
            foreach (var script in npc.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            foreach (var collider in npc.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            if (data.material != null)
                foreach (var renderer in npc.GetComponentsInChildren<Renderer>(true))
                {
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++) materials[i] = data.material;
                    renderer.sharedMaterials = materials;
                }
            var animator = npc.GetComponentInChildren<Animator>();
            if (animator != null && data.controller != null) animator.runtimeAnimatorController = data.controller;
            return npc.transform;
        }

        private void AddProp(GameObject prefab, Vector3 position, float yaw)
        {
            var prop = Instantiate(prefab, stage.transform, false);
            prop.transform.localPosition = position;
            prop.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            foreach (var script in prop.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            foreach (var collider in prop.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var body in prop.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
        }

        // ゲームの視界と同じ扇形（VisionSensor と同じ作り方・同じ赤）
        private void AddCone(Transform owner, float distance, float angle)
        {
            const int segments = 24;
            var mesh = new Mesh { name = "ResultVisionCone" };
            var vertices = new Vector3[segments + 2];
            var colors = new Color[segments + 2];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            for (int i = 0; i <= segments; i++)
            {
                float a = (-angle * 0.5f + angle * i / segments) * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * distance;
            }
            for (int i = 0; i < segments; i++) { triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = i + 2; }
            for (int i = 0; i < colors.Length; i++) colors[i] = SeeingColor;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors = colors;
            mesh.RecalculateBounds();
            created.Add(mesh);

            var cone = new GameObject("VisionCone", typeof(MeshFilter), typeof(MeshRenderer));
            cone.transform.SetParent(owner, false);
            cone.transform.localPosition = Vector3.up * 0.05f;
            cone.GetComponent<MeshFilter>().sharedMesh = mesh;
            var material = new Material(Shader.Find("Sprites/Default"));
            created.Add(material);
            cone.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        // 頭の上の「！」（立てた棒と点）
        private void AddMark(Transform owner, float height)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var material = new Material(shader);
            var color = new Color(1f, 0.82f, 0.24f);
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            created.Add(material);
            var mark = new GameObject("Exclamation").transform;
            mark.SetParent(owner, false);
            mark.localPosition = Vector3.up * height;
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.transform.SetParent(mark, false);
            bar.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            bar.transform.localScale = new Vector3(0.12f, 0.38f, 0.12f);
            var dot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dot.transform.SetParent(mark, false);
            dot.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            dot.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            foreach (var part in new[] { bar, dot })
            {
                Destroy(part.GetComponent<Collider>());
                part.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        private IEnumerator FrameActor(GameObject root, Vector3 lift)
        {
            // Animatorが待機姿勢へ遷移してから、衣装を含む全体を収める。
            yield return new WaitForSecondsRealtime(0.5f);
            bool found = false;
            Bounds bounds = default;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (!found) yield break;
            // ゲームオーバーは少し上から見下ろして、囲まれている様子を見せる
            previewCamera.transform.position = bounds.center + new Vector3(0, 0.05f, 5) + lift;
            previewCamera.transform.LookAt(bounds.center);
            float size = Mathf.Max(bounds.extents.y, bounds.extents.x / previewCamera.aspect);
            if (lift != Vector3.zero) size = Mathf.Max(size, bounds.extents.z * 0.6f);
            previewCamera.orthographicSize = size * 1.18f;
        }

        private void OnDestroy()
        {
            if (sceneCamera != null) sceneCamera.cullingMask = originalMask;
            if (previewCamera != null) previewCamera.targetTexture = null;
            if (stage != null) Destroy(stage);
            foreach (var material in previewMaterials) Destroy(material);
            foreach (var item in created) if (item != null) Destroy(item);
            if (texture != null) { texture.Release(); Destroy(texture); }
        }
    }
}
