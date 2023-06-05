using Bogus;
using Bogus.Extensions.Brazil;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Kafka.Sample.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

var adminClientConfig = configuration.GetSection("AdminClient").Get<AdminClientConfig>().ThrowIfContainsNonUserConfigurable();
var producerConfig = configuration.GetSection("Producer").Get<ProducerConfig>().ThrowIfContainsNonUserConfigurable();

var adminClientBuilder = new AdminClientBuilder(adminClientConfig)
    .SetErrorHandler((_, e) => Console.WriteLine($"Admin Error: {e.Reason}"));

producerConfig.EnableIdempotence = true;
producerConfig.Acks = Acks.All;
producerConfig.MessageSendMaxRetries = 3;
producerConfig.RetryBackoffMs = 1000;

var producerBuilder1 = new ProducerBuilder<string, Customer>(producerConfig)
    .SetKeySerializer(Serializers.Utf8)
    .SetValueSerializer(new GenericJsonSerializer<Customer>())
    .SetLogHandler((_, message) => Console.WriteLine($"Facility: {message.Facility} - {message.Level} Message: {message.Message}"))
    .SetErrorHandler((_, e) => Console.WriteLine($"Producer Error: {e.Reason}"));

var producerBuilder2 = new ProducerBuilder<string, Product>(producerConfig)
    .SetKeySerializer(Serializers.Utf8)
    .SetValueSerializer(new GenericJsonSerializer<Product>())
    .SetLogHandler((_, message) => Console.WriteLine($"Facility: {message.Facility} - {message.Level} Message: {message.Message}"))
    .SetErrorHandler((_, e) => Console.WriteLine($"Producer Error: {e.Reason}"));

var producerBuilder3 = new ProducerBuilder<string, Order>(producerConfig)
    .SetKeySerializer(Serializers.Utf8)
    .SetValueSerializer(new GenericJsonSerializer<Order>())
    .SetLogHandler((_, message) => Console.WriteLine($"Facility: {message.Facility} - {message.Level} Message: {message.Message}"))
    .SetErrorHandler((_, e) => Console.WriteLine($"Producer Error: {e.Reason}"));

var container = new ServiceCollection()
    .AddSingleton(adminClientBuilder.Build())
    .AddSingleton(producerBuilder1.Build())
    .AddSingleton(producerBuilder2.Build())
    .AddSingleton(producerBuilder3.Build())
    .BuildServiceProvider();

var admin = container.GetService<IAdminClient>();

try
{
    await admin.CreateTopicsAsync(new TopicSpecification[]
    {
        new TopicSpecification
        {
            Name = "products",
            ReplicationFactor = 1,
            NumPartitions = 1
        },
        new TopicSpecification
        {
            Name = "customers",
            ReplicationFactor = 1,
            NumPartitions = 1
        },
        new TopicSpecification
        {
            Name = "orders",
            ReplicationFactor = 1,
            NumPartitions = 1
        }
    });

    Console.WriteLine($"Topics created.");
}
catch (Exception)
{
    Console.WriteLine($"Topics already created.");
}

var customerProducer = container.GetService<IProducer<string, Customer>>();
var productProducer = container.GetService<IProducer<string, Product>>();
var orderProducer = container.GetService<IProducer<string, Order>>();

var fakerProduct = new Faker<Product>()
    .RuleFor(r => r.Code, r => r.Commerce.Ean13())
    .RuleFor(r => r.Name, r => r.Commerce.ProductName());

var products = fakerProduct.Generate(5);
var productsCodes = products.Select(s => s.Code).ToList();

foreach (var product in products)
{
    try
    {
        var deliveryReport = await productProducer.ProduceAsync("products", new Message<string, Product>()
        {
            Key = product.Code,
            Value = product
        });

        Console.WriteLine($"Product code: {product.Code}, product name: {product.Name} delivered to: {deliveryReport.TopicPartitionOffset}. Delivery status: {deliveryReport.Status}");
    }
    catch (ProduceException<string, Product> e)
    {
        Console.WriteLine($"failed to deliver message: {e.Message} [{e.Error.Code}]");
    }
}

while (true)
{
    for (int i = 0; i < 10; i++)
    {
        try
        {
            var customer = new Faker<Customer>()
                .RuleFor(r => r.Id, r => r.Person.Cpf())
                .RuleFor(r => r.Name, r => r.Person.FullName)
                .Generate();

            var customerDeliveryReport = await customerProducer.ProduceAsync("customers", new Message<string, Customer>()
            {
                Key = customer.Id,
                Value = customer
            });

            Console.WriteLine($"Customer id: {customer.Id}, customer name: {customer.Name} delivered to: {customerDeliveryReport.TopicPartitionOffset}. Delivery status: {customerDeliveryReport.Status}");

            var order = new Faker<Order>()
                .RuleFor(r => r.Date, r => DateTime.Now)
                .RuleFor(r => r.ProductCode, r => r.PickRandom(productsCodes))
                .RuleFor(r => r.CustomerId, r => customer.Id)
                .RuleFor(r => r.Quantity, r => r.Random.Int(1, 10))
                .Generate();

            var orderDeliveryReport = await orderProducer.ProduceAsync("orders", new Message<string, Order>()
            {
                Key = order.Date.ToString("yyyyMMddHHmmss"),
                Value = order
            });

            Console.WriteLine($"Order date: {order.Date:yyyyy-MM-dd HH:mm:ss}, product code: {order.ProductCode}, customer id: {order.CustomerId} delivered to: {orderDeliveryReport.TopicPartitionOffset}. Delivery status: {orderDeliveryReport.Status}");
        }
        catch (ProduceException<string, Customer> e)
        {
            Console.WriteLine($"failed to deliver message: {e.Message} [{e.Error.Code}]");
        }
        catch (ProduceException<string, Order> e)
        {
            Console.WriteLine($"failed to deliver message: {e.Message} [{e.Error.Code}]");
        }
    }

    await Task.Delay(3000);

    Console.WriteLine($"Waiting 3 seconds... ");
}
