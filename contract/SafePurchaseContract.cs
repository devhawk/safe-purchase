using System;
using System.ComponentModel;
using System.Numerics;
using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

// Note, this contract is a port of safe-remote-purchase.py 
//       originally written by Joe Stewart (aka hal0x2328)
//       https://github.com/Splyse/MCT/blob/master/safe-remote-purchase.py
// Ported from python to C# by Harry Pierson (aka DevHawk)

#nullable enable

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

    [ContractPermission("*", "transfer")]
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

        [DisplayName("SaleUpdated")]
        public static event OnSaleUpdatedDelegate OnSaleUpdated = default!;

        [DisplayName("SaleCompleted")]
        public static event OnSaleCompletedDelegate OnSaleCompleted = default!;

        [Safe]
        public static Map<ByteString, Map<string, object>> Sales(UInt160 seller)
        {
            if (seller is null || !seller.IsValid) throw new ArgumentException(nameof(seller));

            StorageMap accountMap = new(Storage.CurrentContext, Prefix_AccountSales);
            StorageMap salesMap = new(Storage.CurrentContext, Prefix_Sales);

            Map<ByteString, Map<string, object>> map = new();
            var iterator = (Iterator<ByteString>)accountMap.Find(seller, FindOptions.KeysOnly | FindOptions.RemovePrefix);
            foreach (var saleId in iterator)
            {
                var saleInfo = (SaleInfo)StdLib.Deserialize(salesMap.Get(saleId));
                Map<string, object> saleMap = new();
                saleMap["seller"] = saleInfo.Seller;
                saleMap["buyer"] = saleInfo.Buyer;
                saleMap["description"] = saleInfo.Description;
                saleMap["price"] = saleInfo.Price;
                saleMap["token"] = saleInfo.Token;
                saleMap["state"] = saleInfo.State;
                map[saleId] = saleMap;
            }
            return map;
        }

        public static void OnNEP17Payment(UInt160 from, BigInteger amount, object[] data)
        {
            if (from == null && Runtime.CallingScriptHash == GAS.Hash)
            {
                // When NEO balance changes, the contract receives GAS from the platform
                // Ignore this payment for purposes of selling/buying items
                return;
            }

            if (from == null) throw new ArgumentNullException(nameof(from));
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length < 2) throw new Exception("Invalid transfer data length");
            var command = (string)data[0];
            if (command == nameof(CreateSale))
            {
                if (data.Length < 3 || data.Length > 4) throw new Exception("Invalid transfer data length");
                var price = (BigInteger)data[1];
                var description = (string)data[2];
                ByteString? saleId = data.Length == 4 ? (ByteString)data[3] : null;
                CreateSale(from, amount, price, description, saleId);
            }
            else if (command == nameof(BuyerDeposit))
            {
                var saleId = (ByteString)data[1];
                BuyerDeposit(from, amount, saleId);
            }
            else
            {
                throw new Exception($"Invalid command {command}");
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

            var saleInfo = new SaleInfo();
            saleInfo.Seller = seller;
            saleInfo.Description = description;
            saleInfo.Price = price;
            saleInfo.Token = token;
            saleInfo.State = SaleState.New;

            StorageMap salesMap = new(Storage.CurrentContext, Prefix_Sales);
            salesMap.Put(saleId, StdLib.Serialize(saleInfo));

            StorageMap accountMap = new(Storage.CurrentContext, Prefix_AccountSales);
            accountMap.Put(seller + saleId, 0);

            OnNewSale(saleId, seller, description, token, price);
        }

        public static void BuyerDeposit(UInt160 buyer, BigInteger amount, ByteString saleId)
        {
            StorageMap salesMap = new(Storage.CurrentContext, Prefix_Sales);

            var serializedSale = salesMap.Get(saleId);
            if (serializedSale == null) throw new Exception("invalid saleId");
            var saleInfo = (SaleInfo)StdLib.Deserialize(serializedSale);
            if (saleInfo.State != SaleState.New) throw new Exception("sale state incorrect");

            var token = Runtime.CallingScriptHash;
            if (saleInfo.Token != token) throw new Exception("Invalid token payment");
            if (amount != saleInfo.Price * 2) throw new Exception("buyer deposit must be 2x price");

            saleInfo.Buyer = buyer;
            saleInfo.State = SaleState.AwaitingShipment;
            salesMap.Put(saleId, StdLib.Serialize(saleInfo));

            OnSaleUpdated(saleId, saleInfo.Buyer, saleInfo.State);
        }        
        
        public static void ConfirmShipment(ByteString saleId)
        {
            StorageMap salesMap = new(Storage.CurrentContext, Prefix_Sales);

            var serializedSale = salesMap.Get(saleId);
            if (serializedSale == null) throw new Exception("invalid saleId");
            var saleInfo = (SaleInfo)StdLib.Deserialize(serializedSale);
            if (saleInfo.State != SaleState.AwaitingShipment) throw new Exception("sale state incorrect");

            if (!Runtime.CheckWitness(saleInfo.Seller)) throw new Exception("only seller can confirm shipment");

            saleInfo.State = SaleState.ShipmentConfirmed;
            salesMap.Put(saleId, StdLib.Serialize(saleInfo));
            OnSaleUpdated(saleId, UInt160.Zero, saleInfo.State);
        }

        public static void ConfirmReceived(ByteString saleId)
        {
            StorageMap salesMap = new(Storage.CurrentContext, Prefix_Sales);

            var serializedSale = salesMap.Get(saleId);
            if (serializedSale == null) throw new Exception("invalid saleId");
            var saleInfo = (SaleInfo)StdLib.Deserialize(serializedSale);
            if (saleInfo.State != SaleState.ShipmentConfirmed) throw new Exception("sale state incorrect");

            if (!Runtime.CheckWitness(saleInfo.Buyer)) throw new Exception("only buyer can confirm receipt");

            Contract.Call(saleInfo.Token, "transfer", CallFlags.All,
                Runtime.ExecutingScriptHash, saleInfo.Buyer, saleInfo.Price, null);
            Contract.Call(saleInfo.Token, "transfer", CallFlags.All,
                Runtime.ExecutingScriptHash, saleInfo.Seller, saleInfo.Price * 3, null);

            salesMap.Delete(saleId);
            StorageMap accountMap = new(Storage.CurrentContext, Prefix_AccountSales);
            accountMap.Delete(saleInfo.Seller + saleId);

            OnSaleCompleted(saleId);
        }
 
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
            var key = new byte[] { Prefix_ContractOwner };
            var contractOwner = (UInt160)Storage.Get(Storage.CurrentContext, key);
            var tx = (Transaction)Runtime.ScriptContainer;
            if (contractOwner.Equals(tx.Sender) && Runtime.CheckWitness(contractOwner))
            {
                ContractManagement.Update(nefFile, manifest, null);
            }
            else
            {
                throw new Exception("Only contract owner can update the contract");
            }
        }
    }
}
