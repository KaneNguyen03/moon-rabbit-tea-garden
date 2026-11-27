using System.Collections.Generic;
using NUnit.Framework;
using VuonTra.Release;

namespace VuonTra.Core.Tests
{
    public class AndroidSigningTests
    {
        static Dictionary<string, string> Complete() => new Dictionary<string, string>
        {
            { AndroidSigning.KeystoreVariable, "keys/upload.keystore" },
            { AndroidSigning.KeystorePasswordVariable, "store-secret" },
            { AndroidSigning.KeyAliasVariable, "upload" },
            { AndroidSigning.KeyPasswordVariable, "key-secret" }
        };

        static AndroidSigning Read(Dictionary<string, string> environment, bool keystoreOnDisk, out string problem) =>
            AndroidSigning.Read(name => environment.TryGetValue(name, out string value) ? value : null, path => keystoreOnDisk, out problem);

        [Test]
        public void A_Complete_Environment_Gives_The_Keystore_The_Alias_And_Both_Passwords()
        {
            AndroidSigning signing = Read(Complete(), true, out string problem);

            Assert.IsNull(problem);
            Assert.AreEqual("keys/upload.keystore", signing.Keystore);
            Assert.AreEqual("store-secret", signing.KeystorePassword);
            Assert.AreEqual("upload", signing.KeyAlias);
            Assert.AreEqual("key-secret", signing.KeyPassword);
        }

        [Test]
        public void An_Empty_Environment_Stops_The_Build_And_Names_All_Four_Variables()
        {
            AndroidSigning signing = Read(new Dictionary<string, string>(), true, out string problem);

            Assert.IsNull(signing);
            foreach (string variable in AndroidSigning.Variables) StringAssert.Contains(variable, problem);
            Assert.AreEqual(4, AndroidSigning.Variables.Length);
        }

        [Test]
        public void One_Missing_Or_Blank_Variable_Is_The_Only_One_Named()
        {
            foreach (string absent in AndroidSigning.Variables)
            {
                foreach (string blank in new[] { null, "", "   " })
                {
                    Dictionary<string, string> environment = Complete();
                    if (blank == null) environment.Remove(absent); else environment[absent] = blank;

                    AndroidSigning signing = Read(environment, true, out string problem);

                    Assert.IsNull(signing, absent);
                    foreach (string variable in AndroidSigning.Variables)
                    {
                        bool named = problem.Contains(variable + ",") || problem.Contains(variable + ".");
                        Assert.AreEqual(variable == absent, named, absent + " absent, " + variable + " in: " + problem);
                    }
                }
            }
        }

        [Test]
        public void A_Keystore_Path_With_No_File_Stops_The_Build_And_Names_Its_Variable()
        {
            AndroidSigning signing = Read(Complete(), false, out string problem);

            Assert.IsNull(signing);
            StringAssert.Contains(AndroidSigning.KeystoreVariable, problem);
        }

        [Test]
        public void No_Message_Repeats_A_Value_From_The_Environment()
        {
            Dictionary<string, string> withoutAlias = Complete();
            withoutAlias.Remove(AndroidSigning.KeyAliasVariable);
            Read(withoutAlias, true, out string missing);
            Read(Complete(), false, out string noFile);

            foreach (string message in new[] { missing, noFile })
                foreach (string value in Complete().Values)
                    StringAssert.DoesNotContain(value, message);
        }

        [Test]
        public void Passwords_Keep_Their_Spaces_And_The_Path_And_Alias_Lose_Theirs()
        {
            Dictionary<string, string> environment = Complete();
            environment[AndroidSigning.KeystoreVariable] = " keys/upload.keystore ";
            environment[AndroidSigning.KeyAliasVariable] = " upload ";
            environment[AndroidSigning.KeystorePasswordVariable] = " store secret ";

            AndroidSigning signing = Read(environment, true, out _);

            Assert.AreEqual("keys/upload.keystore", signing.Keystore);
            Assert.AreEqual("upload", signing.KeyAlias);
            Assert.AreEqual(" store secret ", signing.KeystorePassword);
        }
    }
}
