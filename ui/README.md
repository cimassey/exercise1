# ACTS personnel archive UI

The Angular application searches the Stargate API for a person by name and displays their current astronaut information and duty history.

## Requirements

- Node.js 22.12 or newer (Angular CLI 20 does not run on Node.js 14)
- npm 10 or newer
- .NET 8 SDK

## Run locally

Before installing dependencies, open a new terminal and verify the installed Node.js and npm versions:

```powershell
node --version
npm --version
```

If Node.js is older than 22.12, install a current Node.js 22 LTS release, then close and reopen VS Code so its terminal uses the updated installation. Re-run the version checks; `node --version` should report `v22.12.0` or newer and `npm --version` should report `10` or newer.

Open a terminal in the `exercise1` project root (the parent of `api` and `ui`) and start the API using its HTTPS launch profile:

```powershell
dotnet run --project .\api\StargateAPI.csproj --launch-profile https
```

In a second terminal, change to the `ui` directory and start Angular:

```powershell
cd .\ui
npm install
npm start
```

Open `http://localhost:4200`. The Angular development server proxies `/AstronautDuty` requests to `https://localhost:7204`, so no API CORS configuration is needed for local development.

## Validate

```powershell
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```
