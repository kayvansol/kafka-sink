FROM docker.arvancloud.ir/confluentinc/cp-kafka-connect:latest

RUN confluent-hub install confluentinc/kafka-connect-jdbc:10.7.6 --no-prompt
