import React, { useState, useEffect, useCallback } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import DashboardLayout from '../../../components/DashboardLayout';
import Input from '../../../components/Input';
import Button from '../../../components/Button';
import StatusBadge from '../../../components/StatusBadge';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import ConfirmDialog from '../../../components/ConfirmDialog';
import { getMyProfile, updateMyProfile, getDeletionEligibility, deleteMyAccount } from '../../../services/profileApi';
import { useAuth } from '../../../context/AuthContext';

// Shared ProfilePage used by every role dashboard.
// Props:
//   navItems           — same format as DashboardLayout navItems
//   roleBadge          — e.g. "Hotel Partner", "Transport Partner"
//   profileRoute       — passed through to DashboardLayout
//   dashboardHomePath  — path for the "View My [X]" link in the danger zone (e.g. "/hotel-owner/hotels")
//   dashboardHomeLabel — label for that link (e.g. "My Hotels", "My Vehicles")
export default function ProfilePage({ navItems, roleBadge, profileRoute, dashboardHomePath, dashboardHomeLabel, showDangerZone = true }) {
  const { logout } = useAuth();
  const navigate = useNavigate();

  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const [formData, setFormData] = useState({ FullName: '', Email: '' });
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState(null);
  const [saveSuccess, setSaveSuccess] = useState(false);

  const [eligibility, setEligibility] = useState(null);
  const [eligibilityLoading, setEligibilityLoading] = useState(true);

  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);

  const fetchProfile = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getMyProfile();
      setProfile(data);
      setFormData({ FullName: data.fullName || '', Email: data.email || '' });
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load profile.');
    } finally {
      setLoading(false);
    }
  }, []);

  const fetchEligibility = useCallback(async () => {
    try {
      setEligibilityLoading(true);
      const data = await getDeletionEligibility();
      setEligibility(data);
    } catch (err) {
      console.error('Failed to load deletion eligibility', err);
    } finally {
      setEligibilityLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchProfile();
    fetchEligibility();
  }, [fetchProfile, fetchEligibility]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleSaveProfile = async (e) => {
    e.preventDefault();
    if (!formData.FullName.trim() || !formData.Email.trim()) return;
    try {
      setSaving(true);
      setSaveError(null);
      setSaveSuccess(false);
      const updated = await updateMyProfile(formData);
      setProfile(updated);
      setSaveSuccess(true);
      setTimeout(() => setSaveSuccess(false), 3000);
    } catch (err) {
      setSaveError(err.response?.data?.message || 'Failed to update profile.');
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteAccount = async () => {
    await deleteMyAccount();
    logout();
    navigate('/');
  };

  if (loading) {
    return (
      <DashboardLayout navItems={navItems} roleBadge={roleBadge} profileRoute={profileRoute}>
        <div className="flex justify-center items-center h-64">
          <LoadingSpinner size="lg" />
        </div>
      </DashboardLayout>
    );
  }

  if (error || !profile) {
    return (
      <DashboardLayout navItems={navItems} roleBadge={roleBadge} profileRoute={profileRoute}>
        <ErrorBanner message={error || 'Profile not found'} />
      </DashboardLayout>
    );
  }

  return (
    <DashboardLayout navItems={navItems} roleBadge={roleBadge} profileRoute={profileRoute}>
      {/* Page Header */}
      <div className="mb-8">
        <span className="text-label-uppercase text-primary tracking-widest block mb-2">
          ● MY ACCOUNT
        </span>
        <h1 className="text-headline-lg font-heading font-bold text-text mb-1">
          Profile Settings
        </h1>
        <p className="text-body-md text-text-secondary">
          Update your account information.
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 space-y-8">

          {/* Profile Form Card */}
          <div className="bg-white border border-border-neutral rounded-xl shadow-soft p-[var(--space-xl)]">
            <h2 className="text-headline-md font-heading font-bold text-text mb-6 border-b border-border-neutral pb-4">
              Personal Information
            </h2>

            <form onSubmit={handleSaveProfile} className="space-y-4">
              {saveError && <ErrorBanner message={saveError} />}
              {saveSuccess && (
                <div className="bg-status-success/10 text-status-success border border-status-success/30 rounded-md p-3 mb-4 text-sm font-semibold">
                  Profile updated successfully.
                </div>
              )}

              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <Input
                  label="Full Name"
                  name="FullName"
                  value={formData.FullName}
                  onChange={handleChange}
                  required
                />
                <Input
                  label="Email Address"
                  name="Email"
                  type="email"
                  value={formData.Email}
                  onChange={handleChange}
                  required
                />
              </div>

              <div className="bg-surface-neutral/30 rounded-md p-4 space-y-3 mt-4">
                <div className="flex justify-between items-center">
                  <span className="text-body-sm text-text-secondary font-bold font-heading">Role</span>
                  <span className="text-body-md text-text font-medium">{profile.role}</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-body-sm text-text-secondary font-bold font-heading">Account Status</span>
                  <StatusBadge status={profile.status} />
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-body-sm text-text-secondary font-bold font-heading">Member Since</span>
                  <span className="text-body-md text-text">{new Date(profile.createdAt).toLocaleDateString()}</span>
                </div>
              </div>

              <div className="flex justify-end pt-4">
                <Button type="submit" disabled={saving || !formData.FullName.trim() || !formData.Email.trim()}>
                  {saving ? <LoadingSpinner size="sm" /> : 'Save Changes'}
                </Button>
              </div>
            </form>
          </div>

          {/* Danger Zone Card — hidden for roles where deletion is unreliable */}
          {showDangerZone && (
          <div className="border border-status-danger/30 bg-status-danger/5 rounded-xl p-[var(--space-lg)]">
            <div className="text-status-danger text-label-uppercase tracking-widest mb-2 font-bold font-heading">
              DANGER ZONE
            </div>
            <h2 className="text-headline-sm font-heading font-bold text-text mb-2">
              Delete Account
            </h2>
            <p className="text-body-md text-text-secondary mb-6">
              Permanently delete your account and all associated data. This action cannot be undone.
            </p>

            {eligibilityLoading ? (
              <LoadingSpinner size="sm" />
            ) : eligibility?.canDelete === false ? (
              <div className="space-y-3">
                <button
                  disabled
                  className="px-6 py-2.5 rounded-pill font-heading text-xs font-bold uppercase tracking-widest bg-status-danger text-white opacity-50 cursor-not-allowed"
                >
                  Delete My Account
                </button>
                <p className="text-status-danger text-body-sm font-semibold">
                  {eligibility.blockingMessage}
                </p>
                {/* Helper link: tells the user exactly where to go to resolve the block */}
                <Link
                  to={dashboardHomePath}
                  className="text-primary hover:underline text-sm font-bold inline-flex items-center gap-1"
                >
                  <span>→</span> View {dashboardHomeLabel}
                </Link>
              </div>
            ) : (
              <button
                onClick={() => setDeleteConfirmOpen(true)}
                className="px-6 py-2.5 rounded-pill font-heading text-xs font-bold uppercase tracking-widest bg-status-danger hover:bg-red-700 text-white transition-colors focus:ring-2 focus:ring-offset-2 focus:ring-status-danger"
              >
                Delete My Account
              </button>
            )}
          </div>
          )}
        </div>
      </div>

      <ConfirmDialog
        isOpen={deleteConfirmOpen}
        onClose={() => setDeleteConfirmOpen(false)}
        onConfirm={handleDeleteAccount}
        title="Delete Your Account"
        message="This will permanently delete your account and all associated data. This action cannot be undone."
        confirmLabel="Delete My Account"
        isDanger
      />
    </DashboardLayout>
  );
}

