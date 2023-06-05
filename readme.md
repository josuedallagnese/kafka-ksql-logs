# Exemplo

1) Suba o kafka + ksqldb na pasta tools:
```
docker compose up -d
```

2) Verifique o status do ksql:
- http://localhost:8088/info
- http://localhost:8088/healthcheck

3) Abra um cmd ou um sh dentro da pasta "Kafka.Sample.Producer" e execute para criar e alimentar os tópicos "products", "customers" e "orders":
```
dotnet run
```

4) Acesse o ksql-cli executando o ksql-cli:
```
docker compose exec ksqldb-cli ksql http://ksqldb-server:8088

// Para ver o conteúdo do tópicos, tables e streams:
ksql> show streams;
ksql> CREATE STREAM orders (Date VARCHAR KEY, ProductCode VARCHAR, CustomerId VARCHAR, Quantity INTEGER) WITH (KAFKA_TOPIC='orders', VALUE_FORMAT='JSON');
ksql> CREATE STREAM products (Code VARCHAR KEY, Name VARCHAR) WITH (KAFKA_TOPIC='products', VALUE_FORMAT='JSON');

ksql> CREATE TABLE TOP_ORDERS AS
        SELECT p.name, SUM(o.quantity) AS total
        FROM orders o JOIN products p WITHIN 7 DAYS GRACE PERIOD 30 MINUTES ON p.code = o.productCode
        GROUP BY p.name;
ksql> SET 'auto.offset.reset' = 'earliest';
ksql> print TOP_ORDERS;
```
