# Campus Facility Booking and Maintenance Tracker

Course project for **SE410, Spring 2025-26**.

A line-of-business system for managing campus facility reservations (labs, classrooms, meeting rooms) and the maintenance work that keeps them running. In the long-term plan, students and staff use a web app to find and book rooms while facility managers and maintenance personnel use a Windows desktop app to approve bookings, assign maintenance, and generate logs. Both clients talk to a shared Web API backed by a single SQL Server database.

**Project members:**

Muhammed (Arda) SEZAİ
Aral CAVLAK
Ahmet Seçkin BÜYÜKAVCU
Selin Sinem ERGÜL

---

## V1 Release Notes

V1 is the first graded deliverable (due 2026-05-06). It focuses on getting the database, the API, and the desktop client working end to end. The web MVC project exists in the solution but is not part of V1.

### What V1 includes

**Database integration**

* SQL Server schema (LocalDB on Windows) with `Facilities`, `FacilityTypes`, `Bookings`, `MaintenanceIssues`, `MaintenanceStatusHistory`, `Notifications`, plus the standard ASP.NET Identity tables.
* The schema is built manually from `sql/CampusBooking-Schema.sql`. Entity Framework only reads the tables; it never creates or migrates them.
* A filtered unique index on `(FacilityId, Date, TimeSlot)` prevents double-booking at the database level.

**Basic CRUD**

* Facility types and facilities (create, edit, deactivate) through the API and the desktop Facilities form.
* Users and roles (create, list, disable, reset password) through the API; managed in the desktop Users tab.
* Bookings (create, cancel, modify, approve, reject) through the API; managed from the desktop Bookings tab.
* Maintenance issues (report, assign, transition status) through the API and the desktop Maintenance form.

**LINQ**

* Availability search filters and sorts free slots from `Bookings` and `Facilities`.
* Conflict check on reservation skips cancelled and rejected rows so a freed slot can be reused.
* Maintenance log aggregation filters by facility, date range, status, and assignee.

**Delegates + event handlers**

* A shared `NotificationHandler` delegate raised by `NotificationService` on five events: booking confirmed, booking approved, booking rejected, booking cancelled, maintenance assigned, maintenance status changed.
* The desktop client subscribes to that delegate to show toast popups and update the inbox count without polling logic in the form code.

**WinForms GUI**

* `LoginForm` for authentication.
* `MainDashboardForm` host shell with tabs for Bookings, Facilities, Users, and Maintenance.
* `MaintenanceForm` for reporting issues, assigning personnel, and exporting CSV logs through `FileStream`.

**JWT authentication**

* `POST /api/auth/login` returns an 8-hour bearer token with role claims.
* Every non-anonymous endpoint enforces role authorization. There is no public self-registration.

**Audit columns**

* `Bookings` carries `CreatedBy/At`, `ApprovedBy/At`, `RejectedBy/At`, `CancelledBy/At`.
* `MaintenanceIssues` carries `ReportedBy/At`, `AssignedBy/At`, `AssignedAt`, `ResolvedAt`.
* `MaintenanceStatusHistory` records every status transition with the user and timestamp.

**Photo upload backend**

* `POST /api/maintenance` accepts a multipart form. The API writes the file to `Api/wwwroot/uploads/{yyyy}/{MM}/` and stores the relative path in the database. JPEG, PNG, and WebP are accepted, with a 5 MB cap.

### What is deferred to V2

The following pieces are scaffolded in the codebase or in the FR list but are not part of the V1 release:

* The **web MVC client** (`CampusBooking.Web`). Search, booking, maintenance reporting, and the notification banner from a browser are V2.
* The **scheduler dashboard** (the day and week calendar grid with auto-refresh).
* The **profile page** (web).
* A dedicated **InboxForm** in the desktop client (notifications still appear as toasts in V1).
* A dedicated **UsersForm** as a standalone screen (user management still works through the Users tab in `MainDashboardForm`).
* A dedicated **PendingApprovalsForm** (approve/reject still works through the Bookings tab in `MainDashboardForm`, just without a separate workflow screen).

---

## Prerequisites

| Requirement | Notes |
|---|---|
| .NET 8 SDK | Download from https://dotnet.microsoft.com/download/dotnet/8.0 |
| SQL Server | Windows: SQL Server LocalDB (ships with Visual Studio). macOS / Linux: Docker `mcr.microsoft.com/mssql/server:2022-latest` or any SQL Server 2017+ instance. |
| IDE | Visual Studio 2022 (recommended on Windows for the WinForms designer), VS Code, or JetBrains Rider |

The desktop project targets `net8.0-windows` and only runs on Windows. The API and the (V2) web project run on any OS.

---

## Setup (one command)

From the repository root:

**Windows (PowerShell):**

```powershell
pwsh tools/setup.ps1
```

**macOS / Linux (bash):**

```bash
./tools/setup.sh
```

The script restores packages, applies the schema, runs `dotnet run -- seed --test` against the API, and prints the URL the API will listen on.

If you are not on Windows, set the connection string before running the script:

```bash
export CFB_API_CONNECTIONSTRING="Server=localhost,1433;Database=CampusBooking;User Id=sa;Password=Your_Password123;TrustServerCertificate=True"
```

The API reads `CFB_API_CONNECTIONSTRING` if it is set; otherwise it falls back to the LocalDB connection string in `appsettings.json`.

---

## Manual setup (if scripts fail)

1. Clone the repository and open `src/CampusBooking.sln` in your IDE.
2. Open SSMS (or `sqlcmd`) and connect to `(localdb)\mssqllocaldb`. On macOS or Linux, connect to your own SQL Server.
3. Create a database named `CampusBooking`.
4. Run the DDL in `sql/CampusBooking-Schema.sql` against the new database.
5. Restore and build:

```bash
dotnet build src/CampusBooking.sln
```

6. Seed base rows plus test data:

```bash
dotnet run --project src/CampusBooking.Api -- seed --test
```

The seed command runs the seeder, then exits without starting the HTTP server.

7. Start the API:

```bash
dotnet run --project src/CampusBooking.Api
```

The API listens on `https://localhost:7XXX` and `http://localhost:5XXX` (the exact ports come from `Properties/launchSettings.json`). Swagger is available at `/swagger`.

8. Start the desktop client (Windows only):

```bash
dotnet run --project src/CampusBooking.Desktop
```

---

## Default credentials

After running `seed --test`:

| Email | Password | Role |
|---|---|---|
| `admin@campus.local` | `Admin!23` | FacilityManager |
| `student1@campus.local` | `Pass!23` | Student |
| `staff1@campus.local` | `Pass!23` | Staff |
| `manager1@campus.local` | `Pass!23` | FacilityManager |
| `mp1@campus.local` | `Pass!23` | MaintenancePersonnel |

Running `seed` without `--test` only creates the admin account.

---

## What each feature does

**Booking with auto-confirm vs approval flow**

When a student or staff member submits a reservation, the API runs a LINQ conflict check against existing bookings on the same facility, date, and hour. Reservations for Classrooms and Meeting Rooms are auto-confirmed. Reservations for Labs are saved as `Pending` so a Facility Manager can approve or reject them on the desktop. If two requests collide on the same slot, the database unique index rejects the second one and the API returns HTTP 409.

**Cancellation window**

Users can cancel their own reservations up to two hours before the slot starts. After that cutoff the API rejects the request. Cancellation immediately frees the slot (the unique index is filtered to ignore cancelled and rejected rows) and fires a notification to the Facility Manager.

**Maintenance reporting with photo, assignment, and CSV log**

A user submits a maintenance issue with a description, severity, and an optional photo. The photo is uploaded as multipart and the API stores it under `Api/wwwroot/uploads/{yyyy}/{MM}/`. A Facility Manager assigns the issue to a Maintenance Personnel user; the assignee transitions the issue through `Pending`, `In Progress`, and `Resolved`. Every transition is recorded in `MaintenanceStatusHistory`. Managers can export the log filtered by facility, date range, status, and assignee to a CSV file using a `FileStream`-backed writer.

**In-app notifications via delegate**

The API persists a `NotificationRecord` for each event and exposes `GET /api/notifications/unread`. The desktop client polls that endpoint every 15 seconds. When new notifications arrive, the shared `NotificationHandler` delegate fires; subscribers display a toast popup and refresh the inbox count. There is no SignalR or WebSocket layer; the polling interval was chosen to keep V1 simple.

---

## Limitations / known issues

* V1 is desktop-only. There is no web client yet; the `CampusBooking.Web` project compiles but is not wired up in V1.
* No scheduler grid. The day and week calendar dashboard is planned for V2.
* On Windows, the API uses an HTTPS dev certificate. If your browser or the desktop client cannot reach the API, run `dotnet dev-certs https --trust` once and restart the terminal.
* SQL Server LocalDB is Windows-only. macOS and Linux users need their own SQL Server (Docker image works fine) and must set `CFB_API_CONNECTIONSTRING`.
* The API ships with a self-signed certificate in development. The desktop client is configured to accept it. This is acceptable only for local development; do not use these settings in a real deployment.

---

## Project Specification

The full requirement set from the course brief is preserved below for reference. Items marked V2 above are still listed here so the spec stays intact.

### Functional Requirements

#### FR1 Role-Based Access with Admin-Provisioned Accounts
The system shall support four roles (Student, Staff, Facility Manager, Maintenance Personnel) enforced via ASP.NET Core Identity. Facility Managers shall create all accounts through the desktop application; no public self-registration is permitted. Every Web API endpoint shall enforce role authorization before executing any action.

#### FR2 Facility and Facility Type Management
Facility Managers shall be able to create, edit, and deactivate facility types (e.g., Lab, Classroom, Meeting Room, or any custom type) and individual facilities from the desktop application. Each facility shall record a name, type, capacity, and location, persisted through the Web API to the shared SQL Server database.

#### FR3 Facility Availability Search
Students and Staff shall search for available facilities from the web application by date, hourly time slot (08:00–20:00), facility type, and minimum capacity. The API shall return results sorted by facility name and availability.

#### FR4 Facility Reservation with Conditional Approval Flow
Students and Staff shall reserve facilities by selecting a facility and one or more hourly slots on a chosen date. Reservations for Classrooms and Meeting Rooms shall be auto-confirmed if a LINQ-based conflict check finds no overlap; reservations for Labs shall be created in Pending status until a Facility Manager explicitly approves or rejects them on the desktop.

#### FR5 Booking Cancellation and Modification Window
Users shall be able to cancel or modify their own reservations up to 2 hours before the slot start time. Cancellations shall immediately free the slot in the database, and a delegate-based notification shall inform the Facility Manager of the change.

#### FR6 Delegate-Based In-App Notification System
The solution shall define a shared NotificationHandler delegate type, raised on five events: booking confirmed, booking approved/rejected, booking cancelled, maintenance assigned, maintenance status changed. The same delegate mechanism shall drive toast popups + an inbox list in the WinForms desktop and equivalent banners + an inbox page in the MVC web application, with no duplicated event logic.

#### FR7 Maintenance Issue Reporting with Photo Attachment
Students and Staff shall submit maintenance issue reports from the web application, providing a target facility, description, severity (Low/Medium/High/Critical), and an optional photo upload. The photo shall be transmitted as a multipart form and stored on the API server, with the file path recorded in the database.

#### FR8 Manual Maintenance Task Assignment
Facility Managers shall view all open issue reports on the desktop application and manually assign each one to a specific Maintenance Personnel user. Upon assignment, the notification delegate shall immediately fire an alert to the assignee and record an audit entry with the timestamp and assigning manager.

#### FR9 Maintenance Status Tracking and CSV Log Generation
Maintenance Personnel shall update each assigned task through the states Pending → In Progress → Resolved from the desktop application, with every transition timestamped. Facility Managers shall generate maintenance logs filtered by facility, date range, status, and assignee using LINQ aggregation; the resulting log shall be displayed in a grid and exportable to CSV through a FileStream-based writer.

#### FR10 Desktop Resource Scheduler Dashboard
The desktop application shall provide a day-and-week calendar grid (facilities on one axis, hourly time slots on the other) rendering all confirmed bookings and scheduled maintenance windows, color-coded by status. The grid shall support LINQ-backed filters for facility type, status, and date range, and shall refresh automatically every 30 seconds plus on demand via a Refresh button.

### Non-Functional Requirements

#### NFR1 Performance
Under typical academic load (≤100 facilities, ≤1,000 active bookings, ≤20 concurrent users), every LINQ-backed search, availability check, and log aggregation shall return within 2 seconds end-to-end, and each Web API call shall complete within 1 second of server processing time.

#### NFR2 Security
All passwords shall be stored as salted hashes via ASP.NET Core Identity (never plaintext). The Web API shall require a valid bearer token on every non-anonymous endpoint, reject requests failing role authorization with HTTP 403, and serve all production traffic over HTTPS.

#### NFR3 Data Integrity and Reliability
The database schema shall enforce a uniqueness constraint on the combination (FacilityId, Date, TimeSlot) so that concurrent reservation attempts cannot double-book the same slot; conflicting writes shall return HTTP 409 and surface a clear message to the user. Desktop and web views shall reflect any committed change within 30 seconds.

#### NFR4 Real-Time Data Consistency Across Platforms
The shared SQL Server database shall serve as the single source of truth for all bookings, maintenance records, facility data, and user accounts. Any change committed through one application (web or desktop) shall be retrievable through the Web API by the other application within 30 seconds, with no manual synchronisation step, fulfilling the "seamless data management and real-time updates" requirement of the project specification.

#### NFR5 Auditability of Bookings and Maintenance Actions
Every booking creation, approval or rejection, cancellation, maintenance issue report, maintenance assignment, and status transition shall be persisted with the acting user's ID and a UTC timestamp, retained for the full academic term, and retrievable by a Facility Manager through the Web API for display and CSV export in the desktop log generator.

---

## Application Scenario

A mid-sized university campus operates ~80 bookable facilities across several buildings. Students and staff reserve rooms through the web app; facility managers and maintenance personnel work through the desktop app.

### Sequence A - Reservation (auto-confirm)
1. A student signs in to the web app and opens *Find a Facility*.
2. They filter by date, hourly slot, facility type *Meeting Room*, capacity ≥ 6.
3. The API returns the matching free rooms sorted by name.
4. The student picks one and confirms.
5. The API runs a final conflict check, inserts the booking as `Confirmed`, audits it, and fires `BookingConfirmed`.

### Sequence B - Reservation (manager approval)
1. A staff member reserves a Lab for two consecutive hours.
2. The API creates the booking as `Pending` (Labs require approval).
3. A Facility Manager sees a toast on the desktop, opens *Pending Approvals*, and approves it.
4. The API marks it `Approved`, audits it, and fires `BookingApproved`.

### Sequence C - Cancellation
1. The student cancels their booking from the profile page, more than 2 hours before the slot.
2. The API removes the booking, frees the slot, audits, and fires `BookingCancelled`.

### Sequence D - Maintenance reporting & assignment
1. A staff member reports a broken projector in a classroom and uploads a photo.
2. The API stores the photo on disk, records the issue as `Open`, and audits it.
3. A Facility Manager assigns the issue to a Maintenance Personnel user; the delegate fires `MaintenanceAssigned`.

### Sequence E - Maintenance resolution & logging
1. The Maintenance Personnel sets the task to `In Progress`, performs the repair, and sets it to `Resolved`. Each transition is timestamped.
2. End of week, the Facility Manager filters the log by date range and status, then exports CSV via `FileStream`.

### Sequence F - Resource scheduler dashboard
1. The Facility Manager keeps the desktop *Scheduler Dashboard* open during the workday.
2. The grid (facilities × hourly slots) shows all bookings + maintenance windows, color-coded by status.
3. It auto-refreshes every 30 seconds plus on demand via a Refresh button.
