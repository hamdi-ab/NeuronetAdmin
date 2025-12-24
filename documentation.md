# NEURONET: Admin & Counselor Module
**Course**: Event Driven Programming (Senior Project Part)
**Academic Year**: V | **Semester**: I

---

## 📄 Module Description
The NEURONET Admin Module is a centralized system for managing a mental health support platform. It implements secure authentication flows and a specialized verification system for professional counselors. This project focuses on **Event-Driven interactions** where administrative decisions (Approve/Reject) trigger cascade events like automated account provisioning.

---

## 🛠️ Implemented Functionalities (Full CRUD)

This project implements two core administrative modules, both supporting **Full CRUD** (Create, Read, Update, Delete) operations as required by the guidelines.

### 1. User Account Management (Module 1)
*   **Create**: Manual registration of administrative and staff accounts via a secure form.
*   **Read**: Real-time list view with fuzzy search by name/email and role-based filtering.
*   **Update**: Full profile editing capabilities including role changes and account status toggling.
*   **Delete**: Soft and hard deletion support with **Self-Deletion Prevention** logic to ensure system stability.

### 2. Counselor Verification System (Module 2)
*   **Create**: Dual-entry system—allows both public "Apply" requests and manual Admin "Create" requests.
*   **Read**: A specialized dashboard that highlights pending requests using conditional styling.
*   **Update**: Dedicated **Edit** functionality for modifying record data, plus state-change actions (**Approve/Reject**) that trigger backend user creation events.
*   **Delete**: Management of rejected or expired professional records.

---

## 🎨 HCI & Usability (HCI Perspective)
Evaluated based on **Usability Metrics**:
*   **Efficiency**: Minimized clicks via a "Hero" section that provides direct paths to login or registration.
*   **Safety**: Destructive actions (Delete) use disabled button states for active admins to prevent accidental lockout events.
*   **Feedback**: **TempData-driven notifications** provide immediate confirmation of every successful or failed action.
*   **Visibility of System Status**: Status badges (Pending, Verified, Rejected) and table row color-coding provide instant situational awareness.
*   **Aesthetics**: Glassmorphism design and the **Inter Google Font** ensure a premium, clinical, and trustworthy appearance.

---

## 🛡️ Exception Handling & Validation
*   **Form Validation**: Leverages ASP.NET Core **Data Annotations** (Required, EmailAddress, Compare) for both client and server-side validation.
*   **Model Bound Checking**: Every Controller action validates `ModelState.IsValid` before persistence.
*   **Security**: CSRF protection implemented on all state-changing actions using `[ValidateAntiForgeryToken]`.
*   **Errors**: User-friendly error messages are propagated via `TempData` to ensure the user is never left without feedback during a failure.

---

## 💻 How to Run the Project

### Prerequisites
1.  **SDK**: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2.  **Database**: SQL Server / LocalDB.

### Setup Instructions
1.  **Update Database**:
    ```bash
    dotnet ef database update
    ```
2.  **Run Application**:
    ```bash
    dotnet run
    ```
3.  **Access**: [http://localhost:5108](http://localhost:5108)

---

## 🎤 Presentation Highlights (For Evaluation)
*   **Point 1**: Show how the **"Apply"** form creates an *Inactive* user, and clicking **"Approve"** *Activates* them.
*   **Point 2**: Demonstrate **Self-Deletion Prevention** by showing that the "Delete" button is hidden for your own account.
*   **Point 3**: Highlight the **Search/Filter** capabilities in the User Management list.

---
*Last Updated: December 2025*
