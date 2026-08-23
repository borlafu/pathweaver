using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Android;
using UnityEngine;

namespace Pathweaver.EditorTools
{
    /// <summary>
    /// Adds the permissions the game needs to the generated Android manifest.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unity infers permissions from the engine APIs a build uses. The vibrator is reached
    /// through <c>AndroidJavaClass</c> instead, so nothing tells Unity the game vibrates and
    /// <c>VIBRATE</c> was left out — Android then refused every call with a SecurityException
    /// while the game carried on as though it had buzzed.
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

            if (added == 0)
            {
                return;
            }

            document.Save(manifestPath);
            Debug.Log($"[manifest] added {added} permission(s) to {manifestPath}");
        }
    }
}
