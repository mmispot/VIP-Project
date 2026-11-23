using UnityEngine;

namespace VIPProject.Core
{
    /// <summary>Anything the player can look at and press Interact on (pickups, levers, doors...).</summary>
    public interface IInteractable
    {
        /// <summary>Short text shown on screen while the player looks at this, e.g. "Pick up Brass Key".</summary>
        string Prompt { get; }

        void Interact(GameObject interactor);
    }
}
