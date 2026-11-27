using System;
using System.Collections.Generic;

namespace VuonTra.Release
{
    public sealed class AndroidSigning
    {
        public const string KeystoreVariable = "VUONTRA_ANDROID_KEYSTORE";
        public const string KeystorePasswordVariable = "VUONTRA_ANDROID_KEYSTORE_PASSWORD";
        public const string KeyAliasVariable = "VUONTRA_ANDROID_KEY_ALIAS";
        public const string KeyPasswordVariable = "VUONTRA_ANDROID_KEY_PASSWORD";

        public static readonly string[] Variables =
        {
            KeystoreVariable, KeystorePasswordVariable, KeyAliasVariable, KeyPasswordVariable
        };

        public readonly string Keystore;
        public readonly string KeystorePassword;
        public readonly string KeyAlias;
        public readonly string KeyPassword;

        AndroidSigning(string keystore, string keystorePassword, string keyAlias, string keyPassword)
        {
            Keystore = keystore;
            KeystorePassword = keystorePassword;
            KeyAlias = keyAlias;
            KeyPassword = keyPassword;
        }

        public static AndroidSigning Read(Func<string, string> variable, Func<string, bool> fileExists, out string problem)
        {
            var missing = new List<string>();
            foreach (string name in Variables)
                if (string.IsNullOrWhiteSpace(variable(name))) missing.Add(name);
            if (missing.Count > 0)
            {
                problem = "[VuonTra] Thiếu biến môi trường để ký bản Android phát hành: " + string.Join(", ", missing)
                    + ". Đặt đủ bốn biến rồi chạy lại; bản phát hành không ký bằng khoá debug.";
                return null;
            }
            string keystore = variable(KeystoreVariable).Trim();
            if (!fileExists(keystore))
            {
                problem = "[VuonTra] Thiếu tệp keystore: đường dẫn trong " + KeystoreVariable + " không trỏ tới tệp nào.";
                return null;
            }
            problem = null;
            return new AndroidSigning(keystore, variable(KeystorePasswordVariable), variable(KeyAliasVariable).Trim(), variable(KeyPasswordVariable));
        }
    }
}
