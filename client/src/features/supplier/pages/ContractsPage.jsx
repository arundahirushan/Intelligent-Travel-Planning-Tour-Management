import React, { useState, useEffect, useCallback, useMemo } from 'react';
import DashboardLayout from '../../../components/DashboardLayout';
import Button from '../../../components/Button';
import StatusBadge from '../../../components/StatusBadge';
import EmptyState from '../../../components/EmptyState';
import ErrorBanner from '../../../components/ErrorBanner';
import ContractRequestModal from '../components/ContractRequestModal';
import { getMyContractRequests } from '../../../services/supplierApi';
import { SUPPLIER_NAV_ITEMS } from './SuppliesPage';

// ContractRequestStatus values from backend
const STATUS_LABEL_MAP = {
  Pending: { label: 'Under Review', colorClass: 'bg-status-warning/15 text-status-warning' },
  Approved: { label: 'Approved', colorClass: 'bg-status-success/15 text-status-success' },
  Rejected: { label: 'Rejected', colorClass: 'bg-status-danger/15 text-status-danger' },
};

// ContractRequestType labels
const TYPE_LABEL = { New: 'New Contract', Renewal: 'Renewal' };

export default function ContractsPage() {
  const [requests, setRequests] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [isRequestModalOpen, setIsRequestModalOpen] = useState(false);

  const fetchRequests = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getMyContractRequests({ pageSize: 100 });
      setRequests(data.items || []);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load contract requests. Please try again.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchRequests();
  }, [fetchRequests]);

  // ── Infer contract state from request history ──────────────────────────────
  //
  // NOTE: There is no Supplier-authorized GET /api/contracts endpoint.
  // We derive state from request history:
  //   - If any request is Pending → show "pending review" state, disable submit button
  //   - Else if any request is Approved → supplier has a contract → offer "New" request if needed
  //   - If all requests are Rejected or no requests → offer "New" request

  const hasPendingRequest = useMemo(() =>
    requests.some((r) => r.status === 'Pending'), [requests]);

  const hasApprovedRequest = useMemo(() =>
    requests.some((r) => r.status === 'Approved'), [requests]);

  // For contract status banner
  const contractStatusInfo = useMemo(() => {
    if (loading) return null;
    if (hasPendingRequest) {
      return {
        icon: 'pending',
        title: 'Request Under Review',
        body: 'Your contract request has been submitted and is currently being reviewed by an admin. You will be able to list supplies once it is approved.',
        colorClass: 'bg-status-warning/10 border-status-warning/30',
        titleColor: 'text-status-warning',
      };
    }
    if (hasApprovedRequest) {
      return {
        icon: 'verified',
        title: 'Contract Granted',
        body: 'A supplier contract has been approved for your account. You can now create and list supply items. If your contract has expired, submit a new request to renew.',
        colorClass: 'bg-status-success/10 border-status-success/30',
        titleColor: 'text-status-success',
      };
    }
    return {
      icon: 'info',
      title: 'No Active Contract',
      body: 'You do not have a supplier contract yet. Submit a contract request below — an admin will review it and issue your contract.',
      colorClass: 'bg-surface-blue border-border-blue',
      titleColor: 'text-primary',
    };
  }, [loading, hasPendingRequest, hasApprovedRequest]);

  return (
    <DashboardLayout navItems={SUPPLIER_NAV_ITEMS} roleBadge="Supply Partner" profileRoute="/supplier/profile">

      {/* Page Header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-4 mb-8">
        <div>
          <span className="text-label-uppercase text-primary tracking-widest block mb-2">
            ● CONTRACT MANAGEMENT
          </span>
          <h1 className="text-headline-lg font-heading font-bold text-text mb-1">
            Contracts
          </h1>
          <p className="text-body-md text-text-secondary">
            View your contract request history and submit new requests.
          </p>
        </div>

        {/* Submit button — disabled while a request is pending */}
        {!loading && (
          <div>
            <Button
              onClick={() => setIsRequestModalOpen(true)}
              disabled={hasPendingRequest}
              title={hasPendingRequest ? 'You already have a request pending review' : undefined}
            >
              {hasPendingRequest ? '↻ Request Pending…' : '+ Request Contract'}
            </Button>
            {hasPendingRequest && (
              <p className="text-body-sm text-text-secondary text-right mt-1">
                Waiting for admin review
              </p>
            )}
          </div>
        )}
      </div>

      {error && !loading && <ErrorBanner message={error} className="mb-6" />}

      {/* Contract status banner */}
      {contractStatusInfo && (
        <div className={`rounded-xl border p-5 mb-8 flex items-start gap-4 ${contractStatusInfo.colorClass}`}>
          <span className={`material-symbols-outlined text-2xl mt-0.5 ${contractStatusInfo.titleColor}`}>
            {contractStatusInfo.icon}
          </span>
          <div>
            <p className={`font-heading font-bold text-headline-sm mb-1 ${contractStatusInfo.titleColor}`}>
              {contractStatusInfo.title}
            </p>
            <p className="text-body-md text-text-secondary">{contractStatusInfo.body}</p>
          </div>
        </div>
      )}

      {/* Important note about the contract limitation */}
      <div className="bg-surface-neutral/50 border border-border-neutral rounded-xl p-4 mb-8 flex items-start gap-3">
        <span className="material-symbols-outlined text-text-secondary text-lg mt-0.5">info</span>
        <p className="text-body-sm text-text-secondary">
          <strong className="text-text">Note:</strong> Contract details (start/end dates, terms) are managed by your admin.
          This page shows your submitted requests. Contact your admin for contract specifics.
        </p>
      </div>

      {/* Section title */}
      <h2 className="text-headline-sm font-heading font-bold text-text mb-4">Request History</h2>

      {/* Request history table */}
      {loading ? (
        <div className="space-y-3">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-16 bg-surface-neutral/50 animate-pulse rounded-xl border border-border-neutral" />
          ))}
        </div>
      ) : requests.length === 0 ? (
        <EmptyState
          icon="description"
          title="No contract requests yet"
          description="Submit a contract request to get started as a supplier."
          action={
            <Button onClick={() => setIsRequestModalOpen(true)}>
              Request a Contract
            </Button>
          }
        />
      ) : (
        <div className="bg-white border border-border-neutral rounded-xl overflow-hidden">
          <table className="w-full text-left" aria-label="Contract request history">
            <thead>
              <tr className="border-b border-border-neutral bg-surface-neutral/50">
                <th className="px-6 py-3 text-label-uppercase text-text-secondary tracking-widest">Type</th>
                <th className="px-6 py-3 text-label-uppercase text-text-secondary tracking-widest">Requested End</th>
                <th className="px-6 py-3 text-label-uppercase text-text-secondary tracking-widest">Status</th>
                <th className="px-6 py-3 text-label-uppercase text-text-secondary tracking-widest">Submitted</th>
                <th className="px-6 py-3 text-label-uppercase text-text-secondary tracking-widest">Admin Note</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border-neutral">
              {requests.map((req) => {
                const statusInfo = STATUS_LABEL_MAP[req.status] || { label: req.status, colorClass: 'bg-surface-neutral text-text-secondary' };
                return (
                  <tr key={req.id} className="hover:bg-surface-neutral/30 transition-colors">
                    <td className="px-6 py-4">
                      <span className="text-body-sm font-semibold text-text">
                        {TYPE_LABEL[req.requestType] || req.requestType}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <span className="text-body-sm text-text-secondary">
                        {new Date(req.requestedEndDate).toLocaleDateString()}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-label-badge font-semibold ${statusInfo.colorClass}`}>
                        {statusInfo.label}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <span className="text-body-sm text-text-secondary">
                        {new Date(req.createdAt).toLocaleDateString()}
                      </span>
                    </td>
                    <td className="px-6 py-4 max-w-xs">
                      {req.adminNote ? (
                        <span className="text-body-sm text-text-secondary italic line-clamp-2" title={req.adminNote}>
                          {req.adminNote}
                        </span>
                      ) : (
                        <span className="text-body-sm text-border-neutral">—</span>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Contract Request Modal */}
      <ContractRequestModal
        isOpen={isRequestModalOpen}
        onClose={() => setIsRequestModalOpen(false)}
        onSuccess={fetchRequests}
        requestType="New"
      />
    </DashboardLayout>
  );
}
