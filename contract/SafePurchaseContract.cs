using System;
using System.ComponentModel;
using System.Numerics;
using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

// Note, this contract is a port of safe-remote-purchase.py 
//       originally written by Joe Stewart (aka hal0x2328)
//       https://github.com/Splyse/MCT/blob/master/safe-remote-purchase.py
// Ported from python to C# by Harry Pierson (aka DevHawk)

namespace NgdEnterprise.Samples
{
    public enum SaleState : byte
    {
        New,
        AwaitingShipment,
        ShipmentConfirmed,
    }

    public class SaleInfo
    {
        public UInt160 Seller = UInt160.Zero;
        public UInt160 Buyer = UInt160.Zero;
        public string Description = string.Empty;
        public UInt160 Token = UInt160.Zero;
        public BigInteger Price;
        public SaleState State;
    }

    // [ManifestName("DevHawk.SafePurchase")]
    // [ManifestExtra("Author", "Harry Pierson")]
    // [ManifestExtra("Email", "hpierson@ngd.neo.org")]
    // [ManifestExtra("Description", "This is an example contract")]
    // [Features(ContractFeatures.HasStorage | ContractFeatures.Payable)]
    public class SafePurchaseContract : SmartContract
    {
        const byte Prefix_Sales = 0x00;
        const byte Prefix_AccountSales = 0x01;
        const byte Prefix_ContractOwner = 0xFF;

        public delegate void OnNewSaleDelegate(ByteString saleId, UInt160 seller, string description, UInt160 token, BigInteger price);
        public delegate void OnSaleUpdatedDelegate(ByteString saleId, UInt160 buyer, SaleState saleState);
        public delegate void OnSaleCompletedDelegate(ByteString saleId);

        [DisplayName("NewSale")]
        public static event OnNewSaleDelegate OnNewSale = default!;

        // [DisplayName("SaleUpdated")]
        // public static event OnSaleUpdatedDelegate OnSaleUpdated = default!;

        // [DisplayName("SaleCompleted")]
        // public static event OnSaleUpdatedDelegate OnSaleCompleted = default!;

        public static void OnNEP17Payment(UInt160 from, BigInteger amount, object[] data)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length < 1) throw new Exception("Invalid transfer data length");
            if ((string)data[0] == "createSale")
            {
                if (data.Length < 3 || data.Length > 4) throw new Exception("Invalid transfer data length");
                var price = (BigInteger)data[1];
                var description = (string)data[2];
                ByteString? saleId = data.Length == 4 ? (ByteString)data[3] : null;
                CreateSale(from, amount, price, description, saleId);
            }
        }

        public static void CreateSale(UInt160 seller, BigInteger amount, BigInteger price, string description, ByteString? saleId)
        {
            if (amount != price * 2) throw new Exception("seller deposit must be 2x price");
            var token = Runtime.CallingScriptHash;

            if (saleId != null)
            {
                if (saleId.Length != 16) throw new Exception("sale ID must be 16 bytes long");
            }
            else
            {
                var saleHash = new List<object>();
                saleHash.Add(seller);
                saleHash.Add(price);
                saleHash.Add(description);
                saleHash.Add(Runtime.GetRandom());
                saleId = CryptoLib.Sha256(StdLib.Serialize(saleHash));
            }

            var saleInfo = new SaleInfo()
            {
                Seller = seller,
                Buyer = UInt160.Zero,
                Description = description,
                Price = price,
                Token = token,
                State = SaleState.New
            };

            StorageMap salesMap = new(Storage.CurrentContext, Prefix_Sales);
            salesMap.Put(saleId, StdLib.Serialize(saleInfo));

            StorageMap accountMap = new(Storage.CurrentContext, Prefix_AccountSales);
            accountMap.Put(seller + saleId, 0);

            OnNewSale(saleId, seller, description, token, price);
        }

        public static void BuyerDeposit(ByteString saleId)
        {
        //     var saleInfo = GetSale(saleId);
        //     if (saleInfo == null) throw new Exception("could not find sale");
        //     if (saleInfo.State != SaleState.New) throw new Exception("sale state incorrect");

        //     var notifications = Runtime.GetNotifications();
        //     if (notifications.Length == 0) throw new Exception("Contribution transaction not found.");

        //     BigInteger gas = 0;
        //     for (int i = 0; i < notifications.Length; i++)
        //     {
        //         gas += GetTransactionAmount(notifications[i], GAS.Hash);
        //     }

        //     if (gas != saleInfo.Price * 2) throw new Exception("buyer deposit must be 2x price");

        //     var tx = (Transaction)ExecutionEngine.ScriptContainer;
        //     saleInfo.Buyer = tx.Sender;
        //     saleInfo.State = SaleState.AwaitingShipment;

        //     SaveSale(saleInfo);
        //     OnSaleUpdated(saleInfo.Id, saleInfo.Buyer, (byte)saleInfo.State);
        //     return true;
        }        
        
        public static void ConfirmShipment(ByteString saleId)
        {
        //     var saleInfo = GetSale(saleId);
        //     if (saleInfo == null) throw new Exception("could not find sale");
        //     if (saleInfo.State != SaleState.AwaitingShipment) throw new Exception("sale state incorrect");

        //     if (saleInfo.Buyer == null) throw new Exception("buyer not specified");

        //     if (!Runtime.CheckWitness(saleInfo.Seller)) throw new Exception("must be seller to confirm shipment");

        //     saleInfo.State = SaleState.ShipmentConfirmed;

        //     SaveSale(saleInfo);
        //     OnSaleUpdated(saleInfo.Id, null, (byte)saleInfo.State);
        //     return true;
        }

        public static void ConfirmReceived(ByteString saleId)
        {
        //     var saleInfo = GetSale(saleId);
        //     if (saleInfo == null) throw new Exception("could not find sale");
        //     if (saleInfo.State != SaleState.ShipmentConfirmed) throw new Exception("sale state incorrect");

        //     if (!Runtime.CheckWitness(saleInfo.Buyer)) throw new Exception("must be buyer to confirm receipt");

        //     GAS.Transfer(ExecutionEngine.ExecutingScriptHash, saleInfo.Buyer, saleInfo.Price, null);
        //     GAS.Transfer(ExecutionEngine.ExecutingScriptHash, saleInfo.Seller, saleInfo.Price * 3, null);

        //     DeleteSale(saleInfo);
        //     OnSaleCompleted(saleInfo.Id);
        //     return true;
        }
        
        // private static SaleInfo GetSale(byte[] saleId)
        // {
        //     if (saleId.Length != 16)
        //     {
        //         throw new ArgumentException("The saleId parameter MUST be 16 bytes long.", nameof(saleId));
        //     }

        //     var salesMap = Storage.CurrentContext.CreateMap(SALES_MAP_NAME);
        //     var result = salesMap.Get(saleId);
        //     if (result == null)
        //     {
        //         return null;
        //     }

        //     var zzz = result.Deserialize();
        //     return zzz as SaleInfo;
        // }

        // private static void SaveSale(SaleInfo saleInfo)
        // {
        //     var salesMap = Storage.CurrentContext.CreateMap(SALES_MAP_NAME);
        //     salesMap.Put(saleInfo.Id, saleInfo.Serialize());
        // }

        // private static void DeleteSale(SaleInfo saleInfo)
        // {
        //     var salesMap = Storage.CurrentContext.CreateMap(SALES_MAP_NAME);
        //     salesMap.Delete(saleInfo.Id);
        // }


        // // private static BigInteger GetTransactionAmount(Notification notification, UInt160 scriptHash)
        // // {
        // //     if (notification.ScriptHash != scriptHash) return 0;
        // //     // Only allow Transfer notifications
        // //     if (notification.EventName != "Transfer") return 0;
        // //     var state = notification.State;
        // //     // Checks notification format
        // //     if (state.Length != 3) return 0;
        // //     // Check dest
        // //     if ((UInt160)state[1] != ExecutionEngine.ExecutingScriptHash) return 0;
        // //     // Amount
        // //     var amount = (BigInteger)state[2];
        // //     if (amount < 0) return 0;
        // //     return amount;
        // // }     

        // public static bool Update(byte[] script, string manifest)
        // {
        //     if (!IsOwner()) throw new Exception("No authorization.");
        //     // Check empty
        //     if (script.Length == 0 && manifest.Length == 0) return false;
        //     Contract.Update(script, manifest);
        //     return true;
        // }

        // public static bool Destroy()
        // {
        //     if (!IsOwner()) throw new Exception("No authorization.");
        //     Contract.Destroy();
        //     return true;
        // }

        // private static bool IsOwner() => Runtime.CheckWitness(Owner);

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (update) return;

            var tx = (Transaction)Runtime.ScriptContainer;
            var key = new byte[] { Prefix_ContractOwner };
            Storage.Put(Storage.CurrentContext, key, tx.Sender);
        }

        public static void Update(ByteString nefFile, string manifest)
        {
            if (!ValidateContractOwner()) throw new Exception("Only the contract owner can update the contract");

            ContractManagement.Update(nefFile, manifest, null);
        }

        static bool ValidateContractOwner()
        {
            var key = new byte[] { Prefix_ContractOwner };
            var contractOwner = (UInt160)Storage.Get(Storage.CurrentContext, key);
            var tx = (Transaction)Runtime.ScriptContainer;
            return contractOwner.Equals(tx.Sender) && Runtime.CheckWitness(contractOwner);
        }
    }
}
