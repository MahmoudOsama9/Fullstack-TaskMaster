# Project TaskMaster: Full-Stack Collaborative Task Management System

<p align="center">
  <img src="https://via.placeholder.com/800x400.png/111827/FFFFFF?text=Project+TaskMaster+Screenshot" alt="Project TaskMaster Screenshot">
  <!-- After you run the app, take a nice screenshot of your dashboard, upload it somewhere (like the GitHub repo itself), and replace the placeholder URL above! -->
</p>

## Overview

Project TaskMaster is a comprehensive, enterprise-grade full-stack application for collaborative task management, built from the ground up to demonstrate a wide range of modern software development technologies and architectural best practices.

The backend is architected as a **polyglot API**, serving data through multiple, distinct styles to cater to different client needs and use cases. The frontend is a responsive, feature-rich Single-Page Application (SPA) built with modern, standalone Angular components.

### Key Features

- **Full-Stack Application:** .NET 8 Backend with an Angular 17+ Frontend.
- **Complete CRUD Functionality:** Create, Read, Update, and Delete projects and tasks.
- **Secure Authentication:**
  - Robust local authentication with **JWT and Refresh Tokens**.
  - External social login via **"Continue with Google" (OAuth2)**.
- **Multi-Tenancy:** Data is partitioned per user; users can only view and manage projects they own or are a member of.
- **Real-time Collaboration:** Live updates are pushed to all connected clients using **WebSockets (via SignalR)** when project or task data changes.
- **High-Performance Caching:** Implemented a **Redis** cache-aside pattern on the backend for frequently accessed data.
- **API Hardening & Security:** Integrated ASP.NET Core **Rate Limiting** to prevent abuse and brute-force attacks.
- **Professional Observability:** Features structured logging with **Serilog** and centralized log management with **Seq** for powerful querying and analysis.
- **Advanced API Styles:**
  - **REST:** For standard client-server communication.
  - **GraphQL:** For flexible, efficient data fetching.
  - **gRPC:** For high-performance, low-latency internal service communication.
- **Foundation for Enterprise Auth:** Includes **OpenIddict** configuration, establishing the backend as a full OAuth2/OpenID Connect Authorization Server.

## Technology Stack

| Category          | Technology                                   |
| ----------------- | -------------------------------------------- |
| **Backend**       | .NET 8, C#, ASP.NET Core, Entity Framework Core |
| **Frontend**      | Angular, TypeScript, SCSS                      |
| **Database**      | MS SQL Server                                  |
| **API Styles**    | REST, GraphQL, gRPC, WebSockets (SignalR)      |
| **Authentication**| JWT, Refresh Tokens, OAuth2 (Google), OpenIddict |
| **Caching**       | Redis                                          |
| **Logging**       | Serilog & Seq                                  |
| **Containerization**| Docker, Kubernetes, NGINX                      |
| **Monitoring**    | Prometheus & Grafana                           |

## Getting Started

### Prerequisites

- .NET 8 SDK
- Docker Desktop
- Node.js (v18+) and Angular CLI
- An IDE like Visual Studio 2022 or VS Code

### 1. Backend Setup (Folder: `ProjectTaskMaster`)

First, start the required infrastructure services using Docker.

```bash
# Start MS SQL Server
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=yourStrong(!)Password" -p 1433:1433 --name sql_server_taskmaster -d mcr.microsoft.com/mssql/server:2022-latest

# Start Redis
docker run --name redis_taskmaster -p 6379:6379 -d redis

# Start Seq
docker run --name seq_taskmaster -e ACCEPT_EULA=Y -p 5341:80 -d datalust/seq:latest