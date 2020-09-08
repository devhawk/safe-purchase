using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Neo;
using Neo.Wallets;

namespace SafePuchaseWeb.Models
{

    public class CreateSaleViewModel : IValidatableObject
    {
        [Required]
        public string Description { get; set; } = string.Empty;
        [Required]
        [Display(Name = "Seller Address")]
        public string SellerAddress { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public Guid SaleId { get; set; } = Guid.NewGuid();

        public UInt160 SellerScriptHash => SellerAddress.ToScriptHash();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Price <= 0)
            {
                yield return new ValidationResult($"Price must be above zero");
            }

            if (!TryToScriptHash(SellerAddress))
            {
                yield return new ValidationResult("SellerAddress must be a valid Neo address");
            }
        }

        private static bool TryToScriptHash(string address)
        {
            try
            {
                _ = address.ToScriptHash();
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
