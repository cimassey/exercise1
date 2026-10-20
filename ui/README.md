# ACTS personnel archive UI

The Angular application searches the Stargate API for a person by name and displays their current astronaut information and duty history.

## Requirements

- Node.js 20.19 or newer
- npm 10 or newer
- .NET 8 SDK

## Run locally

Start the API using its HTTPS launch profile:

```powershell
dotnet run --project ..\api\StargateAPI.csproj --launch-profile https
```

In a second terminal, from this directory:

```powershell
npm install
npm start
```

Open `http://localhost:4200`. The Angular development server proxies `/AstronautDuty` requests to `https://localhost:7204`, so no API CORS configuration is needed for local development.

## Validate

```powershell
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```
