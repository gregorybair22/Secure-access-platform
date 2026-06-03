# User Manual (Technician)

Secure Customer Access and Credential Management Platform

Audience: support technicians who need to retrieve customer credentials to perform
their work. Every credential reveal is **justified, logged, and auditable**.

---

## 1. Signing in

1. Open the platform URL in your browser (HTTPS).
2. Enter your **username** and **password**.
3. If prompted, set a new password (first login or admin-reset).
4. You will land on the **Dashboard** (or the first page you have access to).

If your account is locked after repeated failed attempts, wait for the cooldown or
contact an administrator.

---

## 2. The credential request workflow

This is the core task. Credentials are **never** shown until you record *why* you need
them. Steps:

1. Go to **Request Credentials**.
2. **Search / select the customer**.
3. **Select the machine** you are working on.
4. Fill in the mandatory fields:
   - **Reason category** (e.g. Incident, Maintenance, Installation, Other).
   - **Reason** (free text — be specific).
   - **Internal ticket** (your helpdesk ticket number) — **mandatory**.
   - **Customer ticket** (optional).
5. **Submit**.
6. The applicable credentials are now shown, resolved automatically for that machine
   (machine-specific values override client and global ones).
7. Use the **Show / Hide** toggle to reveal secret fields (passwords, keys) only when
   you need them. Use the copy button to copy a value.

> Everything you do here is recorded: an **access request** plus an **audit entry**
> capturing who, what, when, the machine/customer, the reason, and the ticket.

---

## 3. Viewing customers and machines

- **Clients** — browse/search customers you have access to.
- **Machines** — browse/search machines and see which customer they belong to.

Some customers are marked **restricted**; their credentials may be hidden unless you
are an administrator.

---

## 4. Global search

Use the search bar (top of the page) to find anything quickly across:
customer name/code, machine name/serial, RustDesk/AnyDesk ID, VPN server, username,
IP address, and notes.

Search **never** returns secret values (passwords/keys) — only the searchable,
non-secret fields. To obtain a secret, use the request workflow.

---

## 5. Changing your password

1. Open **Change Password** (from the user menu).
2. Enter your current password and a new strong password.
3. Save. You remain signed in.

Choose a strong, unique password. Never share your account.

---

## 6. Signing out

Use **Log out** in the top-right menu. Your session ends and is recorded (login time,
logout time, duration, and what you accessed). Sessions also time out automatically
after a period of inactivity.

---

## 7. Good practice

- Always enter an accurate reason and the correct internal ticket.
- Reveal secrets only when actively using them; hide them again afterwards.
- Never copy credentials into chat, email, or unmanaged notes.
- Report anything that looks wrong (unexpected access, missing machines) to an admin.
