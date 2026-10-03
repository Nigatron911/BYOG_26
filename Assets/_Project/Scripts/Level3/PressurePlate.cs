using System;
using UnityEngine;
using Project.Audio;
using Project.Player;

namespace Project.Level3
{
    public class PressurePlate : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float requiredMass = 40.0f;
        [SerializeField] private Transform plateVisual;
        [SerializeField] private SpriteRenderer plateRenderer;
        [SerializeField] private HeavyGate targetGate;

        [Header("Plate Colors")]
        [SerializeField] private Color normalColor = new Color(0.9f, 0.75f, 0.1f, 1f);
        [SerializeField] private Color activeColor = new Color(0.2f, 1.0f, 0.4f, 1f);

        private bool isPressed = false;
        private Vector3 unpressedLocalPos;
        private Vector3 pressedLocalPos;
        private int heavyObjectsCount = 0;

        public bool IsPressed => isPressed;
        public event Action<bool> OnPlateStateChanged;

        private void Awake()
        {
            if (plateVisual == null) plateVisual = transform.Find("PlateTop");
            if (plateVisual != null)
            {
                unpressedLocalPos = plateVisual.localPosition;
                pressedLocalPos = unpressedLocalPos + new Vector3(0f, -0.15f, 0f);
            }
            if (plateRenderer == null && plateVisual != null)
            {
                plateRenderer = plateVisual.GetComponent<SpriteRenderer>();
            }
            if (plateRenderer != null)
            {
                plateRenderer.color = normalColor;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CheckAndActivate(other, true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            CheckAndActivate(other, false);
        }

        private void CheckAndActivate(Collider2D other, bool isEntering)
        {
            bool isHeavy = false;

            var anvil = other.GetComponentInParent<AnvilCreation>();
            if (anvil != null) isHeavy = true;

            var rb = other.attachedRigidbody;
            if (rb != null && rb.mass >= requiredMass) isHeavy = true;

            if (isHeavy)
            {
                if (isEntering) heavyObjectsCount++;
                else heavyObjectsCount = Mathf.Max(0, heavyObjectsCount - 1);

                UpdatePlateState(heavyObjectsCount > 0);
            }
            else if (isEntering && other.GetComponentInParent<PlayerMaterialController>() != null)
            {
                // Player stepped on plate but is too light
                ProceduralAudio.Instance?.PlayFail();
            }
        }

        private void UpdatePlateState(bool pressed)
        {
            if (isPressed == pressed) return;
            isPressed = pressed;

            if (isPressed)
            {
                ProceduralAudio.Instance?.PlaySwitchActivate();
                if (plateVisual != null) plateVisual.localPosition = pressedLocalPos;
                if (plateRenderer != null) plateRenderer.color = activeColor;
                if (targetGate != null) targetGate.SetOpen(true);
            }
            else
            {
                if (plateVisual != null) plateVisual.localPosition = unpressedLocalPos;
                if (plateRenderer != null) plateRenderer.color = normalColor;
                if (targetGate != null) targetGate.SetOpen(false);
            }

            OnPlateStateChanged?.Invoke(isPressed);
        }
    }
}
