# Monitor

A dynamic server health monitoring system that collects CPU, memory, and service status from Linux servers via SSH, stores metrics in PostgreSQL, and displays them in a modern Angular dashboard.

## Architecture

- **ASP.NET Core 8 API** - Backend for server management, metric collection via SSH, and REST API
- **Angular 17+ Web Dashboard** - Frontend with real-time charts and server management
- **PostgreSQL 16** - Time-series storage for historical metrics
- **Docker Compose** - Container orchestration

## Quick Start

### Prerequisites

- Docker and Docker Compose
- SSH key access to your Linux servers

### Setup

1. Clone the repository and navigate to the project directory

2. Create environment file:
   ```bash
   cp .env.example .env
   # Edit .env and set a secure POSTGRES_PASSWORD
   ```

3. Place your SSH private keys in a directory and mount them:
   ```bash
   mkdir ssh-keys
   cp ~/.ssh/your_key ssh-keys/
   ```

4. Start the stack:
   ```bash
   docker-compose up -d
   ```

5. Access the dashboard at http://localhost:4200

## Development

### API (ASP.NET Core)

```bash
cd apps/api/Monitor.Api
dotnet restore
dotnet run
```

### Web Dashboard (Angular)

```bash
cd apps/web
npm install
ng serve
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/servers` | List all monitored servers |
| POST | `/api/servers` | Add a new server |
| PUT | `/api/servers/{id}` | Update a server |
| DELETE | `/api/servers/{id}` | Remove a server |
| GET | `/api/servers/{id}/metrics` | Get historical metrics |
| GET | `/api/servers/{id}/services` | Get service statuses |
| POST | `/api/servers/{id}/test-connection` | Test SSH connection |

## Configuration

Environment variables for the API:

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | - |
| `MetricCollection__IntervalSeconds` | Metric collection interval | 30 |
| `MetricCollection__RetentionDays` | Days to keep historical data | 30 |

## License

MIT
