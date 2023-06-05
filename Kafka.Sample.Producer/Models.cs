using System;

namespace Kafka.Sample.Producer
{
    public class Customer
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class Product
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; }
    }

    public class Order
    {
        public DateTime Date { get; set; }
        public string CustomerId { get; set; }
        public string ProductCode { get; set; }
        public int Quantity { get; set; }
    }
}
