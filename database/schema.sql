/* =====================================================================
   Tasky database script (SQL Server)
   Run this whole file once in SSMS / Azure Data Studio / sqlcmd.
   It is safe to run again: tables are only created when missing and the
   starter API sources are only inserted when the table is empty.
   ===================================================================== */

IF DB_ID(N'TaskyDb') IS NULL
    CREATE DATABASE TaskyDb;
GO

USE TaskyDb;
GO

/* ---------------------------------------------------------------------
   api_sources : one row = one tab in the extension.
   To plug in YOUR OWN API later, just INSERT a new row here. No code change
   and no extension change is needed.
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.api_sources', N'U') IS NULL
CREATE TABLE dbo.api_sources
(
    id               INT IDENTITY(1,1) CONSTRAINT pk_api_sources PRIMARY KEY,
    source_key       NVARCHAR(50)   NOT NULL,              -- short unique name, e.g. 'weather'
    title            NVARCHAR(100)  NOT NULL,              -- tab title
    icon             NVARCHAR(10)   NOT NULL,              -- emoji shown on the tab
    description      NVARCHAR(300)  NOT NULL,
    sort_order       INT            NOT NULL,              -- tab order (small number = left)
    http_method      NVARCHAR(10)   NOT NULL,              -- GET or POST
    url_template     NVARCHAR(1000) NOT NULL,              -- e.g. https://host/path?x={x}
    body_template    NVARCHAR(MAX)  NULL,                  -- JSON body for POST, with {placeholders}
    headers_json     NVARCHAR(MAX)  NOT NULL CONSTRAINT df_api_sources_headers DEFAULT N'{}',
    input_schema_json NVARCHAR(MAX) NOT NULL CONSTRAINT df_api_sources_inputs  DEFAULT N'[]',
    display_type     NVARCHAR(20)   NOT NULL,              -- list | stats | keyvalue
    display_config_json NVARCHAR(MAX) NOT NULL,            -- tells the UI which fields to show
    cache_seconds    INT            NOT NULL CONSTRAINT df_api_sources_cache DEFAULT 0,
    is_active        BIT            NOT NULL CONSTRAINT df_api_sources_active DEFAULT 1,
    created_at_utc   DATETIME2(0)   NOT NULL CONSTRAINT df_api_sources_created DEFAULT SYSUTCDATETIME(),
    CONSTRAINT uq_api_sources_key UNIQUE (source_key),
    CONSTRAINT ck_api_sources_method CHECK (http_method IN (N'GET', N'POST'))
);
GO

/* ---------------------------------------------------------------------
   api_call_logs : audit trail of every call the backend makes for the UI.
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.api_call_logs', N'U') IS NULL
CREATE TABLE dbo.api_call_logs
(
    id               BIGINT IDENTITY(1,1) CONSTRAINT pk_api_call_logs PRIMARY KEY,
    api_source_id    INT            NOT NULL,
    requested_at_utc DATETIME2(0)   NOT NULL,
    duration_ms      INT            NOT NULL,
    status_code      INT            NOT NULL,
    is_success       BIT            NOT NULL,
    from_cache       BIT            NOT NULL,
    error_message    NVARCHAR(500)  NULL,
    client_host      NVARCHAR(200)  NULL,
    CONSTRAINT fk_api_call_logs_source FOREIGN KEY (api_source_id) REFERENCES dbo.api_sources(id)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ix_api_call_logs_requested_at')
    CREATE INDEX ix_api_call_logs_requested_at ON dbo.api_call_logs (requested_at_utc DESC);
GO

/* ---------------------------------------------------------------------
   Starter data : 10 public APIs (all free, no API key).
   --------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.api_sources)
INSERT INTO dbo.api_sources
    (source_key, title, icon, description, sort_order, http_method, url_template, body_template,
     headers_json, input_schema_json, display_type, display_config_json, cache_seconds)
VALUES
    (N'weather', N'Weather', N'🌤', N'Live weather for any latitude and longitude (Open-Meteo, no key needed).', 1, N'GET', N'https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,relative_humidity_2m,apparent_temperature,wind_speed_10m', NULL, N'{}', N'[{"name": "latitude", "label": "Latitude", "type": "number", "required": true, "default": "28.61"}, {"name": "longitude", "label": "Longitude", "type": "number", "required": true, "default": "77.21"}]', N'stats', N'{"fields": [{"label": "Temperature", "path": "current.temperature_2m", "suffix": " °C"}, {"label": "Feels like", "path": "current.apparent_temperature", "suffix": " °C"}, {"label": "Humidity", "path": "current.relative_humidity_2m", "suffix": " %"}, {"label": "Wind", "path": "current.wind_speed_10m", "suffix": " km/h"}, {"label": "Observed at", "path": "current.time"}]}', 300),
    (N'crypto', N'Crypto', N'₿', N'Top coins by market cap (CoinGecko public API).', 2, N'GET', N'https://api.coingecko.com/api/v3/coins/markets?vs_currency={currency}&order=market_cap_desc&per_page=10&page=1&sparkline=false', NULL, N'{}', N'[{"name": "currency", "label": "Currency", "type": "select", "required": true, "default": "usd", "options": ["usd", "inr", "eur"]}]', N'list', N'{"itemsPath": "", "title": "name", "subtitle": "symbol", "subtitleFormat": "upper", "value": {"path": "current_price", "format": "number"}, "badge": {"path": "price_change_percentage_24h", "format": "percent", "tone": "signed"}}', 60),
    (N'currency', N'Currency', N'💱', N'Exchange rates for a base currency (open.er-api.com).', 3, N'GET', N'https://open.er-api.com/v6/latest/{base}', NULL, N'{}', N'[{"name": "base", "label": "Base currency", "type": "select", "required": true, "default": "USD", "options": ["USD", "INR", "EUR", "GBP", "JPY", "AUD"]}]', N'list', N'{"itemsPath": "rates", "entries": true, "limit": 20, "title": "key", "value": {"path": "value", "format": "number4"}}', 600),
    (N'countries', N'Countries', N'🌍', N'Search countries by name (REST Countries).', 4, N'GET', N'https://restcountries.com/v3.1/name/{name}?fields=name,capital,region,population', NULL, N'{}', N'[{"name": "name", "label": "Country name", "type": "text", "required": true, "default": "India", "pattern": "^[A-Za-z .''-]{2,60}$"}]', N'list', N'{"itemsPath": "", "title": "name.common", "subtitle": "capital[0]", "value": {"path": "population", "format": "number", "suffix": " people"}, "badge": {"path": "region"}}', 3600),
    (N'users', N'Users', N'👥', N'Sample user directory (JSONPlaceholder).', 5, N'GET', N'https://jsonplaceholder.typicode.com/users', NULL, N'{}', N'[]', N'list', N'{"itemsPath": "", "title": "name", "subtitle": "email", "badge": {"path": "company.name"}, "value": {"path": "address.city"}}', 600),
    (N'github', N'GitHub', N'🐙', N'Repository statistics (GitHub public API).', 6, N'GET', N'https://api.github.com/repos/{repo}', NULL, N'{"Accept": "application/vnd.github+json"}', N'[{"name": "repo", "label": "Repository (owner/name)", "type": "text", "required": true, "default": "dotnet/aspnetcore", "pattern": "^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$"}]', N'stats', N'{"heading": {"title": "full_name", "subtitle": "description"}, "fields": [{"label": "Stars", "path": "stargazers_count", "format": "number"}, {"label": "Forks", "path": "forks_count", "format": "number"}, {"label": "Open issues", "path": "open_issues_count", "format": "number"}, {"label": "Watchers", "path": "subscribers_count", "format": "number"}, {"label": "Language", "path": "language"}, {"label": "License", "path": "license.spdx_id"}, {"label": "Last push", "path": "pushed_at", "format": "date"}]}', 300),
    (N'holidays', N'Holidays', N'🎉', N'Public holidays by year and country (Nager.Date).', 7, N'GET', N'https://date.nager.at/api/v3/PublicHolidays/{year}/{country}', NULL, N'{}', N'[{"name": "year", "label": "Year", "type": "number", "required": true, "default": "2026", "pattern": "^[0-9]{4}$"}, {"name": "country", "label": "Country", "type": "select", "required": true, "default": "US", "options": ["US", "GB", "DE", "FR", "CA", "AU"]}]', N'list', N'{"itemsPath": "", "title": "name", "subtitle": "localName", "badge": {"path": "date", "format": "date"}}', 3600),
    (N'news', N'Hacker News', N'📰', N'Front page stories (HN Algolia API).', 8, N'GET', N'https://hn.algolia.com/api/v1/search?tags=front_page&hitsPerPage=10', NULL, N'{}', N'[]', N'list', N'{"itemsPath": "hits", "title": "title", "subtitle": "author", "link": "url", "value": {"path": "points", "suffix": " points", "format": "number"}, "badge": {"path": "num_comments", "suffix": " comments"}}', 300),
    (N'todos', N'Todos', N'✅', N'Sample task list (JSONPlaceholder).', 9, N'GET', N'https://jsonplaceholder.typicode.com/todos?_limit=10', NULL, N'{}', N'[]', N'list', N'{"itemsPath": "", "title": "title", "subtitle": "userId", "subtitleFormat": "user", "badge": {"path": "completed", "format": "done", "tone": "bool"}}', 300),
    (N'createpost', N'Create Post', N'✍', N'Execute example: sends a POST request (JSONPlaceholder echoes it back, nothing is stored).', 10, N'POST', N'https://jsonplaceholder.typicode.com/posts', N'{"title":"{title}","body":"{body}","userId":{userId}}', N'{}', N'[{"name": "title", "label": "Title", "type": "text", "required": true, "default": "Hello from Tasky", "maxLength": 100}, {"name": "body", "label": "Body", "type": "textarea", "required": true, "default": "Created through the Tasky extension.", "maxLength": 300}, {"name": "userId", "label": "User id", "type": "number", "required": true, "default": "1", "pattern": "^[0-9]{1,4}$"}]', N'keyvalue', N'{"fields": [{"label": "New post id", "path": "id"}, {"label": "Title", "path": "title"}, {"label": "Body", "path": "body"}, {"label": "User id", "path": "userId"}]}', 0);
GO

SELECT id, source_key, title, http_method, cache_seconds FROM dbo.api_sources ORDER BY sort_order;
GO
