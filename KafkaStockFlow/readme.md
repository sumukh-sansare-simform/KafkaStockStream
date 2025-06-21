```markdown
# KafkaStockFlow

## Table of Contents
- [Project Overview](#project-overview)
  - [Key Features](#key-features)
  - [Technologies Used](#technologies-used)
- [Project Structure](#project-structure)
- [Kafka Installation & Setup](#kafka-installation--setup)
  - [Prerequisites](#prerequisites)
  - [Local Kafka Setup](#local-kafka-setup)
  - [Docker Setup (Alternative)](#docker-setup-alternative)
  - [Configuration](#configuration)
- [How to Run the Project](#how-to-run-the-project)
  - [Setup](#setup)
  - [Building and Running](#building-and-running)
  - [Troubleshooting](#troubleshooting)
- [Project Communication Flow](#project-communication-flow)
  - [Data Flow Architecture](#data-flow-architecture)
  - [Process Description](#process-description)
- [Testing](#testing)
- [Deployment](#deployment)
- [Additional Documentation](#additional-documentation)
- [Contributing](#contributing)
- [License](#license)

## Project Overview

KafkaStockFlow is a real-time stock data processing application built on .NET 9 that leverages Apache Kafka for reliable data streaming. The application fetches stock market data from Alpha Vantage API and processes the trade events through a robust producer-consumer architecture.

### Key Features
- Real-time ingestion of stock market data from Alpha Vantage
- Fault-tolerant data streaming using Apache Kafka
- Reliable message processing with configurable consumer groups
- CSV export of processed trade events with timestamp-based file organization
- Configurable stock symbols and API integration

### Technologies Used
- .NET 9.0
- Apache Kafka (via Confluent.Kafka 2.10.1)
- Confluent Schema Registry
- Microsoft Extensions (Configuration, DI, Logging)
- Alpha Vantage API for financial market data

## Project Structure



```
KafkaStockFlow/
├── Program.cs                   # Main application entry point
├── KafkaStockFlow.csproj        # Project file with dependencies
├── appsettings.json             # Configuration file for Kafka and API settings
├── ConsumedTrades/              # Directory for storing processed trade data
│   └── consumed_trades_*.csv    # Output CSV files with timestamped naming
├── Models/                      # Data models
│   └── TradeEvent.cs            # Stock trade event model class
├── Services/                    # Application services
│   ├── ProducerService.cs       # Service for producing Kafka messages
│   └── ConsumerService.cs       # Service for consuming Kafka messages
└── Kafka/                       # Kafka-specific implementations
    ├── KafkaConfig.cs           # Kafka configuration loader
    ├── JsonSerializerHelper.cs  # JSON serialization for Kafka messages
    ├── JsonSerializer.cs        # JSON serializer for producer
    └── JsonDeserializer.cs      # JSON deserializer for consumer


```

## Kafka Installation & Setup

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Java JDK 8+](https://adoptopenjdk.net/)
- [Apache Kafka](https://kafka.apache.org/downloads) or [Confluent Platform](https://www.confluent.io/download/)

### Local Kafka Setup

1. **Download and Extract Kafka**:
   

```
   wget https://downloads.apache.org/kafka/3.6.1/kafka_2.13-3.6.1.tgz
   tar -xzf kafka_2.13-3.6.1.tgz
   cd kafka_2.13-3.6.1
   

```

2. **Start ZooKeeper**:
   

```
   bin/zookeeper-server-start.sh config/zookeeper.properties
   

```

3. **Start Kafka Server** (in a new terminal):
   

```
   bin/kafka-server-start.sh config/server.properties
   

```

4. **Create a Topic** (replace `stock-trades` with your topic name):
   

```
   bin/kafka-topics.sh --create --topic stock-trades --bootstrap-server localhost:9092 --partitions 1 --replication-factor 1
   

```

### Docker Setup (Alternative)

Alternatively, you can use Docker Compose for a containerized setup:

1. Create a `docker-compose.yml` file:



```
version: '3'
services:
  zookeeper:
    image: confluentinc/cp-zookeeper:latest
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
    ports:
      - "2181:2181"

  kafka:
    image: confluentinc/cp-kafka:latest
    depends_on:
      - zookeeper
    ports:
      - "9092:9092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:29092,PLAINTEXT_HOST://localhost:9092
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT
      KAFKA_INTER_BROKER_LISTENER_NAME: PLAINTEXT
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1

```

2. Start the services:
   

```
   docker-compose up -d
   

```

3. Create a topic:
   

```
   docker exec kafka kafka-topics --create --topic stock-trades --bootstrap-server kafka:29092 --partitions 1 --replication-factor 1
   

```

### Configuration

Update the `appsettings.json` file with your Kafka settings and Alpha Vantage API key:



```
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "stock-trade-consumers",
    "Topic": "stock-trades"
  },
  "AlphaVantage": {
    "ApiKey": "YOUR_API_KEY_HERE",
    "Symbol": "AAPL"
  }
}


```

## How to Run the Project

### Setup

1. **Clone the repository**:
   

```
   git clone https://github.com/yourusername/KafkaStockFlow.git
   cd KafkaStockFlow
   

```

2. **Restore dependencies**:
   

```
   dotnet restore
   

```

3. **Update appsettings.json** with your Alpha Vantage API key and preferred stock symbol.

### Building and Running

1. **Build the project**:
   

```
   dotnet build
   

```

2. **Run the application**:
   

```
   dotnet run
   

```

3. **Expected output**:
   - The application will start producing messages from Alpha Vantage API
   - After a brief delay, the consumer will start processing these messages
   - Processed trades will be saved to CSV files in the `ConsumedTrades` directory
   - Console will display logs of the producer and consumer operations
   - Press any key to stop the application gracefully

### Troubleshooting

- **Kafka Connection Issues**: Ensure Kafka and ZooKeeper are running and check the bootstrap server address in `appsettings.json`.
- **Alpha Vantage API Limits**: Free API keys have usage limits. If you encounter errors, you might need to wait or obtain a premium API key.
- **Consumer Group Already Exists**: If you see warnings about consumer groups, you can reset offsets or use a new group ID in the configuration.
- **JVM Memory Issues**: If Kafka crashes due to memory constraints, adjust the JVM heap size in Kafka's `kafka-server-start.sh` script.
- **Network Connectivity**: Verify that there are no firewall rules blocking the required ports (9092 for Kafka, 2181 for ZooKeeper).
- **CSV File Access**: Ensure the application has write permissions to the ConsumedTrades directory.
- **Schema Errors**: If messages fail to serialize/deserialize, check that your `TradeEvent` model matches the data coming from Alpha Vantage.

## Project Communication Flow

### Data Flow Architecture



```
┌─────────────────┐    ┌─────────────┐    ┌──────────────┐    ┌─────────────────┐
│  Alpha Vantage  │───→│  Producer   │───→│  Kafka Topic │───→│  Consumer       │
│  API            │    │  Service    │    │  stock-trades│    │  Service        │
└─────────────────┘    └─────────────┘    └──────────────┘    └────────┬────────┘
                                                                       │
                                                                       ▼
                                                              ┌─────────────────┐
                                                              │  CSV Export     │
                                                              │  (ConsumedTrades)│
                                                              └─────────────────┘


```

### Process Description

1. **Data Ingestion**:
   - `ProducerService` connects to Alpha Vantage API
   - Fetches stock data for the configured symbol
   - Converts API responses to `TradeEvent` objects
   - Serializes events to JSON and publishes to Kafka

2. **Message Streaming**:
   - Kafka ensures reliable message delivery
   - Messages are stored in the topic with configurable retention
   - Consumer groups track message offsets for reliable processing

3. **Data Processing**:
   - `ConsumerService` subscribes to the Kafka topic
   - Deserializes JSON messages back to `TradeEvent` objects
   - Processes events according to business logic
   - Writes processed data to CSV files with date-based naming

4. **Error Handling**:
   - Producer implements idempotent deliveries for exactly-once semantics
   - Consumer manually controls offset commits for at-least-once processing
   - Logging captures operational events and errors for monitoring

## Testing

### Unit Testing
- Unit tests are available in the `KafkaStockFlow.Tests` project.
- Tests cover individual components including serialization/deserialization and service logic.
- Use the following command to run unit tests:


```
dotnet test

```

### Integration Testing
- Integration tests verify the interaction between components and Kafka.
- These tests require a running Kafka instance (use the Docker setup for tests).
- Run integration tests with:


```
dotnet test --filter Category=Integration

```

### Performance Testing
- A simple load test script is available in the `Tools/LoadTester` directory.
- This helps simulate high message throughput to verify system stability:


```
dotnet run --project Tools/LoadTester -- --messages 10000 --rate 100

```

### Test Data
- Sample test data is included in the `TestData` directory.
- Use this data to verify consumer behavior without API calls:


```
dotnet run -- --use-test-data

```

## Deployment

### Prerequisites for Deployment
- Target environment must have .NET 9 runtime installed
- Kafka cluster must be accessible from the application
- Network configuration must allow connections to Alpha Vantage API

### Docker Deployment

1. **Build Docker image**:


```
docker build -t kafkastockflow:latest .

```

2. **Run the container**:


```
docker run -d --name kafkastockflow \
  -e "Kafka__BootstrapServers=your-kafka-server:9092" \
  -e "AlphaVantage__ApiKey=YOUR_API_KEY" \
  -v /path/to/output:/app/ConsumedTrades \
  kafkastockflow:latest

```

### Cloud Deployment

The application can be deployed as a service on various cloud platforms:

#### Azure Service Deployment
1. Create an Azure App Service with .NET 9 runtime.
2. Deploy the application using Azure DevOps pipelines or GitHub Actions.
3. Configure connection strings and API keys in Application Settings.
4. Set up Azure Event Hubs as a managed Kafka alternative.

#### AWS Deployment
1. Deploy using Elastic Beanstalk or ECS for containerized deployment.
2. Use AWS MSK (Managed Streaming for Kafka) for the Kafka cluster.
3. Configure environment variables for connection settings.

### Health Monitoring
- The application exposes health endpoints at `/health` for monitoring.
- Use your infrastructure's monitoring tools to check application status.
- Configure alerts for service disruptions or high latency.

## Additional Documentation

- [Alpha Vantage API Documentation](https://www.alphavantage.co/documentation/)
- [Confluent Kafka .NET Client](https://docs.confluent.io/platform/current/clients/dotnet.html)
- [Apache Kafka Documentation](https://kafka.apache.org/documentation/)

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

```
