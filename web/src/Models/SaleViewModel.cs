using System;
using System.Numerics;
using Neo;
using Neo.SmartContract.Native;
using Neo.VM.Types;
using NeoArray = Neo.VM.Types.Array;

namespace SafePuchaseWeb.Models
{
    public class SaleViewModel
    {
        public Guid SaleId { get; set; }
        public UInt160 Seller { get; set; } = UInt160.Zero;
        public UInt160? Buyer { get; set; }
        public string Description { get; set; } = string.Empty;
        public BigDecimal Price;
        public SaleState State;

        public static SaleViewModel FromStackItem(NeoArray item)
        {
            if (item.Count == 6)
            {
                var saleId = item[0] is ByteString a ? new Guid(a.GetSpan()) : throw new InvalidCastException();
                var seller = item[1] is ByteString b ? new UInt160(b.GetSpan()) : throw new InvalidCastException();
                var buyer = item[2] is ByteString c ? new UInt160(c.GetSpan()) : null;
                var description = item[3] is ByteString d ? d.GetString() : throw new InvalidCastException();
                var price = item[4] is Integer e ? e.GetInteger() : throw new InvalidCastException();
                var state = item[5] is Integer f ? (SaleState)(byte)f.GetInteger() : throw new InvalidCastException();

                return new SaleViewModel()
                {
                    SaleId = saleId,
                    Seller = seller,
                    Buyer = buyer,
                    Description = description,
                    Price = new BigDecimal(price, NativeContract.GAS.Decimals),
                    State = state,
                };
            }

            throw new InvalidCastException();
        }


        public enum SaleState : byte
        {
            New,
            AwaitingShipment,
            ShipmentConfirmed,
        }
    }
}
