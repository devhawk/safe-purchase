using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Neo;
using Neo.Wallets;

namespace SafePuchaseWeb
{
    public class NeoExpress
    {
        [JsonPropertyName("magic")]
        public uint Magic { get; set; }

        [JsonPropertyName("wallets")]
        public Wallet[] Wallets { get; set; } = Array.Empty<Wallet>();

        public Wallet GetWallet(string name)
        {
            return Wallets.First(w => w.Name == name);
        }

        public class Wallet
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("accounts")]
            public Account[] Accounts { get; set; } = Array.Empty<Account>();

            [JsonIgnore]
            public Account Default => Accounts.First(a => a.IsDefault);
        }
        
        public class Account
        {
            [JsonPropertyName("private-key")]
            public string PrivateKey { get; set; } = string.Empty;

            [JsonPropertyName("script-hash")]
            public string ScriptHashString { get; set; } = string.Empty;

            [JsonPropertyName("is-default")]
            public bool IsDefault { get; set; }

            [JsonIgnore]
            public UInt160 ScriptHash => ScriptHashString.ToScriptHash();

            [JsonIgnore]
            public KeyPair KeyPair => new KeyPair(PrivateKey.HexToBytes());
        }

        public static NeoExpress Load(string filename)
        {
            var json = File.ReadAllText(filename);
            return JsonSerializer.Deserialize<NeoExpress>(json);
        }
    }
}
