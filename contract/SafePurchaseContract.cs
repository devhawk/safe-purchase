using System;
using System.Numerics;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;

// Note, this contract is a port of safe-remote-purchase.py 
//       originally written by Joe Stewart (aka hal0x2328)
//       https://github.com/Splyse/MCT/blob/master/safe-remote-purchase.py
// Ported from python to C# by Harry Pierson (aka DevHawk)

namespace SafePurchaseSample
{
    public class SafePurchaseContract : SmartContract
    {
        // Note, using GAS instead of NEO due to transfer issue in preview 3 debugger
        //       using reversed token string to work around issue in preview 3 HexToBytes 

        static readonly byte[] GasToken = "0xbcaf41d684c7d4ad6ee0d99da9707b9d1f0c8e66".HexToBytes();
        static readonly byte[] Owner = "NWoLj8g5Hr43B3CDkpMKDJFfBV3p6NM732".ToScriptHash();


    }
}
