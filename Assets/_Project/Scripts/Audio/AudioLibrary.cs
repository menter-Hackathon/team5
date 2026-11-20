using System;
using UnityEngine;

namespace Team5.Audio
{
    public enum BgmId { Title, Omote, Ura, Clear, GameOver }
    public enum SeId { Pickup, Craft, Crafted, Caught, TaskComplete, TaskFail, Equip, Button, ParameterUp, ParameterDown, TaskAssigned }

    // BGM と SE の AudioClip を名前（BgmId / SeId）付きで持つ（CD-17）
    [CreateAssetMenu(menuName = "Team5/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Serializable] public struct BgmEntry { public BgmId id; public AudioClip clip; public bool loop; }
        [Serializable] public struct SeEntry { public SeId id; public AudioClip clip; }

        public BgmEntry[] bgm;
        public SeEntry[] se;

        public bool TryGetBgm(BgmId id, out BgmEntry entry)
        {
            foreach (var e in bgm) if (e.id == id && e.clip != null) { entry = e; return true; }
            entry = default;
            return false;
        }

        public AudioClip GetSe(SeId id)
        {
            foreach (var e in se) if (e.id == id) return e.clip;
            return null;
        }
    }
}
