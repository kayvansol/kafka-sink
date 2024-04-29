using AvroSpecific;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

public void Main()
{
    string bootstrapServers = "localhost:29092";
    string schemaRegistryUrl = "localhost:8081";
    string topicName = "usertopic";

    var producerConfig = new ProducerConfig
    {
        BootstrapServers = bootstrapServers
    };

    var schemaRegistryConfig = new SchemaRegistryConfig
    {        
        Url = schemaRegistryUrl
    };


    var avroSerializerConfig = new AvroSerializerConfig
    {
        // optional Avro serializer properties:
        BufferBytes = 100
    };

    CancellationTokenSource cts = new CancellationTokenSource();

    using (var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig))

    using (var producer = new ProducerBuilder<string, User>(producerConfig)
            .SetValueSerializer(new AvroSerializer<User>(schemaRegistry, avroSerializerConfig))
            .Build())
    {

        Console.WriteLine($"{producer.Name} producing on {topicName}. Enter user names, q to exit.");

        int i = 1;

        string text;

        while ((text = Console.ReadLine()) != "q")
        {
            User user = new User { name = text, favorite_color = "green", favorite_number = ++i };

            producer.ProduceAsync(topicName, new Message<string, User> { Key = null, Value = user })
                .ContinueWith(task =>
                {
                    if (!task.IsFaulted)
                    {
                        Console.WriteLine($"produced to: {task.Result.TopicPartitionOffset}");
                        return;
                    }
                    
                    Console.WriteLine($"error producing message: {task.Exception.InnerException}");
                });
        }
    }

    cts.Cancel();

}