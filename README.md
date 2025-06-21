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
   

```shell
wget https://downloads.apache.org/kafka/3.6.1/kafka_2.13-3.6.1.tgz
tar -xzf kafka_2.13-3.6.1.tgz
cd kafka_2.13-3.6.1

```

2. **Start ZooKeeper**:
   

```shell
bin/zookeeper-server-start.sh config/zookeeper.properties

```

3. **Start Kafka Server** (in a new terminal):
   

```shell
bin/kafka-server-start.sh config/server.properties

```

4. **Create a Topic** (replace `stock-trades` with your topic name):
   

```shell
bin/kafka-topics.sh --create --topic stock-trades --bootstrap-server localhost:9092 --partitions 1 --replication-factor 1

```

### Docker Setup (Alternative)

Alternatively, you can use Docker Compose for a containerized setup:

1. Create a `docker-compose.yml` file:


```yaml
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
   

```shell
docker-compose up -d

```

3. Create a topic:
   

```shell
docker exec kafka kafka-topics --create --topic stock-trades --bootstrap-server kafka:29092 --partitions 1 --replication-factor 1

```

### Configuration

Update the `appsettings.json` file with your Kafka settings and Alpha Vantage API key:


```json
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
   

```shell
git clone https://github.com/yourusername/KafkaStockFlow.git
cd KafkaStockFlow

```

2. **Restore dependencies**:
   

```shell
dotnet restore

```

3. **Update appsettings.json** with your Alpha Vantage API key and preferred stock symbol.

### Building and Running

1. **Build the project**:
   

```shell
dotnet build

```

2. **Run the application**:
   

```shell
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
- **Port Conflicts**: If ports 9092 or 2181 are already in use, configure different ports in Kafka/ZooKeeper settings.
- **Message Serialization Errors**: Ensure the TradeEvent model properties match the data structure expected in serialization/deserialization.
- **Missing Dependencies**: Run `dotnet restore` if you encounter assembly loading exceptions.
- **File Permission Issues**: Ensure the application has write permissions to the ConsumedTrades directory.

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
The project includes unit tests to verify individual components:


```shell
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter Category=Unit

```

### Integration Testing
Integration tests verify the interaction between components and external systems:

1. **Kafka Integration Tests**:
   - Tests require a running Kafka instance (use Docker setup)
   - Verifies connectivity, message publishing, and consumption
   - Validates serialization/deserialization processes

2. **API Integration Tests**:
   - Tests Alpha Vantage API connectivity
   - Validates API response parsing
   - Includes mock responses for offline testing

### Performance Testing
Load testing scripts are available to validate system performance under high volume:


```shell
# Run performance test with 1000 messages
dotnet run --project tests/KafkaStockFlow.LoadTests -- --count 1000

```

### Testing Best Practices
- Keep unit tests isolated from external dependencies
- Use mock objects for external services in unit tests
- Run integration tests in CI/CD pipelines with dedicated environments
- Monitor performance metrics during load testing

## Deployment

### Local Deployment
For local development and testing:


```shell
dotnet publish -c Release
cd bin/Release/net9.0/publish
dotnet KafkaStockFlow.dll

```

### Docker Deployment
Package the application in a container for portable deployment:

1. **Create Dockerfile**:


```docker
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["KafkaStockFlow.csproj", "./"]
RUN dotnet restore "KafkaStockFlow.csproj"
COPY . .
RUN dotnet build "KafkaStockFlow.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KafkaStockFlow.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KafkaStockFlow.dll"]

```

2. **Build and run container**:


```shell
docker build -t kafkastockflow:latest .
docker run -d --name kafkastockflow \
  -e "Kafka__BootstrapServers=kafka:9092" \
  -e "AlphaVantage__ApiKey=YOUR_API_KEY" \
  kafkastockflow:latest

```

### Cloud Deployment Options

1. **Azure**:
   - Deploy as Azure App Service
   - Use Azure Event Hubs as managed Kafka service
   - Configure app settings for connection strings

2. **AWS**:
   - Deploy to EC2 or ECS containers
   - Use MSK (Managed Streaming for Kafka)
   - Configure environment variables for settings

3. **Kubernetes**:
   - Package as container and deploy to Kubernetes cluster
   - Use StatefulSets for Kafka brokers
   - Configure with ConfigMaps and Secrets

### Production Considerations
- Implement robust logging and monitoring
- Configure proper retry policies and circuit breakers
- Use managed Kafka services for production workloads
- Implement health checks and automated scaling
- Set up alerts for critical errors and performance degradation

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