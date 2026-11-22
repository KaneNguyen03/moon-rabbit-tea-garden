using VuonTra.Core;

namespace VuonTra.Game.Platform
{
    /// <summary>
    /// Plugs the platform's own account and cloud into the runner (#39): Game Center and the iCloud key-value store
    /// on an iPhone or iPad, and nothing anywhere else, so the Mac build, the editor and the tests keep the
    /// local-only services exactly as before.
    /// </summary>
    public static class PlatformServices
    {
        /// <summary>How often the iCloud store is asked whether another device wrote to it.</summary>
        public const float ExternalPollSeconds = 5f;

        /// <summary>True in the built game on an iPhone or iPad; never in the editor.</summary>
        public static bool IsIOSDevice =>
#if UNITY_IOS && !UNITY_EDITOR
            true;
#else
            false;
#endif

        /// <summary>Sets the runner's account and cloud for the platform; a no-op where there is nothing to plug in.</summary>
        public static void Install(GameRunner runner)
        {
#if UNITY_IOS && !UNITY_EDITOR
            var cloud = new ICloudKvsCloudSave();
            cloud.Start();
            runner.Cloud = cloud;
            GameCenterAccount account = GameCenterAccount.Device();
            // Whenever the sign-in changes, a garden with no name yet takes the Game Center name.
            account.Changed += () => runner.AdoptLinkedName();
            runner.Account = account;
            account.Open();
#endif
        }

        /// <summary>Once a frame from the runner: on iOS, a write from another device starts a sync.</summary>
        public static void Tick(GameRunner runner, float dt)
        {
#if UNITY_IOS && !UNITY_EDITOR
            _sincePoll += dt;
            if (_sincePoll < ExternalPollSeconds) return;
            _sincePoll = 0f;
            if (runner.Cloud is ICloudKvsCloudSave kvs && kvs.TakeExternalChange()) runner.SyncNow(SyncReason.Remote);
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        static float _sincePoll;
#endif
    }
}
