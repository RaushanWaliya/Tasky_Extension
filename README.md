# Tasky

Tasky ek Chrome extension hai jo website ke right side me Apollo jaisa panel kholta hai, aur ek ASP.NET Core backend jo 10 external public APIs ka data laata hai. Panel Chrome ka native Side Panel hai: Chrome khud page ko chhota karta hai, isliye website ka layout aur CSS bilkul nahi bigadte.

## Kya milta hai

| Requirement | Kaise pura hua |
| --- | --- |
| Right side me Apollo jaisa tab | Page ke right edge par chhota Tasky tab (aapka logo) |
| Hover karke kahin bhi le jaana | Tab ko pakad kar upar neeche drag karo, position yaad rehti hai |
| Click par screen adjust ho | Chrome ka native Side Panel: browser khud viewport chhota karta hai, website ko pata bhi nahi chalta, layout ya CSS me koi change nahi |
| Click par open, dobara click par close | Tab par click, panel ka X button, Esc, toolbar icon, Alt+Shift+T |
| Panel ki width | Width Chrome control karta hai (Apollo me bhi yahi hai). Panel ke left edge ko drag karke 35% jitna kar do, Chrome use yaad rakhta hai |
| Sirf apni websites par chale | `bumblebot.tech` aur `nexthink.com` (manifest.json me) |
| 8 se 10 tabs, har tab ka alag API | 10 tabs: Weather, Crypto, Currency, Countries, Users, GitHub, Holidays, Hacker News, Todos, Create Post |
| Execute karwana | Create Post tab ek POST request chalata hai |
| Frontend | Simple HTML, CSS, JavaScript (TypeScript nahi) |
| Backend | ASP.NET Core 8 Web API |
| Database | SQL Server |

## Folder structure

```
Tasky/
  extension/            Chrome me yahi folder load hota hai
    manifest.json       permissions, websites, shortcut
    background/         side panel open/close, sirf allowed websites par enable
    content/            launcher.js (right edge ka tab, drag), dock.css (sirf tab ka style)
    panel/              panel ka UI (Chrome ke side panel me khulta hai)
      js/main.js        poori screen ka flow
      js/apiClient.js   backend se baat karne wali ek hi file
      js/formRenderer.js    database ke inputs se form banata hai
      js/resultRenderer.js  list, stats, keyvalue me result dikhata hai
      js/config.js, dom.js, format.js, pathUtil.js
      css/panel.css
    icons/              aapke diye hue icons
  backend/Tasky.Api/    ASP.NET Core 8 Web API
    Controllers/        sirf request lena aur response dena
    Services/           business logic (ProxyService sabse important)
    Repositories/       database queries + UnitOfWork
    Data/               DbContext, seed data
    Domain/Entities/    ApiSource, ApiCallLog
    Dtos/, Mapping/     response ke shapes, manual mapping (AutoMapper nahi)
    Middleware/         saari errors ek jagah handle
    Exceptions/, Options/, Extensions/
  database/schema.sql   SQL Server script (tables + 10 starter APIs)
  tools/mock_backend.js sirf test ke liye nakli backend
  tests/e2e_extension.cjs   real Chromium me automatic test
```

Layers: `Controller` to `Service` to `Repository` to `UnitOfWork` to `DbContext`. Ek layer sirf apne neeche wali layer ko jaanti hai.

## Run kaise karein

### 1. Database

SQL Server Management Studio ya Azure Data Studio me `database/schema.sql` poori run karo. Isse `TaskyDb` database, 2 tables aur 10 APIs ki rows ban jaati hain.

### 2. Backend

`backend/Tasky.Api/appsettings.json` me connection string check karo (default Windows login wali local SQL Server hai):

```
Server=localhost;Database=TaskyDb;Trusted_Connection=True;TrustServerCertificate=True;
```

Phir:

```
cd backend/Tasky.Api
dotnet dev-certs https --trust
dotnet run --launch-profile https
```

Swagger `https://localhost:5081/swagger` par khulega. Check karne ke liye `https://localhost:5081/api/sources` browser me kholo, 10 APIs ki list dikhni chahiye.

SQL Server abhi install nahi hai aur sirf try karna hai? `appsettings.json` me `"Provider": "InMemory"` kar do. Backend bina SQL Server ke starter data ke saath chal jaayega (restart par logs reset hote hain).

### 3. Extension

1. Chrome me `chrome://extensions` kholo.
2. Developer mode ON karo.
3. Load unpacked dabao aur **extension** folder chuno (poora Tasky folder nahi).
4. `https://bumblebot.tech/` ya `https://nexthink.com/` kholo. Right edge par Tasky tab dikhega.

Backend ka URL panel ke upar ⚙ icon se badal sakte ho. Default `https://localhost:5081` hai.

## Apni API kaise lagayein

Code badalne ki zarurat nahi. `api_sources` table me ek row daalo, naya tab apne aap aa jaayega.

```sql
INSERT INTO dbo.api_sources
 (source_key, title, icon, description, sort_order, http_method, url_template,
  headers_json, input_schema_json, display_type, display_config_json, cache_seconds)
VALUES
 (N'orders', N'Orders', N'📦', N'Meri company ke orders', 11, N'GET',
  N'https://api.mycompany.com/orders?status={status}',
  N'{"Authorization":"Bearer MY_SECRET_TOKEN"}',
  N'[{"name":"status","label":"Status","type":"select","required":true,"default":"open","options":["open","closed"]}]',
  N'list',
  N'{"itemsPath":"items","title":"orderNo","subtitle":"customer","value":{"path":"amount","format":"number"},"badge":{"path":"status"}}',
  60);
```

Samjhne ki baatein:

* `{status}` jaisa hissa user ke input se bharta hai. Value URL encode hoti hai aur validate hoti hai (type, select options, pattern, maxLength).
* Secret token headers me backend par rehta hai, browser ya extension me kabhi nahi aata.
* `display_type` teen hain: `list`, `stats`, `keyvalue`. `display_config_json` batata hai response ka kaun sa field kahan dikhana hai (path jaise `company.name` ya `capital[0]`).
* Formats: `number`, `number4`, `percent`, `bool`, `done`, `date`, `upper`.
* POST API ke liye `http_method = POST` aur `body_template` do, jaise `{"title":"{title}"}`.
* Naye API ka website extension me `host_permissions` badalne ki zarurat nahi, kyunki extension sirf aapke backend se baat karta hai.

## Websites badalna ya jodna

`extension/manifest.json` me `content_scripts.matches` aur `web_accessible_resources.matches` dono jagah naya pattern jodo, jaise `"https://*.mysite.com/*"`. Phir chrome://extensions me extension reload karo.

Backend ko jab server par deploy karo, to manifest ke `host_permissions` me uska URL bhi jodo (abhi sirf localhost allowed hai).

## Panel ki width

Native side panel ki width Chrome sambhalta hai, extension nahi. Panel ke left edge ko pakad kar drag karo aur 35% jitna kar do. Chrome ye width yaad rakhta hai, to ek hi baar karna padega.

## Test

Extension ka automatic test real Chromium me chalta hai (nakli backend `tools/mock_backend.js` ke saath). Ye drag, side panel ka sirf allowed websites par enable hona, website ke html aur body me koi change na hona, open aur close ka toggle, 10 tabs, POST execute, validation error, offline message aur strict CSP wali site check karta hai.

```
cd tests
npm install playwright
npx playwright install chromium
SHOT_DIR=. xvfb-run -a node e2e_extension.cjs      # Linux
```

## Dhyan rakhne wali baatein

* Page ko chhota karne ke liye Tasky kuch bhi nahi badalta. Chrome khud window jitna chhota kar deta hai, isliye website apne aap responsive layout dikhati hai.
* Side panel tab ke hisaab se khulta hai: dusre tab par jaoge to wahan panel nahi dikhega, wapas aaoge to dikhega.
* Public APIs ki rate limit hoti hai (jaise CoinGecko). Isliye backend har tab ka result `cache_seconds` tak yaad rakhta hai.
* Har API call `api_call_logs` table me likhi jaati hai. Dekhne ke liye `GET /api/logs`.
