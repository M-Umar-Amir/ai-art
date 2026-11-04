using UnityEngine;
using UnityEngine.UI;

namespace DallasDash.UI
{
    /// Keeps selected, disabled and bright action labels legible when screens update a button's tint.
    public sealed class ButtonVisualState : MonoBehaviour
    {
        Button button;
        Image face;
        Text[] labels;
        Color previous;
        bool enabledBefore;
        void Start()
        {
            button = GetComponent<Button>();
            face = transform.Find("Face")?.GetComponent<Image>();
            labels = face ? face.GetComponentsInChildren<Text>(true) : new Text[0];
            previous = new Color(-1, -1, -1, -1);
        }
        void LateUpdate()
        {
            if (!face || !button || labels == null) return;
            if (face.color == UiKit.Green) face.color = new Color(.035f, .25f, .22f);
            if (previous == face.color && enabledBefore == button.interactable) return;
            previous = face.color; enabledBefore = button.interactable;
            float light = previous.r * .213f + previous.g * .715f + previous.b * .072f;
            Color color = light > .43f ? UiKit.Ink : !enabledBefore ? UiKit.Muted : Color.white;
            foreach (var label in labels) if (label) label.color = color;
        }
    }
}
