using UnityEngine;
using UnityEngine.UI;
using VIPProject.Player;

namespace VIPProject.UI
{
    /// <summary>Shows "[E] Pick up Brass Key" while the player looks at an interactable.</summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private Text label;
        [SerializeField] private string keyHint = "E";

        private void OnEnable()
        {
            if (interactor == null || label == null)
            {
                Debug.LogError("InteractionPromptUI: interactor and label must be assigned.", this);
                enabled = false;
                return;
            }
            interactor.PromptChanged += OnPromptChanged;
            OnPromptChanged(null);
        }

        private void OnDisable()
        {
            if (interactor != null) interactor.PromptChanged -= OnPromptChanged;
        }

        // Only runs when the looked-at target changes, so the string concat is not per-frame.
        private void OnPromptChanged(string prompt)
        {
            bool show = !string.IsNullOrEmpty(prompt);
            label.enabled = show;
            if (show) label.text = $"[{keyHint}] {prompt}";
        }
    }
}
