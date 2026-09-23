import apiClient from './api';

// Shared profile/account API — used by all role dashboards.
// Profile endpoints are role-agnostic on the backend (/profile/me).

const unwrap = (res) => res.data.data;

export async function getMyProfile() {
  return apiClient.get('/profile/me').then(unwrap);
}

export async function updateMyProfile(body) {
  return apiClient.put('/profile/me', body).then(unwrap);
}

export async function getDeletionEligibility() {
  return apiClient.get('/profile/me/deletion-eligibility').then(unwrap);
}

export async function deleteMyAccount() {
  return apiClient.delete('/profile/me').then(res => res.data);
}

