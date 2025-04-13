# Weather Dashboard Application

The Weather Dashboard Application is a containerized full-stack porgram built with ASP.NET Core and React. It provides real-time weather information through an intuitive user interface, using multi-threading for efficient data processing. With Docker containerization, the entire project can be used across different environments with minimal setup requirements.

## What does it do?

This application fetches and processes weather data from a free weather API using multi-threading and caching techniques. Key features include:

- API data fetching using time-chunked requests to prevent timeouts
- Multi-threaded data processing for improved performance
- Real-time weather information display
- Historical weather data storage in MySQL database
- RESTful API endpoints for weather data access
- Caching of requested data to speed up subsequest requests
- Interactive React-based user interface for data visualization

## Threading Techniques Used

The application implements several threading techniques to optimize performance and handle concurrent operations:

### 1. Parallel API Request Processing
- Uses `SemaphoreSlim` to limit concurrent API requests to 2 threads to prevent rate limiting
- Implements async/await pattern for non-blocking I/O operations
- Example from [`WeatherService.cs`](WeatherThreading.Server/Services/WeatherService.cs):
```csharp
private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(2);
// Usage in request handling
await _semaphore.WaitAsync();
try {
    return await FetchWeatherDataForTimeRange(...);
} finally {
    _semaphore.Release();
}
```

### 2. Parallel Data Processing
- Utilizes PLINQ (Parallel LINQ) for CPU-intensive calculations
- Automatically scales to available processor cores
- Example from temperature calculation in [`DataProcessor.cs`](WeatherThreading.Server/Utils/DataProcessor.cs):
```csharp
mergedData.Daily["temperature_2m_avg"] = maxTemps
    .AsParallel()
    .WithDegreeOfParallelism(Environment.ProcessorCount)
    .Select((max, index) => (max + minTemps[index]) / 2.0)
    .Cast<object>()
    .ToList();
```

### 3. Concurrent Database Operations
- Implements Entity Framework Core's async methods for database operations
- Uses bulk operations for data insertion
- Handles concurrent database access through EF Core's built-in connection management

### 4. Time-Chunked Request Processing
- Splits large date ranges into manageable chunks
- Processes chunks concurrently while respecting API rate limits
- Merges results using parallel processing

## Coding conventions used

- Backend: PascalCasing
- Frontend: camelCasing

## Requirements

- [Docker](https://docs.docker.com/get-docker/)
- [Docker Compose](https://docs.docker.com/compose/install/)
- Free ports:
  - 8080 (Server API)
  - 3306 (MySQL)
  
## Important Note

Before running the application, ensure no processes are using port 3306 (commonly used by MySQL/MariaDB):

### Windows
1. Open Command Prompt as administrator
2. Run: `netstat -ano | findstr :3306`
3. If any process is using the port, stop it:
   - For MySQL: `net stop mysql`
   - For other processes: `taskkill /PID <PID> /F`

### Linux
1. Open terminal
2. Run: `sudo lsof -i :3306`
3. If any process is using the port, stop it:
   - For MySQL: `sudo systemctl stop mysql`
   - For other processes: `sudo kill <PID>`

## Running the Application

1. Clone the repository:
```bash
git clone https://github.com/ZsomborHajzer/WeatherThreading.git
cd WeatherThreading
```

2. Start the application using Docker Compose:

For Windows:
```powershell
docker-compose up --build
```

For Linux:
```bash
sudo docker-compose up --build
```

The application will be available at:
- Website: http://localhost:8080
- Swagger UI: http://localhost:8080/swagger

## Stopping the Application

To stop the application and remove containers:

For Windows:
```powershell
docker-compose down
```

For Linux:
```bash
sudo docker-compose down
```

## Stopping the application and deleting database volume
To stop the containers without the data persisting from the database run the following:

For Windows:
```powershell
docker-compose down -v
```

For Linux:
```bash
sudo docker-compose down -v
```

## Troubleshooting

If you encounter any issues:

1. Ensure all required ports are available
2. Try removing all containers and images:

For Windows:
```powershell
docker-compose down --rmi all
```

For Linux:
```bash
sudo docker-compose down --rmi all
```

3. Rebuild the application:

For Windows:
```powershell
docker-compose up --build --force-recreate
```

For Linux:
```bash
sudo docker-compose up --build --force-recreate
```

## Contributors
- Zsombor Hajzer
- Alex Schüpbach
- Alicia Rodriguez Benaches
- Clarissa Dobîrcianu