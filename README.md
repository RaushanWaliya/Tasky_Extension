# Tasky — Browser Side Panel Extension

Tasky is a Chrome extension that opens a useful side panel on the right side of a website. It also includes an ASP.NET Core Web API backend that connects to external APIs and displays their data inside the extension.

Tasky uses Chrome's native Side Panel, so the website does not need to be modified to display the panel.

## Features

- **Side Panel UI:** Open a panel on the right side of supported websites.
- **Draggable Launcher:** Move the Tasky launcher up or down on the page.
- **Easy Access:** Open or close the panel using the launcher or supported controls.
- **Multiple API Tabs:** View data from 10 different public APIs.
- **Dynamic Forms:** Generate input forms from database configuration.
- **API Execution:** Send requests to external APIs, including supported POST requests.
- **Configurable APIs:** Add new API sources through SQL Server without changing the extension UI code.
- **Caching:** Reduce repeated external API requests using configurable cache durations.
- **API Call Logs:** Store API call information in the database.
- **Backend Error Handling:** Handle errors centrally in the ASP.NET Core backend.
- **Local Development:** Run the backend locally with SQL Server or the configured InMemory provider.

## Technologies Used

| Component | Technology |
|---|---|
| Browser Extension | Chrome Extension Manifest V3 |
| Frontend | HTML, CSS, JavaScript |
| Backend | ASP.NET Core 8 Web API |
| Database | Microsoft SQL Server |
| Data Access | Entity Framework Core / configured data-access layer |
| API Testing | Swagger, Playwright |
| Testing Backend | Node.js mock backend |

## API Integrations

Tasky includes 10 starter API integrations:

1. Weather
2. Cryptocurrency
3. Currency Exchange
4. Countries
5. Users
6. GitHub
7. Holidays
8. Hacker News
9. Todos
10. Create Post

The extension loads its tabs and form configuration from the backend, allowing new API sources to be added through database configuration.

## Project Structure

```text
Tasky/
├── extension/             # Chrome extension
│   ├── manifest.json
│   ├── background/
│   ├── content/
│   ├── panel/
│   └── icons/
├── backend/
│   └── Tasky.Api/         # ASP.NET Core Web API
│       ├── Controllers/
│       ├── Services/
│       ├── Repositories/
│       ├── Data/
│       ├── Domain/
│       ├── Dtos/
│       └── Middleware/
├── database/
│   └── schema.sql         # Database schema and starter API data
├── tools/
│   └── mock_backend.js
└── tests/
    └── e2e_extension.cjs
```

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/RaushanWaliya/Tasky_Extension.git
cd Tasky_Extension
```

### 2. Set Up the Database

- Open `database/schema.sql` in SQL Server Management Studio or Azure Data Studio.
- Execute the script to create the `TaskyDb` database, tables, and starter API configuration.
- Check the connection string in `backend/Tasky.Api/appsettings.json`.

Example local SQL Server connection string:

```text
Server=localhost;Database=TaskyDb;Trusted_Connection=True;TrustServerCertificate=True;
```

### 3. Run the Backend

Open a terminal:

```bash
cd backend/Tasky.Api
dotnet dev-certs https --trust
dotnet run --launch-profile https
```

Open Swagger:

`https://localhost:5081/swagger`

To check the configured API sources, open:

`https://localhost:5081/api/sources`

### 4. Load the Chrome Extension

1. Open `chrome://extensions` in Chrome.
2. Enable **Developer mode**.
3. Click **Load unpacked**.
4. Select the project's `extension` folder.
5. Open a supported website, such as `https://bumblebot.tech/`.

The Tasky launcher should appear on the page's right edge.

The backend must be running for API data to load. The extension's backend URL can be configured through its settings, where supported.

## Adding a New API

Tasky supports configurable API sources.

To add a new API:

1. Add a new row to the `dbo.api_sources` table.
2. Configure its title, HTTP method, URL, headers, input schema, and display settings.
3. Restart or refresh components if required by the current implementation.
4. Open the extension and check the new API tab.

Supported display types:

- `list`
- `stats`
- `keyvalue`

**Security:** Store API secrets on the backend, not in the browser extension. Do not commit real API keys, passwords, or sensitive connection strings to GitHub.

## Architecture

Tasky follows a layered backend architecture:

```text
Chrome Extension
       |
       v
ASP.NET Core Web API
       |
       v
Controller
       |
       v
Service
       |
       v
Repository / Unit of Work
       |
       v
DbContext
       |
       v
SQL Server
```

The backend retrieves API configuration, validates inputs, calls the configured external APIs, handles errors, and records API call logs.

## Testing

Tasky includes an end-to-end test using Playwright and Chromium.

The test setup uses a mock backend to verify extension behavior without relying on live external APIs.

See `tests/e2e_extension.cjs` for the test implementation.

## Important Notes

- The extension currently supports only the website patterns configured in `extension/manifest.json`.
- Chrome controls the native Side Panel's width.
- Public APIs may have rate limits or availability issues.
- SQL Server is required for the SQL-backed configuration; the InMemory provider can be used for local experimentation if supported by the current backend configuration.
- Before deploying to production, configure secure credentials, HTTPS, API restrictions, and appropriate access controls.

## Author

**Raushan Waliya**

GitHub: [RaushanWaliya](https://github.com/RaushanWaliya)

## License

No license has been specified yet. Add a `LICENSE` file before presenting this repository as open-source software.

## Screenshots

### Tasky Browser Side Panel
![Tasky Side Panel](docs/screenshots/tasky-panel.png)

### API Data
![Tasky API Data](docs/screenshots/tasky-api-data.png)

### Backend Swagger
![Tasky Swagger](docs/screenshots/tasky-swagger.png)

