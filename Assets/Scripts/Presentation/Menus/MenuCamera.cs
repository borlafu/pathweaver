using UnityEngine;

namespace Pathweaver.Game.Presentation.Menus
{
    /// <summary>
    /// The framing every menu is drawn against.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Menus place their buttons in viewport fractions but size them in world units, so they only
    /// hold their proportions while the camera keeps one zoom. <see cref="BoardCameraFitter"/>
    /// rezooms for each board, which meant a menu opened after a wide level drew smaller buttons
    /// than the same menu opened from a cold start — the layout depended on which level had been
    /// played last.
    /// </para>
    /// <para>
    /// Restoring this framing whenever a menu is shown makes that impossible.
    /// </para>
    /// </remarks>
    internal static class MenuCamera
    {
        /// <summary>Orthographic half-height a menu is laid out for.</summary>
        internal const float OrthographicSize = 3.2f;

        /// <summary>Puts the camera back where the menus expect it.</summary>
        internal static void Frame(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            camera.orthographic = true;
            camera.orthographicSize = OrthographicSize;

            var position = camera.transform.position;
            camera.transform.position = new Vector3(0f, 0f, position.z);
        }

        /// <summary>The world size of the visible area, for converting a layout into world units.</summary>
        /// <summary>
        /// How tall a world radius is, as a fraction of screen height.
        /// </summary>
        /// <remarks>
        /// The conversion the layout kept getting wrong. Positions in this codebase are viewport
        /// fractions while a control's radius is world units, and comparing the two directly is what put
        /// every settings label inside the switch it was labelling: an offset of 0.055 looked generous
        /// next to text 0.017 tall and was less than the switch's own 0.086 half-height.
        /// </remarks>
        internal static float ViewportHalfHeight(float worldRadius)
            => worldRadius / (OrthographicSize * 2f);

        /// <summary>
        /// How wide a world radius is, as a fraction of screen width.
        /// </summary>
        /// <remarks>
        /// Much larger than the vertical figure on a portrait phone, because the same world distance
        /// covers far more of a narrow screen. A hexagon of radius 0.55 is a fifth of the height and
        /// two fifths of the width.
        /// </remarks>
        internal static float ViewportHalfWidth(float worldRadius, float aspect)
            => worldRadius / (OrthographicSize * 2f * (aspect > 0f ? aspect : 1f));

        internal static Vector2 WorldExtents(Camera camera)
        {
            var height = OrthographicSize * 2f;
            var aspect = camera != null && camera.aspect > 0f ? camera.aspect : 1f;
            return new Vector2(height * aspect, height);
        }
    }
}
