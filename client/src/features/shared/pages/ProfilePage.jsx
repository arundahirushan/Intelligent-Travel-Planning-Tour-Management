import React, { useState, useEffect, useCallback } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import DashboardLayout from '../../../components/DashboardLayout';
import Input from '../../../components/Input';
import Button from '../../../components/Button';
import StatusBadge from '../../../components/StatusBadge';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import ConfirmDialog from '../../../components/ConfirmDialog';
import { getMyProfile, updateMyProfile, getDeletionEligibility, deleteMyAccount, changeMyPassword } from '../../../services/profileApi';
import { useAuth } from '../../../context/AuthContext';

const ROLE_LABELS = {
  Traveler: 'Traveler',
  HotelOwner: 'Hotel Partner',
  TransportProvider: 'Transport Partner',
  Admin: 'Administrator',
  SuperAdmin: 'Super Administrator',
  Supplier: 'Supplier'
};

export default function ProfilePage({ navItems, roleBadge, profileRoute, dashboardHomePath, dashboardHomeLabel, showDangerZone = true }) {
  const { logout, updateUser } = useAuth();
  const navigate = useNavigate();

  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const [formData, setFormData] = useState({ FullName: '', Email: '' });
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState(null);
  const [saveSuccess, setSaveSuccess] = useState(false);

  const [passwordData, setPasswordData] = useState({ CurrentPassword: '', NewPassword: '', ConfirmNewPassword: '' });
  const [passwordSaving, setPasswordSaving] = useState(false);
  const [passwordError, setPasswordError] = useState(null);
  const [passwordSuccess, setPasswordSuccess] = useState(false);
  
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

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

  const handleInfoChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handlePasswordChange = (e) => {
    const { name, value } = e.target;
    setPasswordData(prev => ({ ...prev, [name]: value }));
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
      
      // Update global AuthContext state so navbar immediately reflects changes
      updateUser({ fullName: updated.fullName, email: updated.email });
      
      setSaveSuccess(true);
      setTimeout(() => setSaveSuccess(false), 3000);
    } catch (err) {
      setSaveError(err.response?.data?.message || 'Failed to update profile.');
    } finally {
      setSaving(false);
    }
  };

  const handleUpdatePassword = async (e) => {
    e.preventDefault();
    if (!passwordData.CurrentPassword || !passwordData.NewPassword || !passwordData.ConfirmNewPassword) {
        setPasswordError('All password fields are required.');
        return;
    }
    if (passwordData.NewPassword !== passwordData.ConfirmNewPassword) {
        setPasswordError('New password and confirmation do not match.');
        return;
    }
    if (passwordData.NewPassword.length < 6) {
        setPasswordError('New password must be at least 6 characters long.');
        return;
    }

    try {
      setPasswordSaving(true);
      setPasswordError(null);
      setPasswordSuccess(false);
      
      await changeMyPassword(passwordData);
      
      setPasswordSuccess(true);
      setPasswordData({ CurrentPassword: '', NewPassword: '', ConfirmNewPassword: '' });
      
      setTimeout(() => {
          logout();
          navigate('/login');
      }, 2000);
      
    } catch (err) {
      setPasswordError(err.response?.data?.message || 'Failed to update password.');
    } finally {
      setPasswordSaving(false);
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

  const roleLabel = ROLE_LABELS[profile.role] || profile.role;
  const initials = profile.fullName ? profile.fullName.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase() : 'U';
  const memberSince = new Date(profile.createdAt).toLocaleDateString(undefined, { year: 'numeric', month: 'long' });

  return (
    <DashboardLayout navItems={navItems} roleBadge={roleBadge} profileRoute={profileRoute}>
      <div className="max-w-3xl mx-auto w-full pb-12">
        {/* Page Header */}
        <div className="mb-8">
          <span className="text-label-uppercase text-primary tracking-widest block mb-2 font-bold font-heading">
            MY ACCOUNT
          </span>
          <h1 className="text-headline-lg font-heading font-bold text-text mb-2">
            Profile Settings
          </h1>
          <p className="text-body-md text-text-secondary">
            Manage your personal information and account security.
          </p>
        </div>

        {/* Profile Summary */}
        <div className="bg-surface-light border border-border-blue/50 rounded-[20px] shadow-soft p-6 md:p-8 mb-8 flex flex-col sm:flex-row items-center sm:items-start gap-6">
          <div className="w-20 h-20 shrink-0 rounded-full bg-gradient-to-br from-primary to-accent text-white flex items-center justify-center font-heading font-bold text-3xl shadow-md border-2 border-white">
            {initials}
          </div>
          <div className="flex-1 text-center sm:text-left flex flex-col justify-center min-h-[80px]">
            <h2 className="text-headline-sm font-heading font-bold text-text mb-1">{profile.fullName}</h2>
            <p className="text-body-md text-text-secondary mb-3">{profile.email}</p>
            <div className="flex flex-wrap items-center justify-center sm:justify-start gap-3">
              <span className="inline-flex items-center px-3 py-1 bg-surface-blue text-primary font-heading font-bold text-xs uppercase tracking-wider rounded-pill">
                {roleLabel}
              </span>
              <StatusBadge status={profile.status} />
            </div>
          </div>
          <div className="text-center sm:text-right flex flex-col justify-center sm:min-h-[80px]">
            <span className="text-xs font-heading font-bold uppercase tracking-wider text-text-secondary block mb-1">
              Member Since
            </span>
            <span className="text-sm font-body text-text font-medium">{memberSince}</span>
          </div>
        </div>

        {/* Personal Information */}
        <div className="bg-white border border-border-neutral rounded-[20px] shadow-soft p-6 md:p-8 mb-8">
          <h2 className="text-headline-sm font-heading font-bold text-text mb-6">
            Personal Information
          </h2>
          <form onSubmit={handleSaveProfile} className="space-y-6">
            {saveError && <ErrorBanner message={saveError} />}
            {saveSuccess && (
              <div className="bg-status-success/10 text-status-success border border-status-success/30 rounded-md p-4 text-sm font-semibold flex items-center gap-2">
                <span className="material-symbols-outlined text-[20px]">check_circle</span>
                Profile information updated successfully.
              </div>
            )}
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <Input
                label="Full Name"
                name="FullName"
                value={formData.FullName}
                onChange={handleInfoChange}
                required
              />
              <Input
                label="Email Address"
                name="Email"
                type="email"
                value={formData.Email}
                onChange={handleInfoChange}
                required
              />
            </div>
            
            <div className="flex justify-end pt-2">
              <Button type="submit" disabled={saving || !formData.FullName.trim() || !formData.Email.trim()} className="active:scale-95">
                {saving ? <LoadingSpinner size="sm" /> : 'SAVE CHANGES'}
              </Button>
            </div>
          </form>
        </div>

        {/* Password & Security */}
        <div className="bg-white border border-border-neutral rounded-[20px] shadow-soft p-6 md:p-8 mb-8">
          <div className="mb-6">
            <h2 className="text-headline-sm font-heading font-bold text-text mb-1">
              Password & Security
            </h2>
            <p className="text-body-sm text-text-secondary">
              Update your password to keep your account secure.
            </p>
          </div>
          
          <form onSubmit={handleUpdatePassword} className="space-y-6">
            {passwordError && <ErrorBanner message={passwordError} />}
            {passwordSuccess && (
              <div className="bg-status-success/10 text-status-success border border-status-success/30 rounded-md p-4 text-sm font-semibold flex items-center gap-2">
                <span className="material-symbols-outlined text-[20px]">check_circle</span>
                Password updated successfully. You will be redirected to log in.
              </div>
            )}
            
            <div className="max-w-md">
              <div className="relative">
                <Input
                  label="Current Password"
                  name="CurrentPassword"
                  type={showCurrentPassword ? "text" : "password"}
                  value={passwordData.CurrentPassword}
                  onChange={handlePasswordChange}
                  required
                />
                <button type="button" onClick={() => setShowCurrentPassword(!showCurrentPassword)} className="absolute right-3 top-[36px] text-text-secondary hover:text-text transition-colors">
                  <span className="material-symbols-outlined text-[20px]">{showCurrentPassword ? 'visibility_off' : 'visibility'}</span>
                </button>
              </div>

              <div className="relative">
                <Input
                  label="New Password"
                  name="NewPassword"
                  type={showNewPassword ? "text" : "password"}
                  value={passwordData.NewPassword}
                  onChange={handlePasswordChange}
                  required
                />
                <button type="button" onClick={() => setShowNewPassword(!showNewPassword)} className="absolute right-3 top-[36px] text-text-secondary hover:text-text transition-colors">
                  <span className="material-symbols-outlined text-[20px]">{showNewPassword ? 'visibility_off' : 'visibility'}</span>
                </button>
              </div>

              <div className="relative">
                <Input
                  label="Confirm New Password"
                  name="ConfirmNewPassword"
                  type={showConfirmPassword ? "text" : "password"}
                  value={passwordData.ConfirmNewPassword}
                  onChange={handlePasswordChange}
                  required
                />
                <button type="button" onClick={() => setShowConfirmPassword(!showConfirmPassword)} className="absolute right-3 top-[36px] text-text-secondary hover:text-text transition-colors">
                  <span className="material-symbols-outlined text-[20px]">{showConfirmPassword ? 'visibility_off' : 'visibility'}</span>
                </button>
              </div>
            </div>
            
            <div className="flex justify-start pt-2">
              <Button type="submit" disabled={passwordSaving || !passwordData.CurrentPassword || !passwordData.NewPassword || !passwordData.ConfirmNewPassword} className="active:scale-95">
                {passwordSaving ? <LoadingSpinner size="sm" /> : 'UPDATE PASSWORD'}
              </Button>
            </div>
          </form>
        </div>

        {/* Danger Zone */}
        {showDangerZone && (
          <div className="bg-red-50/50 border border-red-200 rounded-[20px] shadow-sm p-6 md:p-8">
            <div className="flex items-center gap-2 text-status-danger mb-4">
              <span className="material-symbols-outlined text-[24px]">warning</span>
              <h2 className="text-headline-sm font-heading font-bold">
                Delete Account
              </h2>
            </div>
            <p className="text-body-md text-text-secondary mb-6 max-w-2xl">
              Permanently delete your account and all associated data. This action cannot be undone.
            </p>

            {eligibilityLoading ? (
              <LoadingSpinner size="sm" />
            ) : eligibility?.canDelete === false ? (
              <div className="space-y-4">
                <button
                  disabled
                  className="px-7 py-3 rounded-pill font-heading text-xs font-bold uppercase tracking-widest bg-status-danger text-white opacity-50 cursor-not-allowed"
                >
                  DELETE MY ACCOUNT
                </button>
                <div className="bg-white border border-red-200 rounded-lg p-4">
                  <p className="text-status-danger text-sm font-semibold mb-2">
                    {eligibility.blockingMessage}
                  </p>
                  <Link
                    to={dashboardHomePath}
                    className="text-primary hover:underline text-sm font-bold inline-flex items-center gap-1"
                  >
                    <span>→</span> View {dashboardHomeLabel}
                  </Link>
                </div>
              </div>
            ) : (
              <button
                onClick={() => setDeleteConfirmOpen(true)}
                className="px-7 py-3 rounded-pill font-heading text-xs font-bold uppercase tracking-widest bg-status-danger hover:bg-red-700 text-white transition-all active:scale-95 focus:ring-2 focus:ring-offset-2 focus:ring-status-danger shadow-soft"
              >
                DELETE MY ACCOUNT
              </button>
            )}
          </div>
        )}
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
