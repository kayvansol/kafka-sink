# Kafka Sink Connector for SQL Server

![alt text](https://raw.githubusercontent.com/kayvansol/kafka-sink/main/img/logo.jpeg?raw=true)

Docker file for the connector that installing kafka-connect-jdbc library [Dockerfile](https://github.com/kayvansol/kafka-sink/blob/main/Dockerfile):
```bash
FROM docker.arvancloud.ir/confluentinc/cp-kafka-connect:latest

RUN confluent-hub install confluentinc/kafka-connect-jdbc:10.7.6 --no-prompt
```

the build command :
```bash
docker build -t docker.arvancloud.ir/confluentinc/cp-kafka-connect:latest .
```
![alt text](https://raw.githubusercontent.com/kayvansol/kafka-sink/main/img/kafka-connect-with_jdbc.png?raw=true)

```bash
cd kafka-connect
```

Docker Compose file [docker-compose.yml](https://github.com/kayvansol/kafka-sink/blob/main/docker-compose.yml) :
```yml
---
version: '3'

services:

  zookeeper1:
    image: docker.arvancloud.ir/confluentinc/cp-zookeeper:latest
    hostname: zookeeper1
    container_name: zookeeper1
    ports:
      - "2181:2181"
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000
    networks:
      - kafka-network


  kafka:
    image: docker.arvancloud.ir/confluentinc/cp-kafka:latest
    hostname: kafka
    container_name: kafka
    depends_on:
      - zookeeper1
    ports:
      - "29092:29092"
      - "9092:9092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: 'zookeeper1:2181'
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:29092,PLAINTEXT_HOST://localhost:9092
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS: 0
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
      KAFKA_AUTO_CREATE_TOPICS_ENABLE: 'true'
      CONFLUENT_METRICS_REPORTER_BOOTSTRAP_SERVERS: kafka:29092
      CONFLUENT_METRICS_REPORTER_ZOOKEEPER_CONNECT: zookeeper1:2181
      CONFLUENT_METRICS_REPORTER_TOPIC_REPLICAS: 1
      CONFLUENT_METRICS_ENABLE: 'false'
    networks:
      - kafka-network


  connect:
    image: docker.arvancloud.ir/confluentinc/cp-kafka-connect:latest
    hostname: connect
    container_name: connect
    depends_on:
      - zookeeper1
      - kafka
    ports:
      - '8083:8083'
    environment:
      CONNECT_BOOTSTRAP_SERVERS: 'kafka:29092'
      CONNECT_REST_ADVERTISED_HOST_NAME: connect
      CONNECT_REST_PORT: 8083
      CONNECT_GROUP_ID: compose-connect-group
      CONNECT_CONFIG_STORAGE_TOPIC: docker-connect-configs
      CONNECT_CONFIG_STORAGE_REPLICATION_FACTOR: 1
      CONNECT_OFFSET_FLUSH_INTERVAL_MS: 10000
      CONNECT_OFFSET_STORAGE_TOPIC: docker-connect-offsets
      CONNECT_OFFSET_STORAGE_REPLICATION_FACTOR: 1
      CONNECT_STATUS_STORAGE_TOPIC: docker-connect-status
      CONNECT_STATUS_STORAGE_REPLICATION_FACTOR: 1
      CONNECT_KEY_CONVERTER: org.apache.kafka.connect.json.JsonConverter
      CONNECT_VALUE_CONVERTER: org.apache.kafka.connect.json.JsonConverter
      CONNECT_INTERNAL_KEY_CONVERTER: 'org.apache.kafka.connect.json.JsonConverter'
      CONNECT_INTERNAL_VALUE_CONVERTER: 'org.apache.kafka.connect.json.JsonConverter'
      CONNECT_KEY_CONVERTER_SCHEMAS_ENABLE: 'false'
      CONNECT_VALUE_CONVERTER_SCHEMAS_ENABLE: 'false'
      CONNECT_ZOOKEEPER_CONNECT: 'zookeeper1:2181'
      # CLASSPATH required due to CC-2422
      CLASSPATH: /usr/share/java/monitoring-interceptors/monitoring-interceptors-5.4.1.jar
      CONNECT_PRODUCER_INTERCEPTOR_CLASSES: 'io.confluent.monitoring.clients.interceptor.MonitoringProducerInterceptor'
      CONNECT_CONSUMER_INTERCEPTOR_CLASSES: 'io.confluent.monitoring.clients.interceptor.MonitoringConsumerInterceptor'
      CONNECT_PLUGIN_PATH: '/usr/share/java,/usr/share/confluent-hub-components'
      CONNECT_LOG4J_LOGGERS: org.apache.zookeeper=ERROR,org.I0Itec.zkclient=ERROR,org.reflections=ERROR
    networks:
      - kafka-network


  schema-registry:
    image: docker.arvancloud.ir/confluentinc/cp-schema-registry:latest
    hostname: schema-registry
    container_name: schema-registry
    depends_on:
      - zookeeper1
      - kafka
    ports:
      - "8081:8081"
    environment:
      SCHEMA_REGISTRY_HOST_NAME: schema-registry
      #SCHEMA_REGISTRY_KAFKASTORE_CONNECTION_URL: 'zookeeper1:2181'
      SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS: PLAINTEXT://kafka:29092
    networks:
      - kafka-network
    restart: unless-stopped


  akhq:
    image: tchiotludo/akhq
    hostname: web-ui
    container_name: web-ui  
    environment:
      AKHQ_CONFIGURATION: |
        akhq:
          connections:
            docker-kafka-server:
              properties:
                bootstrap.servers: "kafka:29092"
              schema-registry:
                url: "http://schema-registry:8081"
              connect:
                - name: "connect"
                  url: "http://connect:8083"
              ksqldb:
                - name: "ksqldb"
                  url: "http://ksqldb-server2:8088"
                  
    ports:
      - 8080:8080
    links:
      - kafka
      - schema-registry
      -	ksqldb-server2
    networks:
      - kafka-network


  ksqldb-server2:
    image: docker.arvancloud.ir/confluentinc/cp-ksqldb-server:latest
    hostname: ksqldb-server2
    container_name: ksqldb-server2
    depends_on:
      - kafka
      - schema-registry
    ports:
      - "8088:8088"
    environment:
      KSQL_CONFIG_DIR: "/etc/ksqldb"
      KSQL_LOG4J_OPTS: "-Dlog4j.configuration=file:/etc/ksqldb/log4j.properties"
      KSQL_BOOTSTRAP_SERVERS: "kafka:29092"
      KSQL_HOST_NAME: ksqldb-server2
      KSQL_LISTENERS: "http://0.0.0.0:8088"
      KSQL_CACHE_MAX_BYTES_BUFFERING: 0
      KSQL_KSQL_SCHEMA_REGISTRY_URL: "http://schema-registry:8081"
      KSQL_CONNECT_URL: "http://connect:8083"
    networks:
      - kafka-network


  ksqldb-cli2:
    image: docker.arvancloud.ir/confluentinc/cp-ksqldb-cli:latest
    container_name: ksqldb-cli2
    depends_on:
      - kafka
      - ksqldb-server2
    entrypoint: /bin/sh
    environment:
      KSQL_CONFIG_DIR: "/etc/ksqldb"
    tty: true
    # volumes:
    #   - codes/src:/opt/app/src
    #   - codes/test:/opt/app/test
    networks:
      - kafka-network
  

networks:
  kafka-network:
    driver: bridge
    name: kafka-network

```
```
docker compose up
```
Docker Desktop :

![alt text](https://raw.githubusercontent.com/kayvansol/kafka-sink/main/img/containers.png?raw=true)

then check all the container's logs for being healthy.

Create a kafka topic named **usertopic** :
```bash
docker exec -it kafka bash

cd ..
cd ..

bin/kafka-topics --create --if-not-exists --topic usertopic --replication-factor=1 --partitions=3 --bootstrap-server kafka:9092
```
Create a kafka stream on top of the created topic with  [apache **avro**](https://avro.apache.org) format :
```bash
docker exec -it ksqldb-cli2 bash

ksql http://ksqldb-server2:8088
```

![alt text](https://raw.githubusercontent.com/kayvansol/kafka-sink/main/img/ksql.png?raw=true)

```bash
ksql> CREATE STREAM s2 (name VARCHAR, favorite_number INTEGER,favorite_color VARCHAR) WITH (kafka_topic='usertopic', value_format='avro');
```

The kafka producer c# program with avro serializer and Schema Registry [Program.cs](https://github.com/kayvansol/kafka-sink/blob/main/Program.cs) :
```C#
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
```

User.cs :
```c#
using Avro;
using Avro.Specific;

public class User : ISpecificRecord
{
	public static Schema _SCHEMA = Avro.Schema.Parse(@"{""type"":""record"",""name"":""User"",""namespace"":""confluent.io.examples.serialization.avro"",""fields"":    
                [{""name"":""name"",""type"":""string""},{""name"":""favorite_number"",""type"":""int""},{""name"":""favorite_color"",""type"":""string""}]}");
	private string _name;
	private int _favorite_number;
	private string _favorite_color;
	public virtual Schema Schema
	{
		get
		{
			return User._SCHEMA;
		}
	}
	public string name
	{
		get
		{
			return this._name;
		}
		set
		{
			this._name = value;
		}
	}
	public int favorite_number
	{
		get
		{
			return this._favorite_number;
		}
		set
		{
			this._favorite_number = value;
		}
	}
	public string favorite_color
	{
		get
		{
			return this._favorite_color;
		}
		set
		{
			this._favorite_color = value;
		}
	}
	public virtual object Get(int fieldPos)
	{
		switch (fieldPos)
		{
			case 0: return this.name;
			case 1: return this.favorite_number;
			case 2: return this.favorite_color;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
		};
	}
	public virtual void Put(int fieldPos, object fieldValue)
	{
		switch (fieldPos)
		{
			case 0: this.name = (System.String)fieldValue; break;
			case 1: this.favorite_number = (System.Int32)fieldValue; break;
			case 2: this.favorite_color = (System.String)fieldValue; break;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
		};
	}
}
```

User.avsc :
```avro
{
  "namespace": "confluent.io.examples.serialization.avro",
  "name": "User",
  "type": "record",
  "fields": [
    {
      "name": "name",
      "type": "string"
    },
    {
      "name": "favorite_number",
      "type": "int"
    },
    {
      "name": "favorite_color",
      "type": "string"
    }
  ]
}
```

Run the c# code to insert some data to stream (to topic) :

then in ksql command shell :
```bash
ksql> select * from s2;
```
![alt text](https://raw.githubusercontent.com/kayvansol/kafka-sink/main/img/ksql.jpeg?raw=true)

go to [http://localhost:8080/ui/docker-kafka-server/topic/usertopic/data?sort=Oldest&partition=All](http://localhost:8080/ui/docker-kafka-server/topic/usertopic/data?sort=Oldest&partition=All)

![alt text](https://raw.githubusercontent.com/kayvansol/kafka-sink/main/img/kafkatopic.jpeg?raw=true)

and the schema created automatically or altered by you at schema-registry, also you can create the schema by powershell script manually:
```powershell
$body = @{
   schema = @{
         "type": "record",
         "name": "User",
         "namespace": "confluent.io.examples.serialization.avro",
         "fields": [
           {
             "name": "name",
             "type": "string"
           },
           {
             "name": "favorite_number",
             "type": "int"
           },
           {
             "name": "favorite_color",
             "type": "string"
           }
         ],
      }
   }

Invoke-RestMethod -Method Post -Uri "http://localhost:8081/subjects/usertopic-value/versions" -ContentType "application/vnd.schemaregistry.v1+json" -Body ($body | ConvertTo-Json)
```
![alt text](https://raw.githubusercontent.com/kayvansol/kafka-sink/main/img/schema.jpeg?raw=true)

then create a kafka connector to sql server :
```powershell
$body = @{
    name = "sql-server-sink"
    config = @{
        "connector.class": "io.confluent.connect.jdbc.JdbcSinkConnector",
        "table.name.format": "mytopic",
        "connection.password": "your password",
        "tasks.max": "1",
        "topics": "usertopic",
        "schema.registry.url": "http://schema-registry:8081",
        "value.converter.schema.registry.url": "http://schema-registry:8081",
        "auto.evolve": "true",
        "connection.user": "sa",
        "value.converter.schemas.enable": "true",
        "name": "sql-server-sink",
        "auto.create": "true",
        "value.converter": "io.confluent.connect.avro.AvroConverter",
        "connection.url": "jdbc:sqlserver://192.168.1.4:1433;databaseName=kafkaconnect",
        "insert.mode": "insert",
        "pk.mode": "none"
    }
}

Invoke-RestMethod -Method Post -Uri "http://localhost:8083/connectors" -ContentType "application/json" -Body ($body | ConvertTo-Json)
```

![alt text](https://raw.githubusercontent.com/kayvansol/kafka-sink/main/img/connect.jpeg?raw=true)

and after inserting new data to the stream by c# program, the defined kafka connector, sync data with related sql server table :

![alt text](https://raw.githubusercontent.com/kayvansol/kafka-sink/main/img/synced.jpeg?raw=true)

