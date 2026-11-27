using UnityEngine;
using TheLastBullet.Audio;
using TheLastBullet.UI;

namespace TheLastBullet.Core
{
    /// <summary>
    /// Headless-safe bootstrap. The shipped scene carries NO serialized script
    /// references, so the project builds without ever opening the Editor:
    /// this static hook creates the persistent root and every core system in
    /// code before the first scene loads.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            var root = new GameObject("TheLastBullet");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<GameManager>();
            root.AddComponent<SaveSystem>();
            root.AddComponent<SynthAudio>();
            root.AddComponent<TwinStickInput>();
            root.AddComponent<MenuController>();
            // JuiceController + camera rig are created by GameManager once the
            // scene exists; the HUD is created by MenuController.
        }
    }
}
