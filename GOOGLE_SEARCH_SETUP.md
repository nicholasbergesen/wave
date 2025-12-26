# Google Search API Setup

This document describes how to configure the Google Custom Search API for the Wave chat assistant.

## Prerequisites

1. A Google Cloud Platform account
2. A Google Custom Search Engine (CSE) created

## Setup Steps

### 1. Enable the Custom Search API

1. Go to the [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Navigate to "APIs & Services" > "Library"
4. Search for "Custom Search API"
5. Click "Enable"

### 2. Create an API Key

1. In the Google Cloud Console, navigate to "APIs & Services" > "Credentials"
2. Click "Create Credentials" > "API Key"
3. Copy the generated API key
4. (Optional but recommended) Click "Restrict Key" and limit it to the Custom Search API

### 3. Create a Custom Search Engine

1. Go to [Programmable Search Engine](https://programmablesearchengine.google.com/)
2. Click "Add" to create a new search engine
3. Configure your search engine:
   - **Sites to search**: You can search the entire web by selecting "Search the entire web"
   - **Name**: Give it a descriptive name (e.g., "Wave Assistant Search")
4. Click "Create"
5. Copy your **Search Engine ID** (also known as `cx` parameter)

### 4. Configure the Application

You have two options to configure the API keys:

#### Option A: Using User Secrets (Recommended for Development)

```bash
cd wave.web/wave.web
dotnet user-secrets init
dotnet user-secrets set "GoogleSearch:ApiKey" "YOUR_API_KEY_HERE"
dotnet user-secrets set "GoogleSearch:SearchEngineId" "YOUR_SEARCH_ENGINE_ID_HERE"
```

#### Option B: Using appsettings.Development.json (Not Recommended - Do Not Commit)

Edit `appsettings.Development.json`:

```json
{
  "GoogleSearch": {
    "ApiKey": "YOUR_API_KEY_HERE",
    "SearchEngineId": "YOUR_SEARCH_ENGINE_ID_HERE"
  }
}
```

**WARNING**: Do NOT commit your API keys to the repository. The `appsettings.Development.json` file should not contain actual keys in version control.

#### Option C: Using Environment Variables (Recommended for Production)

Set the following environment variables:

```bash
export GoogleSearch__ApiKey="YOUR_API_KEY_HERE"
export GoogleSearch__SearchEngineId="YOUR_SEARCH_ENGINE_ID_HERE"
```

Or on Windows:
```cmd
set GoogleSearch__ApiKey=YOUR_API_KEY_HERE
set GoogleSearch__SearchEngineId=YOUR_SEARCH_ENGINE_ID_HERE
```

Note: Double underscore (`__`) is used to represent nested configuration in environment variables.

## Usage Quotas

- **Free tier**: 100 queries per day
- **Paid tier**: Enable billing in Google Cloud Console for higher quotas

## Testing the Configuration

The application will log warnings if the Google Search Service is not configured properly. Check the logs when starting the application to verify the configuration is loaded correctly.

The chat controller will automatically attempt to use web search when appropriate based on the conversation context.
