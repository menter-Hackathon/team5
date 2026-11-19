using System;
using Team5.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Team5.UI
{
    // 着替えタブの服のカード（ui-design.md 5章・outfits.md 6章）
    public class OutfitCardView : MonoBehaviour
    {
        public enum State { NotFound, Locked, CanWear, Wearing }

        [SerializeField] private GameObject selectedFrame; // 着ている服の枠
        [SerializeField] private Text stageLabel;
        [SerializeField] private Image icon;
        [SerializeField] private Text iconLabel;            // アイコンがないとき（見つけていなければ「？」）
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text noteLabel;
        [SerializeField] private Button button;
        [SerializeField] private Image buttonImage;
        [SerializeField] private Text buttonLabel;
        [SerializeField] private Sprite primarySprite;      // UI_Button_Primary
        [SerializeField] private Sprite secondarySprite;    // UI_Button_Secondary
        [SerializeField] private Sprite disabledSprite;     // UI_Button_Disabled
        [SerializeField] private Color textColor = new Color32(0x3B, 0x35, 0x30, 0xFF);
        [SerializeField] private Color grayColor = new Color32(0x9A, 0x96, 0x8F, 0xFF);

        public OutfitDefinition Outfit { get; private set; }
        public Button Button => button;
        public event Action<OutfitCardView> OnWearClicked;

        private void Awake() => button.onClick.AddListener(() => OnWearClicked?.Invoke(this));

        public void Show(OutfitDefinition outfit, State state, string previousName)
        {
            Outfit = outfit;
            bool found = state != State.NotFound;
            var sprite = outfit.item != null ? outfit.item.icon : null;
            icon.sprite = sprite;
            icon.gameObject.SetActive(sprite != null);
            icon.color = found ? Color.white : new Color(0.2f, 0.2f, 0.2f, 0.6f); // 見つけていなければ黒いシルエット
            iconLabel.gameObject.SetActive(sprite == null);
            iconLabel.text = found ? outfit.displayName : "？";

            stageLabel.text = $"段階 {outfit.stage}";
            nameLabel.text = found ? outfit.displayName : $"{outfit.displayName} ？";
            nameLabel.color = found ? textColor : grayColor;
            selectedFrame.SetActive(state == State.Wearing);

            switch (state)
            {
                case State.NotFound:
                    noteLabel.text = "まだ見つけていない（村のどこか）";
                    SetButton(disabledSprite, "見つけていない", false);
                    break;
                case State.Locked:
                    noteLabel.text = $"先に{previousName}を着よう";
                    SetButton(disabledSprite, "着る", false);
                    break;
                case State.CanWear:
                    noteLabel.text = $"初めて着ると 個性 +{outfit.individualityReward:0}";
                    SetButton(primarySprite, "着る", true);
                    break;
                default:
                    noteLabel.text = $"初めて着ると 個性 +{outfit.individualityReward:0}";
                    SetButton(secondarySprite, "着ている", false);
                    break;
            }
            noteLabel.color = state == State.NotFound || state == State.Locked ? grayColor : textColor;
        }

        private void SetButton(Sprite sprite, string label, bool interactable)
        {
            if (sprite != null) buttonImage.sprite = sprite;
            buttonLabel.text = label;
            buttonLabel.color = interactable || label == "着ている" ? textColor : grayColor;
            button.interactable = interactable;
        }
    }
}
