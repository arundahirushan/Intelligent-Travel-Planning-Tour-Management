import apiClient from './api';

// Uploads one listing photo through the ASP.NET Core API (which stores it in Supabase Storage).
// Returns the public image URL to save in the entity's ImageUrl field.
export async function uploadListingPhoto(file) {
  const body = new FormData();
  body.append('file', file);

  const res = await apiClient.post('/uploads/listing-photo', body, {
    // apiClient defaults to JSON, so override it for this multipart request.
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return res.data.data.imageUrl;
}

