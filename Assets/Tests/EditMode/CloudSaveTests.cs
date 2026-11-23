using System.Collections.Generic;
using NUnit.Framework;
using VuonTra.Core;

namespace VuonTra.Core.Tests
{
    public class CloudSaveTests
    {
        IReadOnlyList<StationDef> defs;
        GameState local;
        MemoryCloudSave cloud;
        long now;
        CloudSync sync;
        readonly List<string> replaced = new List<string>();

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            local = GameState.NewGame(defs);
            local.Profile.Code = "THO-7K3M9Q";
            local.LastSeenUnixSeconds = 1000;
            cloud = new MemoryCloudSave();
            now = 5000;
            sync = new CloudSync(cloud, () => now);
            replaced.Clear();
        }

        string Save(GameState s) => SaveCodec.Encode(defs, s);
        SaveSummary Sum(GameState s) => SaveSummary.Of(s);
        bool Replace(string json) { replaced.Add(json); return SaveCodec.Decode(defs, json).Status == LoadStatus.Ok; }
        SyncReport Run(SyncReason reason = SyncReason.Open) { SyncReport r = null; sync.Sync(reason, Save(local), Sum(local), x => r = x); return r; }

        GameState Ahead()
        {
            var s = GameState.NewGame(defs);
            s.Profile.Code = "THO-7K3M9Q";
            s.GardenXp = Garden.XpForLevel(4);
            s.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Name = "A" });
            s.Rabbits.Add(new Rabbit { Id = 2, Tier = 0, Name = "B" });
            s.LastSeenUnixSeconds = 4000;
            return s;
        }

        [Test]
        public void An_Envelope_Carries_The_Save_Its_Version_The_Summary_And_A_Check_That_Round_Trips()
        {
            local.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Name = "A" });
            local.PlaySeconds = 321.5;
            Rabbits.See(local, "tho-bong");
            string text = CloudSave.Wrap(Save(local), Sum(local), 777);
            Assert.AreEqual(EnvelopeStatus.Ok, CloudSave.Open(text, out CloudEnvelope env));
            Assert.AreEqual(SaveCodec.CurrentVersion, env.Version);
            Assert.AreEqual(777, env.SavedAt);
            Assert.AreEqual(1, env.Summary.Rabbits);
            Assert.AreEqual(1, env.Summary.Kinds);
            Assert.AreEqual(321.5, env.Summary.PlaySeconds, 1e-9);
            Assert.AreEqual(1000, env.Summary.LastSeenUnixSeconds);
            Assert.AreEqual("THO-7K3M9Q", env.Summary.Code);
            LoadResult back = SaveCodec.Decode(defs, env.Save);
            Assert.AreEqual(LoadStatus.Ok, back.Status);
            Assert.AreEqual(1, back.State.Rabbits.Count);
            Assert.AreEqual(321.5, back.State.PlaySeconds, 1e-9);
        }

        [Test]
        public void A_Copy_That_Was_Edited_Truncated_Or_Is_Not_An_Envelope_Is_Corrupt()
        {
            string text = CloudSave.Wrap(Save(local), Sum(local), 777);
            Assert.AreEqual(EnvelopeStatus.Corrupt, CloudSave.Open(text.Substring(0, text.Length - 20), out _), "truncated");
            Assert.AreEqual(EnvelopeStatus.Corrupt, CloudSave.Open(text.Replace("\"savedAt\":777", "\"savedAt\":778"), out _), "edited");
            Assert.AreEqual(EnvelopeStatus.Corrupt, CloudSave.Open("not json", out _));
            Assert.AreEqual(EnvelopeStatus.Corrupt, CloudSave.Open("", out _));
            Assert.AreEqual(EnvelopeStatus.Corrupt, CloudSave.Open(Save(local), out _), "a bare save is not an envelope");
            Assert.AreEqual(EnvelopeStatus.Corrupt, CloudSave.Open("{\"cloud\":1,\"version\":1,\"savedAt\":1,\"save\":\"{}\"}", out _), "no check");
        }

        [Test]
        public void A_Copy_From_A_Newer_Build_Is_Recognised_And_Left_Alone()
        {
            string text = CloudSave.Wrap(Save(local), Sum(local), 777).Replace("\"version\":" + SaveCodec.CurrentVersion, "\"version\":" + (SaveCodec.CurrentVersion + 1));
            CloudSave.Open(text, out CloudEnvelope env);
            Assert.AreEqual(EnvelopeStatus.Corrupt, CloudSave.Open(text, out _), "a bumped version without its check is just corrupt");
            string signed = "{\"cloud\":1,\"version\":" + (SaveCodec.CurrentVersion + 1) + ",\"savedAt\":777,\"check\":\"" + CloudSave.Check(Save(local), SaveCodec.CurrentVersion + 1, 777) + "\",\"save\":" + SaveCodec.Quote(Save(local)) + "}";
            Assert.AreEqual(EnvelopeStatus.NewerVersion, CloudSave.Open(signed, out _));
            cloud.Text = signed;
            SyncReport r = Run();
            Assert.AreEqual(SyncOutcome.NewerVersion, r.Outcome);
            Assert.AreEqual(signed, cloud.Text, "nothing sent up over a newer build's save");
        }

        [Test]
        public void An_Old_Save_In_The_Cloud_Still_Opens_And_Loads()
        {
            string old = "{\"version\":11,\"stations\":[],\"profile\":{\"name\":\"A\",\"avatar\":0,\"code\":\"THO-7K3M9Q\"}}";
            string text = "{\"cloud\":1,\"version\":11,\"savedAt\":10,\"level\":1,\"rabbits\":0,\"kinds\":0,\"played\":0,\"seen\":10,\"code\":\"THO-7K3M9Q\",\"check\":\"" + CloudSave.Check(old, 11, 10) + "\",\"save\":" + SaveCodec.Quote(old) + "}";
            Assert.AreEqual(EnvelopeStatus.Ok, CloudSave.Open(text, out CloudEnvelope env));
            Assert.AreEqual(LoadStatus.Ok, SaveCodec.Decode(defs, env.Save).Status);
        }

        [Test]
        public void With_Nothing_In_The_Cloud_The_Device_Copy_Goes_Up()
        {
            SyncReport r = Run();
            Assert.AreEqual(SyncOutcome.Pushed, r.Outcome);
            Assert.AreEqual(5000, sync.LastPushedAt);
            Assert.AreEqual(EnvelopeStatus.Ok, CloudSave.Open(cloud.Text, out CloudEnvelope env));
            Assert.AreEqual(LoadStatus.Ok, SaveCodec.Decode(defs, env.Save).Status);
            Assert.IsEmpty(replaced, "the device's copy is never touched by a push");
        }

        [Test]
        public void The_Same_Garden_Up_There_Means_Nothing_To_Do()
        {
            Run();
            now = 5100;
            SyncReport r = Run(SyncReason.Background);
            Assert.AreEqual(SyncOutcome.Same, r.Outcome);
            Assert.AreEqual(1, cloud.Pushes);
        }

        [Test]
        public void When_The_Device_Is_Ahead_It_Goes_Up_Without_Asking()
        {
            cloud.Text = CloudSave.Wrap(Save(local), Sum(local), 100);
            local.GardenXp = Garden.XpForLevel(3);
            local.LastSeenUnixSeconds = 2000;
            SyncReport r = Run();
            Assert.AreEqual(SyncOutcome.Pushed, r.Outcome);
            Assert.IsNull(sync.Pending);
        }

        [Test]
        public void When_The_Cloud_Is_Ahead_The_Player_Is_Asked_And_The_Cloud_Copy_Is_Suggested()
        {
            GameState ahead = Ahead();
            cloud.Text = CloudSave.Wrap(Save(ahead), Sum(ahead), 4000);
            SyncReport r = Run();
            Assert.AreEqual(SyncOutcome.NeedsChoice, r.Outcome);
            Assert.IsTrue(r.SuggestRemote);
            Assert.AreEqual(4, r.Remote.GardenLevel);
            Assert.AreEqual(2, r.Remote.Rabbits);
            Assert.AreSame(r, sync.Pending);
            Assert.IsEmpty(replaced, "nothing is replaced until the player says so");
            Assert.AreEqual(0, cloud.Pushes, "and nothing is sent up either");

            Assert.AreSame(r, Run(SyncReason.Timer));
        }

        [Test]
        public void When_Each_Copy_Is_Ahead_In_Something_The_Player_Is_Asked_With_A_Suggestion_By_Level_Then_Rabbits_Then_Time()
        {
            GameState other = Ahead();
            local.GardenXp = Garden.XpForLevel(6);
            cloud.Text = CloudSave.Wrap(Save(other), Sum(other), 4000);
            SyncReport r = Run();
            Assert.AreEqual(SyncOutcome.NeedsChoice, r.Outcome);
            Assert.IsFalse(r.SuggestRemote, "the higher garden level is put forward");
            var a = new SaveSummary { GardenLevel = 3, Rabbits = 2, LastSeenUnixSeconds = 10 };
            var b = new SaveSummary { GardenLevel = 3, Rabbits = 5, LastSeenUnixSeconds = 5 };
            Assert.IsTrue(CloudSync.Suggest(a, b), "same level: more rabbits");
            b.Rabbits = 2; b.LastSeenUnixSeconds = 20;
            Assert.IsTrue(CloudSync.Suggest(a, b), "same level and rabbits: seen later");
            Assert.IsFalse(CloudSync.Suggest(b, a));
        }

        [Test]
        public void Taking_The_Cloud_Copy_Goes_Through_Replace_And_Keeping_The_Device_Copy_Sends_It_Up()
        {
            GameState ahead = Ahead();
            cloud.Text = CloudSave.Wrap(Save(ahead), Sum(ahead), 4000);
            Run();
            SyncReport done = null;
            sync.Resolve(true, Save(local), Replace, r => done = r);
            Assert.AreEqual(SyncOutcome.TookRemote, done.Outcome);
            Assert.AreEqual(1, replaced.Count);
            Assert.AreEqual(2, SaveCodec.Decode(defs, replaced[0]).State.Rabbits.Count);
            Assert.IsNull(sync.Pending);

            cloud.Text = CloudSave.Wrap(Save(ahead), Sum(ahead), 4000);
            Run();
            Assert.IsNotNull(sync.Pending);
            sync.Resolve(false, Save(local), Replace, r => done = r);
            Assert.AreEqual(SyncOutcome.Pushed, done.Outcome);
            Assert.AreEqual(1, replaced.Count, "keeping the device's copy replaces nothing");
            CloudSave.Open(cloud.Text, out CloudEnvelope env);
            Assert.AreEqual(0, env.Summary.Rabbits, "the device's copy is what is up there now");
        }

        [Test]
        public void A_Corrupt_Cloud_Copy_Is_Ignored_Reported_And_Replaced_By_The_Device_Copy()
        {
            cloud.Text = "{\"cloud\":1,\"version\":14,\"savedAt\":1,\"check\":\"nope\",\"save\":\"{}\"}";
            SyncReport r = Run();
            Assert.AreEqual(SyncOutcome.Pushed, r.Outcome);
            Assert.IsTrue(r.RemoteWasCorrupt);
            Assert.IsEmpty(replaced);
            Assert.AreEqual(EnvelopeStatus.Ok, CloudSave.Open(cloud.Text, out _), "healed");
        }

        [Test]
        public void Losing_The_Network_Leaves_The_Device_Copy_Alone_And_Reports_Offline()
        {
            cloud.Online = false;
            SyncReport r = Run();
            Assert.AreEqual(SyncOutcome.Offline, r.Outcome);
            Assert.AreEqual(-1, sync.LastPushedAt);
            Assert.IsEmpty(replaced);
            cloud.Online = true;
            Assert.AreEqual(SyncOutcome.Pushed, Run(SyncReason.Background).Outcome, "and the next attempt gets through");
        }

        [Test]
        public void Timer_Syncs_Are_Rationed_But_Every_Other_Reason_Goes_At_Once()
        {
            Assert.AreEqual(SyncOutcome.Pushed, Run(SyncReason.Timer).Outcome);
            now += 60;
            Assert.AreEqual(SyncOutcome.Skipped, Run(SyncReason.Timer).Outcome);
            Assert.AreEqual(SyncOutcome.Same, Run(SyncReason.Milestone).Outcome, "a milestone is not rationed");
            now += CloudSync.MinSecondsBetweenTimerSyncs;
            Assert.AreEqual(SyncOutcome.Same, Run(SyncReason.Timer).Outcome);
        }

        [Test]
        public void With_No_Cloud_Behind_The_Build_Every_Attempt_Says_So()
        {
            var none = new CloudSync(new LocalOnlyCloudSave(), () => now);
            SyncReport r = null;
            none.Sync(SyncReason.Open, Save(local), Sum(local), x => r = x);
            Assert.AreEqual(SyncOutcome.Unavailable, r.Outcome);
            none.PushNow(Save(local), Sum(local), x => r = x);
            Assert.AreEqual(SyncOutcome.Unavailable, r.Outcome);
            none.Restore(Sum(local), Replace, x => r = x);
            Assert.AreEqual(SyncOutcome.Unavailable, r.Outcome);
            Assert.IsEmpty(replaced);
        }

        [Test]
        public void Save_Now_Pushes_Whatever_Is_There_And_Restore_Takes_Whatever_Is_There()
        {
            GameState ahead = Ahead();
            cloud.Text = CloudSave.Wrap(Save(ahead), Sum(ahead), 4000);
            SyncReport r = null;
            sync.PushNow(Save(local), Sum(local), x => r = x);
            Assert.AreEqual(SyncOutcome.Pushed, r.Outcome);
            CloudSave.Open(cloud.Text, out CloudEnvelope env);
            Assert.AreEqual(0, env.Summary.Rabbits, "the device's copy went up over the cloud's");

            cloud.Text = CloudSave.Wrap(Save(ahead), Sum(ahead), 4000);
            sync.Restore(Sum(local), Replace, x => r = x);
            Assert.AreEqual(SyncOutcome.TookRemote, r.Outcome);
            Assert.AreEqual(2, SaveCodec.Decode(defs, replaced[replaced.Count - 1]).State.Rabbits.Count);

            cloud.Text = null;
            sync.Restore(Sum(local), Replace, x => r = x);
            Assert.AreEqual(SyncOutcome.Nothing, r.Outcome);
            cloud.Text = "garbage";
            sync.Restore(Sum(local), Replace, x => r = x);
            Assert.AreEqual(SyncOutcome.Corrupt, r.Outcome);
            Assert.AreEqual(1, replaced.Count, "a corrupt copy never replaces the device's");
        }

        [Test]
        public void Play_Time_Is_Saved_And_An_Old_Save_Has_None()
        {
            local.PlaySeconds = 12.25;
            GameState back = SaveCodec.Decode(defs, Save(local)).State;
            Assert.AreEqual(12.25, back.PlaySeconds, 1e-9);
            GameState old = SaveCodec.Decode(defs, "{\"version\":13,\"stations\":[]}").State;
            Assert.AreEqual(0d, old.PlaySeconds);
            Assert.AreEqual(0, SaveSummary.Of(old).Kinds);
        }

        [Test]
        public void A_Sync_Asked_For_While_One_Is_Waiting_On_The_Cloud_Is_Skipped()
        {
            cloud.Hold = true;
            SyncReport first = null, second = null;
            sync.Sync(SyncReason.Open, Save(local), Sum(local), r => first = r);
            Assert.IsTrue(sync.InFlight);
            Assert.IsNull(first, "still waiting on the cloud");
            sync.Sync(SyncReason.Milestone, Save(local), Sum(local), r => second = r);
            Assert.IsNotNull(second);
            Assert.AreEqual(SyncOutcome.Skipped, second.Outcome, "not run on top of the first");
            Assert.AreEqual(1, cloud.Pulls);
            cloud.Hold = false;
            cloud.Release();
            Assert.IsNotNull(first);
            Assert.AreEqual(SyncOutcome.Pushed, first.Outcome);
            Assert.IsFalse(sync.InFlight);
            Assert.AreEqual(SyncOutcome.Same, Run(SyncReason.Manual).Outcome, "and the next one runs");
        }
    }
}
