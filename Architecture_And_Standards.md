# Architecture & Codebase Standards Documentation

This document outlines the architectural patterns and standards used in our previous projects (e.g., MysteryRooms) to ensure consistency, reliability, and ease of scaling for the AI Business Tycoon project.

## 1. High-Level Architecture
We use a Client-Server Architecture with authoritative game states and robust API integrations.

- Frontend (FE): Built with Unity 3D / Unity 6 in C#. 
- Backend (BE): Built with Python / FastAPI.
- Database: Standardized on NoSQL (MongoDB) or Relational (PostgreSQL).
- Multiplayer/Network Layer: Managed via Unity Netcode for GameObjects (NGO) and orchestrated by Unity Gaming Services (UGS).
- Authentication: Firebase Authentication.

### Interaction Flow
```
[ Unity Client ] ---> (REST/HTTP) ---> [ FastAPI Gateway/API ] ---> [ LLM / Database ]
        |                                       |
    (Netcode)                               (WebSockets for live ops)
        |
[ Host Client (Server) ]
```

## 2. Frontend (Unity) Standards & Patterns

### 2.1 Project Structure
Our Unity Assets/Scripts folder is strictly categorized into domains:
- UI: Canvas, Buttons, Popups, Animations.
- Config: ScriptableObjects holding URLs.
- Data: Plain C# classes for JSON Serialization.
- Game/Managers: Core singletons handling local game states.
- Game/Services: Scripts handling external HTTP requests.
- Authentication: Firebase and UserSession.
- Multiplayer/Core & Network: NGO specific logic.

### 2.2 Configuration Management
We use a ScriptableObject-based BackendConfig to switch between Local and Production environments.

### 2.3 API Integration (UnityWebRequest)
All API calls are housed in dedicated Service classes. 
- Pattern: Using IEnumerator with UnityWebRequest.
- Authentication: Append Firebase tokens to Authorization headers.
- Callbacks: Use Action delegates to return data.

### 2.4 Multiplayer & Session Management
- MultiplayerSessionManager: Handles creation, joining, and disconnection of UGS Lobbies/Sessions.
- UserSession: Holds local user state.
- Networked Objects: Use NetworkBehaviour with Server RPCs and Client RPCs.

## 3. Backend (FastAPI) Standards

### 3.1 Project Structure
- Python dependency management.
- Separation of concerns: api, models, services, core.

### 3.2 Authentication Middleware
The backend must validate the Firebase bearer token sent by Unity using firebase-admin.

### 3.3 Data Contracts (JSON)
Backend responses must map exactly to Unitys Data classes using Pydantic.

### 3.4 API Responses
Always return clear HTTP status codes with a JSON detail key for errors.

## 4. Applying this to AI Business Tycoon

1. MysteryAPIService -> TycoonEconomyService
2. MultiplayerSessionManager: For co-op builds or global server events.
3. GameSessionData -> PlayerTycoonData
4. LLM Integration: Unity sends PlayerTycoonData -> FastAPI sends it to LLM -> LLM returns structured JSON -> Unity parses and updates UI.