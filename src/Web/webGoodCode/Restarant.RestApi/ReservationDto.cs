namespace webGoodCode.Restarant.RestApi
{
    public class ReservationDto
    {
        public string? Id { get; set; }
        public string? At { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public int Quantity { get; set; }
        internal Guid? ParseId()
        {
            if (Guid.TryParse(Id, out var id))
                return id;
            return null;
        }

    }
}
