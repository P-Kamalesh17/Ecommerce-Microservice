using System.ComponentModel.DataAnnotations;

namespace OrderAPI.Model.DTO
{
    public class UpdateOrderStatusDto
    {
        [EnumDataType(typeof(OrderStatus))]
        public OrderStatus Status { get; set; }
    }
}
