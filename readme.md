# Kafka + ksqlDB + Logging

Em determinadas situações sejam por limitações de infra ou de outra natureza, não temos como utilizar uma coleta de log para requests de entrada em nossa API baseada em um modelo sidecar, como fariamos com o istio por exemplo. \
Para um ambiente já existente de Kafka por exemplo, a ideia foi utilizar o proprio broker como ferramenta para armezenar esses logs e consultá-los através do ksqlDB. \
A rentenção desse log será controlada pelo tempo de retenção do próprio tópico criado.
Essa library coleta os dados usando um Attributo de RequestLogAttribute que realiza a leitura no seguinte modelo:

```json

```

KafkaUI: http://localhost:8080
API de exemplo: http://localhost:5002/swagger/index.html

# Executando o exemplo:
1) Dentro da pasta docker execute:
```shell
sh build.sh
sh run.sh
```

2) Dentro da pasta load-test rode o teste de carga para gerar dados para seu teste:
```sh
sudo apt-get install unzip
sudo apt-get update
sudo apt install default-jdk
java -version
```

```sh
python resources_generator.py
```

```sh
sh install-gatling.sh
sh run-test.sh
```

3) Abra o Kafka UI pelo endereço http://localhost:8080/ e na seção ksqlDB execute as queries abaixo alterando conforme o seu contexto:

```sql
-- Para cria o stream
CREATE STREAM account_logs (queryString VARCHAR, headers VARCHAR, path VARCHAR, request VARCHAR, response VARCHAR, statusCode INTEGER, timestamp VARCHAR)
  WITH (kafka_topic='request-logging.account', value_format='JSON');

SET 'auto.offset.reset' = 'earliest';
SET 'auto.offset.reset' = 'latest';

select timestamp, path, JSON_RECORDS(request), JSON_RECORDS(response)
from account_logs
where 
  PARSE_TIMESTAMP(timestamp, 'yyyy-MM-dd HH:mm:ss') > '2024-01-29T14:00:00'
LIMIT 1000;

select timestamp, path, JSON_RECORDS(request), JSON_RECORDS(response)
from account_logs
where 
  PARSE_TIMESTAMP(timestamp, 'yyyy-MM-dd HH:mm:ss') > '2024-01-29T14:00:00'
  AND
  EXTRACTJSONFIELD(request, '$.mail') = 'HX5haVPa@gmail.com'
LIMIT 1000;

select timestamp, path, JSON_RECORDS(request), JSON_RECORDS(response)
from account_logs
where 
  PARSE_TIMESTAMP(timestamp, 'yyyy-MM-dd HH:mm:ss') > '2024-01-29T14:00:00'
  AND
  statusCode = 500
LIMIT 1000;

select timestamp, path, JSON_RECORDS(request), JSON_RECORDS(response)
from account_logs
where 
  PARSE_TIMESTAMP(timestamp, 'yyyy-MM-dd HH:mm:ss') > '2024-01-29T14:00:00'
  AND
  response like '%Sorry about that%'
LIMIT 1000;

```