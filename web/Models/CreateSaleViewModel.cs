using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SafePuchaseWeb.Models
{
    public class CreateSaleViewModel : IValidatableObject
    {
        [Required]
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public Guid SaleId { get; set; } = Guid.NewGuid();
        public Neo.UInt256? TransactionHash { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Price <= 0)
            {
                yield return new ValidationResult($"Price must be above zero");
            }
        }
    }
}
