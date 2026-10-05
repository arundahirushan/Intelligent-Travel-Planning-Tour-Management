# Supabase Storage Setup — Listing Photos

Hotel, vehicle and destination photos are uploaded through the ASP.NET Core API
(`POST /api/uploads/listing-photo`) and stored in **one public Supabase Storage bucket**.
React and Flutter never talk to Supabase directly and never see the key.

## 1. Create the bucket (Supabase Dashboard)

1. Open your Supabase project → **Storage** → **New bucket**.
2. **Name:** `listing-photos` (must match `SupabaseStorage:Bucket`).
3. Turn **Public bucket** ON (photos are read through a public URL).
4. Optional hardening in the bucket settings: set **Restrict file size** to `5 MB` and
   **Allowed MIME types** to `image/jpeg, image/png, image/webp`. The API already enforces both.
5. No storage policies are needed for uploads: the API uploads with the server-side key,
   which bypasses RLS. Do not add public upload/write policies.

Objects are stored as `hotels/<guid>.<ext>`, `vehicles/<guid>.<ext>`, `destinations/<guid>.<ext>`.

## 2. Values you must provide

| Setting | Where to find it | Secret? |
| --- | --- | --- |
| `SupabaseStorage:Url` | Project Settings → API → **Project URL** (e.g. `https://abcdxyz.supabase.co`) | No |
| `SupabaseStorage:ServiceRoleKey` | Project Settings → API Keys → **service_role** (legacy) or a **secret** key (`sb_secret_...`) | **Yes — backend only** |
| `SupabaseStorage:Bucket` | `listing-photos` (already the default in `appsettings.json`) | No |

> Never use the `anon`/publishable key here, and never put the service key in React, Flutter,
> `appsettings.json`, or git.

## 3. Configure the API

Local development (user-secrets):

```bash
cd server/TourManagement.Api
dotnet user-secrets set "SupabaseStorage:Url" "https://<your-project-ref>.supabase.co"
dotnet user-secrets set "SupabaseStorage:ServiceRoleKey" "<service key>"
```

Production (environment variables — note the double underscore):

```
SupabaseStorage__Url=https://<your-project-ref>.supabase.co
SupabaseStorage__ServiceRoleKey=<service key>
SupabaseStorage__Bucket=listing-photos
```

If these are missing, the upload endpoint returns a clear
"Photo upload is not configured on the server." error and nothing is saved.

## 4. Quick manual test

1. Start the API, log in as a HotelOwner / TransportProvider (React) or Admin (Flutter).
2. In the add/edit form choose a JPEG/PNG/WebP photo and press **Save**.
3. Confirm the saved `ImageUrl` starts with
   `https://<project>.supabase.co/storage/v1/object/public/listing-photos/` and opens in a browser.

## Known limitation

If a photo uploads successfully but the following create/update request fails, the file stays in
the bucket unused. The forms reuse the same uploaded URL when the user presses Save again, so
retries do not create duplicates, but a cancelled form after a failed save leaves one orphan file.
Clean these up manually in the Dashboard if needed.

