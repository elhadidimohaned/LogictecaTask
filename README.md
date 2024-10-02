# LogecticaTask

This is a .NET application designed to import product data from an Excel file into a local database, manage product information, and export search results back to Excel. The project follows a clean architecture structure, using Entity Framework Core for database operations and ClosedXML for handling Excel files.

## Technologies Used
- **.NET 8**
- **Entity Framework Core**
- **ClosedXML** (for Excel processing)
- **NLog** (for logging)

## Project Structure
The project is organized into layers following clean architecture principles:

- **Domain**: Contains core entities like `Product`.
- **Application**: Contains services and repository interfaces.
- **Infrastructure**: Handles data access and Excel operations.
- **WebUI**: The front-end layer using ASP.NET MVC.

## Setup

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server or another database

### Steps
1. Clone the repository:
   ```bash
   git clone https://github.com/your-username/LogecticaTask.git
   cd LogecticaTask
2. Restore dependencies:
    ```bash
    dotnet restore
3. Update the database connection string in appsettings.json.

4. Apply migrations:
    ```bash
    dotnet ef database update
5. Run the application:
    ```bash
    dotnet run

    
### Database Seeding
To seed the database, the DbSeeder class imports product data from the Excel file located in the Infrastructure/Persistence/ExcelData/ folder. Ensure the file path is correct before running the seeding process.

### Importing and Exporting
Import: Product data is imported from the Excel file during the seeding process.
Export: Filtered product results can be exported back to an Excel file via a service function.

### Logging
The application uses NLog for logging, including error logs, and process information during seeding and file operations.

