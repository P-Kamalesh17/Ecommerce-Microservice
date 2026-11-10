namespace OrderAPI.Model.DTO
{
    // Local copy of the field OrderAPI needs from AuthAPI's GET api/authentication/me.
    public class UserProfileDto
    {
        public string? Address { get; set; }
    }

    // Local copy of the fields OrderAPI needs from PaymentAPI's PaymentDto.
    public class PaymentSummaryDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
