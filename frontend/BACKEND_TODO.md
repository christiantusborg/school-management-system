# Backend TODO — IBSS Partner Programme Workflow

This document tracks backend work required to make the partner programme approval workflow production-ready.
All features listed here are currently implemented as in-memory mocks in the frontend only — data is lost on page refresh.

---

## 1. Partner Programme API

| Endpoint | Method | Description |
|----------|--------|-------------|
| `GET /api/partners/:id/programmes` | GET | List all programme clones for a partner |
| `POST /api/partners/:id/programmes` | POST | Create a new programme clone (from a core programme ID) |
| `PUT /api/partners/:id/programmes/:cloneId` | PUT | Update a programme clone (name, majors, subjects) |
| `DELETE /api/partners/:id/programmes/:cloneId` | DELETE | Delete a draft/rejected clone |
| `POST /api/partners/:id/programmes/:cloneId/submit` | POST | Partner marks clone as "ready for approval" (status → pending) |

## 2. Admin Approval API

| Endpoint | Method | Description |
|----------|--------|-------------|
| `GET /api/admin/partner-programmes` | GET | List all partner programme clones across all partners, filterable by status |
| `POST /api/admin/partner-programmes/:cloneId/approve` | POST | Approve a partner clone (status → approved) |
| `POST /api/admin/partner-programmes/:cloneId/reject` | POST | Reject with a reason (status → rejected, rejectionReason set) |

## 3. Enrolment Validation

- **Server-side**: When a partner enrols a student in a programme, the backend must validate that the programme is either:
  - An IBSS core programme (always allowed if partner has access)
  - A partner clone with `status === 'approved'`
- Currently the frontend hides/disables unapproved programmes in dropdowns, but there is no server-side check.

## 4. Programme Clone Data Model (DB schema)

```
PartnerProgramme {
  id            UUID (PK)
  partnerId     UUID (FK → Partner)
  srcProgId     String (core programme ID reference)
  name          String
  code          String
  status        Enum: draft | pending | approved | rejected
  rejectionReason String? (null unless rejected)
  createdAt     DateTime
  updatedAt     DateTime
  majors        PartnerMajor[]
}

PartnerMajor {
  id            UUID (PK)
  programmeId   UUID (FK → PartnerProgramme)
  srcMajId      String? (core major ID, null for custom)
  name          String
  subjects      PartnerSubject[]
}

PartnerSubject {
  id            UUID (PK)
  majorId       UUID (FK → PartnerMajor)
  name          String
  credits       Int
}
```

## 5. Auth & Sessions

- Current mock: plaintext username/password checked against in-memory array
- Need: JWT or session-based auth, password hashing (bcrypt), refresh tokens
- Partner portal must only see their own programmes/students (server-enforced, not just frontend filtering)

## 6. Notifications (nice to have)

- Email to IBSS admin when partner submits a programme for approval
- Email to partner when admin approves or rejects their programme
- In-app notification badge (currently shown as a tab badge on the admin Programmes page)

## 7. Admin Programme Management Page

- A dedicated admin page (not yet built) to:
  - List all partner programmes grouped by status
  - Filter by partner, status, date submitted
  - Bulk approve/reject
  - Currently implemented as part of the "Partner Programmes" tab in `ProgrammesView.vue`

## 8. Persistence for All Other Mock Data

The following are also mocked in-memory and need real persistence:
- Core programmes (IBSS admin CRUD)
- Students and enrolments
- Grades
- Absences
- Support tickets
- Announcements

---

_Last updated: 2026-04-15_
_Frontend mock implementation complete. Backend integration pending._
