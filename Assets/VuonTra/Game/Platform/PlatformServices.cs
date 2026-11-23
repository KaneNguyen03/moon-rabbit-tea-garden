using VuonTra.Core;

namespace VuonTra.Game.Platform
{
    public static class PlatformServices
    {
        public const float ExternalPollSeconds = 5f;

        public static bool IsIOSDevice =>
#if UNITY_IOS && !UNITY_EDITOR
            true;
#else
            false;
#endif

        public static void Install(GameRunner runner)
        {
#if UNITY_IOS && !UNITY_EDITOR
            var cloud = new ICloudKvsCloudSave();
            cloud.Start();
            runner.Cloud = cloud;
            GameCenterAccount account = GameCenterAccount.Device();
            account.Changed += () => runner.AdoptLinkedName();
            runner.Account = account;
            account.Open();
#endif
        }

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
