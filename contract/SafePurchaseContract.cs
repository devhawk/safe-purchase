using System;
using System.ComponentModel;
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
    public enum SaleState
    {
        New,
        AwaitingShipment,
        ShipmentConfirmed,
    }

    public class SaleInfo
    {
        public byte[] Id;
        public byte[] Seller;
        public byte[] Buyer;
        public string Description;
        public BigInteger Price;
        public SaleState State;
    }

    [ManifestExtra("Author", "Harry Pierson")]
    [ManifestExtra("Email", "hpierson@ngd.neo.org")]
    [ManifestExtra("Description", "This is an example contract")]
    [Features(ContractFeatures.HasStorage | ContractFeatures.Payable)]
    public class SafePurchaseContract : SmartContract
    {
        // Note, using GAS instead of NEO due to transfer issue in preview 3 debugger
        //       using reversed GAS scriptHash string to work around issue in preview 3 HexToBytes 

        static readonly byte[] GasToken = "0xbcaf41d684c7d4ad6ee0d99da9707b9d1f0c8e66".HexToBytes();
        static readonly byte[] Owner = "NWoLj8g5Hr43B3CDkpMKDJFfBV3p6NM732".ToScriptHash();
        const string SALES_MAP_NAME = nameof(SafePurchaseContract);

        [DisplayName("NewSale")]
        public static event Action<byte[], byte[], string, BigInteger> OnNewSale;

        public static bool CreateSale(BigInteger price, string description)
        {
            if (price <= 0) throw new Exception("price must be larger than zero");

            var notifications = Runtime.GetNotifications();
            if (notifications.Length == 0) throw new Exception("Contribution transaction not found.");

            BigInteger gas = 0;
            for (int i = 0; i < notifications.Length; i++)
            {
                gas += GetTransactionAmount(notifications[i], GasToken);
            }

            if (gas != price * 2) throw new Exception("seller deposit must be 2x price");
            
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            var saleInfo = new SaleInfo()
            {
                Id = tx.Hash,
                Seller = tx.Sender,
                Description = description,
                Price = price,
                State = SaleState.New,
            };

            StorageMap salesMap = Storage.CurrentContext.CreateMap(SALES_MAP_NAME);
            salesMap.Put(saleInfo.Id, saleInfo.Serialize());

            OnNewSale(saleInfo.Id, saleInfo.Seller, saleInfo.Description, saleInfo.Price);
            return true;
        }

        private static BigInteger GetTransactionAmount(Notification notification, byte[] scriptHash)
        {
            if (notification.ScriptHash != scriptHash) return 0;
            // Only allow Transfer notifications
            if (notification.EventName != "Transfer") return 0;
            var state = notification.State;
            // Checks notification format
            if (state.Length != 3) return 0;
            // Check dest
            if ((byte[])state[1] != ExecutionEngine.ExecutingScriptHash) return 0;
            // Amount
            var amount = (BigInteger)state[2];
            if (amount < 0) return 0;
            return amount;
        }     

        public static bool Update(byte[] script, string manifest)
        {
            if (!IsOwner()) throw new Exception("No authorization.");
            // Check empty
            if (script.Length == 0 && manifest.Length == 0) return false;
            Contract.Update(script, manifest);
            return true;
        }

        public static bool Destroy()
        {
            if (!IsOwner()) throw new Exception("No authorization.");
            Contract.Destroy();
            return true;
        }

        private static bool IsOwner() => Runtime.CheckWitness(Owner);
    }
}
