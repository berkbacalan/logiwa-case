# EcomMMS - E-commerce Management System Logiwa Case

A modern, scalable e-commerce management system built with .NET 9, Clean Architecture, and Docker.
Some features have not been implemented since this is a case study for Logiwa.

## 🚀 Features

- **Clean Architecture** with Domain, Application, Infrastructure, and API layers
- **CQRS Pattern** with MediatR for command and query separation
- **Structured Logging** with Serilog and Seq
- **Caching** with Redis
- **Database** with PostgreSQL and Entity Framework Core
- **API Versioning** support
- **Rate Limiting** for API protection
- **CORS** configuration for frontend integration
- **Health Checks** for monitoring
- **Docker** containerization
- **Environment Variables** for secure configuration

## 🛠️ Technology Stack

- **.NET 9** - Latest .NET framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM
- **PostgreSQL** - Database
- **Redis** - Caching
- **Seq** - Log aggregation
- **Docker** - Containerization
- **MediatR** - CQRS implementation
- **FluentValidation** - Input validation
- **Serilog** - Structured logging

## 📋 Prerequisites

- Docker and Docker Compose
- .NET 9 SDK (for local development)

## 🚀 Quick Start

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd logiwa-case
   ```

2. **Set up environment variables**
   ```bash
   cp env.example .env
   # Edit .env file with your configuration
   ```

3. **Run with Docker Compose**
   ```bash
   docker-compose up --build -d
   ```

4. **Access the application**
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger
   - Seq Logs: http://localhost:5341
   - Health Check: http://localhost:5000/health

## 📚 API Documentation

### Base URL
```
http://localhost:5000/api/v1.0
```

### Endpoints

#### Products
- `GET /products` - Get filtered products
- `GET /products/{id}` - Get product by ID
- `POST /products` - Create new product
- `PUT /products/{id}` - Update product
- `DELETE /products/{id}` - Delete product

#### Categories
- `GET /categories` - Get all categories

### API Versioning

The API supports versioning through:
- URL path: `/api/v1.0/products`
- Header: `X-API-Version: 1.0`
- Media type: `Accept: application/json; version=1.0`

## 🔧 Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `POSTGRES_USER` | PostgreSQL username | `postgres` |
| `POSTGRES_PASSWORD` | PostgreSQL password | `postgres123` |
| `POSTGRES_DB` | PostgreSQL database name | `ecommms` |
| `REDIS_CONNECTION_STRING` | Redis connection string | `localhost:6379` |
| `SEQ_SERVER_URL` | Seq server URL | `http://localhost:5341` |

### Rate Limiting

Default configuration:
- **Permit Limit**: 100 requests
- **Window**: 60 seconds
- **Queue Limit**: 2 requests

### CORS

Default allowed origins:
- `http://localhost:3000`
- `http://localhost:4200`

## 📊 Monitoring

### Health Checks

Health check endpoint provides status for:
- Database connectivity
- Redis connectivity

### Logging

Structured logging with:
- Request/Response logging
- Application layer events
- Cache operations
- Database operations
- Error tracking

## 🧪 Testing

Run tests locally:
```bash
dotnet test
```

## 🏗️ Architecture

```
EcomMMS/
├── EcomMMS.Domain/          # Domain entities and interfaces
├── EcomMMS.Application/      # Business logic and CQRS
├── EcomMMS.Infrastructure/   # External services and repositories
├── EcomMMS.Persistence/      # Database context and migrations
├── EcomMMS.API/             # Web API controllers
└── EcomMMS.Tests/           # Unit and integration tests
```

## 🔒 Security

- Environment variables for sensitive data
- Rate limiting to prevent abuse
- CORS configuration for frontend security
- Input validation with FluentValidation

## 📈 Performance

- Redis caching for improved response times
- Connection pooling for database
- Structured logging for monitoring
- Health checks for system status

## What's next ?

- Implement authentication and authorization before production
- Replace monitoring with better options such as sentry, grafana
- For better search experience, use elastic search
- Configure CI/CD (test, code quality etc.)
- Use separated db for each write and read (unless no need to use cqrs pattern)

