using System.Collections.Generic;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Asks whether the player really means to throw the board away.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Restarting discards a run that may be many minutes of thought, and the button sits
    /// within thumb reach where it can be brushed. An accidental restart is unrecoverable, so
    /// it gets a question — except when the board has no moves left, where there is nothing to
    /// protect and asking would only add a tap to an already frustrating moment.
    /// </para>
    /// <para>
    /// Drawn as geometry: the project has no font, no UI canvas, and no localisation. A
    /// circular arrow between a tick and a cross needs none of them.
    /// </para>
    /// <para>
    /// The panel is opaque rather than a dimmed overlay. Unlit URP materials do not blend, and
    /// setting up a transparent variant to fade the background is more machinery than a
    /// two-button question deserves. Being unmistakably in front is what makes it modal.
    /// </para>
    /// </remarks>
    internal sealed class RestartConfirmView : MonoBehaviour
    {
        private const float PanelWidth = 2.4f;
        private const float PanelHeight = 1.3f;
        private const float ButtonRadius = 0.36f;
        private const float ButtonOffsetX = 0.72f;
        private const float ButtonOffsetY = -0.2f;
        private const float GlyphThickness = 0.075f;

        [SerializeField]
        private BoardView _boardView;

        [SerializeField]
        private Camera _camera;

        private Transform _panel;
        private Transform _confirmButton;
        private Transform _cancelButton;

        /// <summary>Whether the question is currently on screen.</summary>
        internal bool IsOpen { get; private set; }

        /// <summary>
        /// How large an area counts as pressing one of the two answers.
        /// </summary>
        internal float TouchRadiusPixels => Mathf.Min(Screen.width, Screen.height) * 0.12f;

        private Camera ResolvedCamera => _camera != null ? _camera : Camera.main;

        internal void Open()
        {
            EnsureBuilt();

            IsOpen = true;
            _panel.gameObject.SetActive(true);
        }

        internal void Close()
        {
            IsOpen = false;

            if (_panel != null)
            {
                _panel.gameObject.SetActive(false);
            }
        }

        internal bool IsConfirmPressed(Vector2 screenPosition)
            => IsOpen && IsWithin(_confirmButton, screenPosition);

        internal bool IsCancelPressed(Vector2 screenPosition)
            => IsOpen && IsWithin(_cancelButton, screenPosition);

        private bool IsWithin(Transform button, Vector2 screenPosition)
        {
            if (button == null)
            {
                return false;
            }

            var buttonScreen = ResolvedCamera.WorldToScreenPoint(button.position);
            return Vector2.Distance(screenPosition, buttonScreen) <= TouchRadiusPixels;
        }

        private void LateUpdate()
        {
            if (!IsOpen || _panel == null)
            {
                return;
            }

            // Centred on the camera each frame rather than positioned once, so it stays put if
            // the board is ever re-framed underneath it.
            var centre = ResolvedCamera.transform.position;
            _panel.position = new Vector3(centre.x, centre.y, -1f);
        }

        private void EnsureBuilt()
        {
            if (_panel != null || _boardView == null)
            {
                return;
            }

            _panel = new GameObject("Panel").transform;
            _panel.SetParent(transform, worldPositionStays: false);

            AddPart(_panel, "Background", HexMeshFactory.CreateRectangle(PanelWidth, PanelHeight),
                BoardPalette.DialogPanel, 0f);

            // The question: this action, yes or no.
            AddPart(
                _panel,
                "Subject",
                HexMeshFactory.CreateCircularArrow(0.26f, 0.075f, 265f),
                BoardPalette.RestartArrow,
                -0.02f,
                new Vector3(0f, 0.34f, 0f));

            _cancelButton = AddButton("Cancel", -ButtonOffsetX, BoardPalette.DialogCancel);
            AddCross(_cancelButton);

            _confirmButton = AddButton("Confirm", ButtonOffsetX, BoardPalette.DialogConfirm);
            AddTick(_confirmButton);

            _panel.gameObject.SetActive(false);
        }

        private Transform AddButton(string childName, float offsetX, Color colour)
        {
            var button = new GameObject(childName).transform;
            button.SetParent(_panel, worldPositionStays: false);
            button.localPosition = new Vector3(offsetX, ButtonOffsetY, -0.02f);

            AddPart(button, "Face", HexMeshFactory.CreateHexagon(ButtonRadius), colour, 0f);

            return button;
        }

        private void AddCross(Transform parent)
        {
            var arm = HexMeshFactory.CreateRectangle(ButtonRadius * 0.95f, GlyphThickness);

            AddPart(parent, "ArmA", arm, BoardPalette.RestartArrow, -0.02f, Vector3.zero, 45f);
            AddPart(parent, "ArmB", arm, BoardPalette.RestartArrow, -0.02f, Vector3.zero, -45f);
        }

        private void AddTick(Transform parent)
        {
            var shortArm = HexMeshFactory.CreateRectangle(ButtonRadius * 0.55f, GlyphThickness);
            var longArm = HexMeshFactory.CreateRectangle(ButtonRadius * 0.95f, GlyphThickness);

            AddPart(
                parent, "Short", shortArm, BoardPalette.RestartArrow, -0.02f,
                new Vector3(-0.11f, -0.06f, 0f), -50f);
            AddPart(
                parent, "Long", longArm, BoardPalette.RestartArrow, -0.02f,
                new Vector3(0.05f, 0.02f, 0f), 52f);
        }

        private void AddPart(
            Transform parent,
            string childName,
            Mesh mesh,
            Color colour,
            float depth,
            Vector3 offset = default,
            float rotationDegrees = 0f)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(parent, worldPositionStays: false);
            child.transform.localPosition = new Vector3(offset.x, offset.y, depth);
            child.transform.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);

            child.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _boardView.TileMaterial;
            renderer.material.color = colour;
        }
    }
}
