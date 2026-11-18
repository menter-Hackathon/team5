using UnityEngine;

namespace Team5.UI
{
    // タイトルの背景。村のまわりをカメラがゆっくり回る（ui-design.md 3 章）
    public class TitleCameraOrbit : MonoBehaviour
    {
        [SerializeField] private Vector3 center = Vector3.zero;
        [SerializeField] private float radius = 16f;
        [SerializeField] private float height = 9f;
        [SerializeField] private float degreesPerSecond = 4f;

        private float angle = 200f;

        private void Update()
        {
            angle += degreesPerSecond * Time.unscaledDeltaTime;
            float rad = angle * Mathf.Deg2Rad;
            transform.position = center + new Vector3(Mathf.Cos(rad) * radius, height, Mathf.Sin(rad) * radius);
            transform.LookAt(center + Vector3.up * 1.5f);
        }
    }
}
