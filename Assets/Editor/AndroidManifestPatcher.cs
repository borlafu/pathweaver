using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Android;
using UnityEngine;

namespace Pathweaver.EditorTools
{
    /// <summary>
    /// Adds the permissions the game needs to the generated Android manifest, and opts out of the
    /// system's predictive back animation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unity infers permissions from the engine APIs a build uses. The vibrator is reached
    /// through <c>AndroidJavaClass</c> instead, so nothing tells Unity the game vibrates and
    /// <c>VIBRATE</c> was left out — Android then refused every call with a SecurityException
    /// while the game carried on as though it had buzzed.
    /// </para>
    /// <para>
    /// Predictive back is the other reason this exists. From Android 13 the system may animate the
    /// window shrinking as a preview of leaving the app while a back gesture is in progress, and it
    /// decides whether to do so from a manifest flag rather than from whether the app handles back. The
    /// game reads back through the legacy Escape key, so the preview was shown over a game that then did
    /// not leave — which looked like the interface scaling down for no reason. Turning the flag off says
    /// plainly that this app answers back itself.
    /// </para>
    /// <para>
    /// Patching the generated manifest rather than checking in a hand-written one keeps a single
    /// source of truth. A full manifest committed alongside Unity's own would silently drift from
    /// whatever the Editor generates next.
    /// </para>
    /// </remarks>
    internal sealed class AndroidManifestPatcher : IPostGenerateGradleAndroidProject
    {
        private static readonly string[] RequiredPermissions =
        {
            "android.permission.VIBRATE",
        };

        private static readonly XNamespace Android = "http://schemas.android.com/apk/res/android";

        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError($"[manifest] not found at {manifestPath}");
                return;
            }

            var document = XDocument.Load(manifestPath);
            var manifest = document.Root;
            if (manifest == null)
            {
                Debug.LogError("[manifest] has no root element.");
                return;
            }

            var changed = DisablePredictiveBack(manifest);

            var added = 0;
            foreach (var permission in RequiredPermissions)
            {
                var alreadyPresent = manifest
                    .Elements("uses-permission")
                    .Any(element => (string)element.Attribute(Android + "name") == permission);

                if (alreadyPresent)
                {
                    continue;
                }

                manifest.Add(new XElement("uses-permission", new XAttribute(Android + "name", permission)));
                added++;
            }

            if (added == 0 && !changed)
            {
                return;
            }

            document.Save(manifestPath);
            Debug.Log(
                $"[manifest] added {added} permission(s)"
                + (changed ? " and disabled predictive back" : string.Empty)
                + $" in {manifestPath}");
        }

        /// <summary>
        /// Tells Android not to preview leaving the app during a back gesture.
        /// </summary>
        /// <returns>Whether the manifest needed changing.</returns>
        private static bool DisablePredictiveBack(XElement manifest)
        {
            var application = manifest.Element("application");
            if (application == null)
            {
                Debug.LogError("[manifest] has no application element.");
                return false;
            }

            var flag = Android + "enableOnBackInvokedCallback";
            if ((string)application.Attribute(flag) == "false")
            {
                return false;
            }

            application.SetAttributeValue(flag, "false");
            return true;
        }
    }
}
