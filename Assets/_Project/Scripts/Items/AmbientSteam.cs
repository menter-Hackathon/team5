using UnityEngine;

namespace Team5.Items
{
    // 作業台（台所）から湯気をゆっくり上げる。作っている間は湯気が増える
    public class AmbientSteam : MonoBehaviour
    {
        [SerializeField] private Material particleMaterial;
        [SerializeField] private Vector3 offset = new(0f, 1.1f, 0f);
        [SerializeField] private float idleRate = 4f;
        [SerializeField] private float craftingRate = 22f;

        private ParticleSystem steam;

        private void Start()
        {
            var go = new GameObject("Steam");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = offset;
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            steam = go.AddComponent<ParticleSystem>();
            steam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = steam.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startColor = new Color(1f, 1f, 1f, 0.35f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = steam.emission;
            emission.rateOverTime = idleRate;
            var shape = steam.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.15f;
            var size = steam.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));
            var color = steam.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.5f, 0.2f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
            steam.Play();
        }

        private void Update()
        {
            var context = Team5.Core.GameSceneContext.Instance;
            bool crafting = context != null && context.Crafting != null && context.Crafting.IsCrafting;
            var emission = steam.emission;
            emission.rateOverTime = crafting ? craftingRate : idleRate;
        }
    }
}
