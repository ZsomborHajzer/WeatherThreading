# Weather Dashboard Application

A full-stack weather dashboard application built with ASP.NET Core and React.

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
- Backend API: http://localhost:8080
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
