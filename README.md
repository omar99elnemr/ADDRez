# ADDRez

**Restaurant Reservation & Guest Management Platform**

A modern B2B SaaS application for hospitality venues — manage reservations, floor plans, guest lists, CRM, campaigns, and more. Built with a dark luxury aesthetic and dual theme support.

## Tech Stack

| Layer | Technology |
|---|---|
| **Backend** | ASP.NET Core 9, Entity Framework Core 9, SignalR |
| **Frontend** | Vue 3, TypeScript, PrimeVue 4, Tailwind CSS 4 |
| **Database** | PostgreSQL 17 |
| **Mobile** | Capacitor 8 (iOS + Android) |
| **Auth** | JWT Bearer tokens |
| **Real-time** | SignalR (WebSocket) |

## Features

- **Multi-venue & multi-branch** with tenant isolation
- **Reservation management** — full lifecycle with state machine
- **Interactive floor plan** — drag-and-drop table editor (Konva canvas)
- **Customer CRM** — profiles, categories, tags, notes, blacklist
- **Guest lists** — per-reservation with QR check-in
- **Time slot configuration** — capacity rules, deposits, category exclusions
- **Email campaigns** — audience targeting, scheduling, analytics
- **Reports & analytics** — summary, reservation, customer reports with export
- **Gate checker** — QR code scanning for event arrival management
- **Public booking form** — embeddable guest-facing widget
- **Role-based access** — 44 permissions, 5 system roles
- **Dual theme** — Midnight Luxury (dark) + Golden Day (light)
- **i18n** — English + Arabic (RTL)
- **Real-time updates** — live floor plan, reservation status via SignalR

## Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 20+](https://nodejs.org/)
- [PostgreSQL 17](https://www.postgresql.org/download/)

### Backend

```bash
cd server/src/ADDRez.Api
dotnet restore
# Update connection string in appsettings.json if needed
dotnet ef database update
dotnet run
# API starts at https://localhost:5001
```

### Frontend

```bash
cd client
npm install
npm run dev
# App starts at http://localhost:5173
```

### Demo Credentials

| Role | Username | Password |
|---|---|---|
| Super Admin | admin | password |
| Manager | sarah.mgr | password |
| Host | ahmed.host | password |
| Gate Checker | fatima.gate | password |

## Deployment

Designed for **IIS on Windows Server**:
- ASP.NET Core runs natively via ANCM (ASP.NET Core Module)
- Vue SPA served as static files with SPA fallback
- PostgreSQL or SQL Server as production database
- SignalR runs in-process — no extra WebSocket server needed

## License

Proprietary — All rights reserved.
