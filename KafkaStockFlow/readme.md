
---

# KafkaStockFlow

A robust .NET 9 demo application for real-time stock trade processing using Apache Kafka, featuring advanced Kafka patterns, JSON serialization, and scalable ingestion.

---

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Setup Instructions](#setup-instructions)
- [Kafka Cluster Setup](#kafka-cluster-setup)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [Data Models](#data-models)
- [Producer/Consumer Code Snippets](#producerconsumer-code-snippets)
- [Serialization Strategy](#serialization-strategy)
- [Performance & Scaling](#performance--scaling)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)
- [Deployment](#deployment)
- [Contributing](#contributing)
- [License](#license)

---

## Overview

KafkaStockFlow demonstrates a scalable, production-inspired stock market data pipeline using .NET 9 and Apache Kafka. It features:

- **Real-time Stock Data:** Ingestion from Alpha Vantage API or other data sources
- **Kafka Integration:** Producer/consumer patterns with advanced features
- **Custom Partitioning:** Symbol-based partitioning for parallel processing
- **Error Handling:** Comprehensive exception management and logging

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/products/docker-desktop)
- [Git](https://git-scm.com/)
- An Alpha Vantage API key (free tier available at [alphavantage.co](https://www.alphavantage.co/))

---

## Project Structure


```
KafkaStockFlow/
│
├── Program.cs
├── appsettings.json
├── docker-compose.yml
│
├── Models/
│   └── TradeEvent.cs
├── Kafka/
│   ├── KafkaConfig.cs
│   ├── CustomPartitioner.cs
│   ├── SchemaRegistryService.cs
│   └── JsonSerializerHelper.cs
├── Services/
│   ├── ProducerService.cs
│   └── ConsumerService.cs
├── ConsumedTrades/
│   └── consumed_trades_[date].csv
│
└── README.md

```

---

## Setup Instructions

### 1. Clone the Repository


```
git clone https://github.com/your-org/KafkaStockFlow.git
cd KafkaStockFlow

```

### 2. Install Dependencies


```
dotnet restore

```

### 3. Start Kafka and Zookeeper


```
docker-compose up -d

```

This will:
- Start Zookeeper and Kafka
- Automatically create the `stock-trades` topic with 3 partitions

---

## Kafka Cluster Setup

- **Local:** Uses Docker Compose (see `docker-compose.yml`)
- **Dev/Prod:** Adjust broker addresses, replication factors, and security settings in `appsettings.json` and Docker Compose as needed.

**Topic Creation:**  
The topic `stock-trades` is created at container startup with 3 partitions and replication factor 1:


```
kafka-topics --create --if-not-exists --topic stock-trades --bootstrap-server localhost:9092 --partitions 3 --replication-factor 1

```

---

## Configuration

Edit `appsettings.json` as needed:


```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "Topic": "stock-trades",
    "GroupId": "stock-consumer-group",
    "EnableIdempotence": true,
    "TransactionalId": "stock-producer-1"
  },
  "Logging": {
    "LogLevel": "Information"
  }
}

```

- **BootstrapServers:** Your Kafka broker address(es)
- **Topic:** The topic for trade events
- **GroupId:** Consumer group identifier
- **EnableIdempotence:** Ensures exactly-once delivery (when possible)
- **TransactionalId:** For transactional producers

---

## Running the Application


```
dotnet run

```

- The app will ingest stock data from Alpha Vantage API
- Process trades via Kafka producer/consumer
- Save consumed trades to CSV in the ConsumedTrades directory

---

## Data Models

### TradeEvent


```csharp
public class TradeEvent
{
    public string TradeId { get; set; }
    public string Symbol { get; set; }
    public string Side { get; set; } // "Buy" or "Sell"
    public int Quantity { get; set; }
    public double Price { get; set; }
    public DateTime Timestamp { get; set; }
}

```

---

## Producer/Consumer Code Snippets

### Producer Example


```csharp
// Create a trade
var trade = new TradeEvent
{
    Symbol = "AAPL",
    Side = "Buy",
    Quantity = 100,
    Price = 175.50,
    Timestamp = DateTime.UtcNow
};

// Produce to Kafka
await producerService.ProduceTradeAsync(trade);

```

### Consumer Example


```csharp
// Start consuming
using var cts = new CancellationTokenSource();
Task.Run(() => consumerService.StartConsuming(cts.Token));

// To stop
cts.Cancel();

```

---

## Serialization Strategy

- **JSON Serialization:** Uses System.Text.Json for message serialization/deserialization
- See `JsonSerializerHelper.cs` for implementation details
- Custom serialization settings can be adjusted as needed

---

## Performance & Scaling

- **Custom Partitioning:** Ensures even distribution by stock symbol
- **Idempotent Producer:** Prevents duplicates on network retries
- **Dynamic Partition Count:** Fetches partition count at runtime
- **Scalability:** Add more partitions and consumer instances as needed

---

## Testing

- **Manual Testing:** Run the app and verify messages in Kafka
- **Tools:**
  - Use [kcat](https://github.com/edenhill/kcat) to view messages in the topic
  - Check the ConsumedTrades directory for output files

---

## Troubleshooting

- **Kafka Not Running:** Ensure Docker containers are up (`docker-compose ps`)
- **Unknown Partition Error:** Topic may not exist or partition count mismatch; check topic with:
  
  
```
  docker exec -it kafka kafka-topics --describe --topic stock-trades --bootstrap-server localhost:9092
  
```
- **API Rate Limits:** Alpha Vantage free tier allows 5 requests/minute
- **Producer/Consumer Errors:** Check logs for detailed information

---

## Deployment

- **Docker Compose:** For local/dev environments
- **Kubernetes:** Adapt `docker-compose.yml` to Kubernetes manifests
- **Cloud:**
  - AWS: Use MSK for managed Kafka
  - Azure: Event Hubs with Kafka interface
  - GCP: Pub/Sub or self-managed Kafka on GKE

---

## Contributing

- Fork the repo and submit pull requests
- Follow the coding standards and add documentation for new features

---

## License

MIT (or your chosen license)

---
