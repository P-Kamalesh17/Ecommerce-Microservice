using System.ComponentModel.DataAnnotations;

namespace PaymentAPI.Model.DTO
{
    public class FailPaymentDto
    {
        [Range(1, int.MaxValue)]
        public int OrderId { get; set; }
    }
}
