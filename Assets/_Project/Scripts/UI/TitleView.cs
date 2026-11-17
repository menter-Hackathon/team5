using Team5.Core;
using Team5.Data;
using UnityEngine;

namespace Team5.UI
{
    // 仮のタイトル画面。見た目の作り込みは CD-15、ロール選択は CD-20
    public class TitleView : MonoBehaviour
    {
        [SerializeField] private RoleDefinition defaultRole; // MVP は Role_Sample

        // 「はじめる」ボタンから呼ぶ
        public void OnStartPressed()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("GameManager がありません。Title シーンから再生してください。", this);
                return;
            }
            GameManager.Instance.StartGame(defaultRole);
        }
    }
}
