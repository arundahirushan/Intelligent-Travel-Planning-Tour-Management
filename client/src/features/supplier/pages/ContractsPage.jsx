import React, { useState, useEffect, useCallback, useMemo } from 'react';
import DashboardLayout from '../../../components/DashboardLayout';
import Button from '../../../components/Button';
import StatusBadge from '../../../components/StatusBadge';
import EmptyState from '../../../components/EmptyState';
import ErrorBanner from '../../../components/ErrorBanner';
import ContractRequestModal from '../components/ContractRequestModal';
import { getMyContractRequests, getMyContractStatus, getMyContracts } from '../../../services/supplierApi';
import { SUPPLIER_NAV_ITEMS } from './SuppliesPage';

// ContractRequestStatus values from backend
const STATUS_LABEL_MAP = {
  Pending: { label: 'Under Review', colorClass: 'bg-status-warning/15 text-status-warning' },
  Approved: { label: 'Approved', colorClass: 'bg-status-success/15 text-status-success' },
  Rejected: { label: 'Rejected', colorClass: 'bg-status-danger/15 text-status-danger' },
};

const TYPE_LABEL = { New: 'New Contract', Renewal: 'Renewal' };

export default function ContractsPage() {
  const [requests, setRequests] = useState([]);
  const [contracts, setContracts] = useState([]);
  const [contractStatus, setContractStatus] = useState(null);
  
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [isRequestModalOpen, setIsRequestModalOpen] = useState(false);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const [requestsData, statusData, contractsData] = await Promise.all([
        getMyContractRequests({ pageSize: 100 }),
        getMyContractStatus(),
        getMyContracts({ pageSize: 100 })
      ]);
      setRequests(requestsData.items || []);
      setContractStatus(statusData);
      setContracts(contractsData.items || []);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load contract information. Please try again.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const hasPendingRequest = useMemo(() =>
    requests.some((r) => r.status === 'Pending'), [requests]);

  const isValidContract = contractStatus?.isValid;

  // Determine if Supplier can request a contract
  const canRequestContract = !hasPendingRequest && !isValidContract;

  // Find the current active contract from the history based on the ID from status
  const currentContract = useMemo(() => {
    if (!contractStatus?.contractId) return null;
    return contracts.find(c => c.id === contractStatus.contractId) || {
      id: contractStatus.contractId,
      status: contractStatus.status,
      endDate: contractStatus.endDate
    };
  }, [contractStatus, contracts]);

  const contractHistory = useMemo(() => {
    if (!contractStatus?.contractId) return contracts;
    return contracts.filter(c => c.id !== contractStatus.contractId);
  }, [contractStatus, contracts]);

  const contractStatusInfo = useMemo(() => {
    if (loading) return null;
    if (isValidContract) {
      return {
        icon: 'verified',
        title: 'Active Contract',
        body: 'You have a valid, active supplier contract. Only one active contract is allowed at a time.',
        colorClass: 'bg-status-success/10 border-status-success/30',
        titleColor: 'text-status-success',
      };
    }
    if (hasPendingRequest) {
      return {
        icon: 'pending',
        title: 'Request Under Review',
        body: 'Your contract request has been submitted and is currently being reviewed by an admin. You will be able to list supplies once it is approved.',
        colorClass: 'bg-status-warning/10 border-status-warning/30',
        titleColor: 'text-status-warning',
      };
    }
    return {
      icon: 'info',
      title: 'No Active Contract',
      body: 'You do not have a valid supplier contract. Submit a contract request below — an admin will review it and issue your contract.',
      colorClass: 'bg-surface-blue border-border-blue',
      titleColor: 'text-primary',
    };
  }, [loading, hasPendingRequest, isValidContract]);

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
            View your contract history and submit new requests.
          </p>
        </div>

        {!loading && (
          <div>
            <Button
              onClick={() => setIsRequestModalOpen(true)}
              disabled={!canRequestContract}
              title={
                hasPendingRequest 
                  ? 'You already have a request pending review' 
                  : isValidContract 
                    ? 'You already have an active valid contract' 
                    : undefined
              }
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

      {/* Current Contract Section */}
      <h2 className="text-headline-sm font-heading font-bold text-text mb-4">Current Contract</h2>
      {!loading && currentContract && currentContract.status === 'Active' && isValidContract ? (
        <div className="bg-white border border-border-neutral rounded-xl overflow-hidden mb-8 p-6">
          <div className="flex flex-col md:flex-row gap-8 justify-between">
            <div>
              <p className="text-body-sm text-text-secondary mb-1">Status</p>
              <StatusBadge status="Active" size="lg" />
            </div>
            <div>
              <p className="text-body-sm text-text-secondary mb-1">Start Date</p>
              <p className="font-semibold text-text">{currentContract.startDate ? new Date(currentContract.startDate).toLocaleDateString() : 'N/A'}</p>
            </div>
            <div>
              <p className="text-body-sm text-text-secondary mb-1">End Date</p>
              <p className="font-semibold text-text">{currentContract.endDate ? new Date(currentContract.endDate).toLocaleDateString() : 'N/A'}</p>
            </div>
          </div>
          {currentContract.terms && (
            <div className="mt-6 border-t border-border-neutral pt-4">
              <p className="text-body-sm text-text-secondary mb-2">Terms & Conditions</p>
              <p className="text-body-md text-text">{currentContract.terms}</p>
            </div>
          )}
        </div>
      ) : !loading ? (
         <div className="bg-surface-neutral/50 border border-border-neutral rounded-xl p-6 mb-8 text-center">
            <p className="text-body-md text-text-secondary">No active current contract.</p>
         </div>
      ) : null}


      {/* Request History */}
      <h2 className="text-headline-sm font-heading font-bold text-text mb-4">Request History</h2>
      {loading ? (
        <div className="space-y-3 mb-8">
          {[1].map((i) => (
            <div key={i} className="h-16 bg-surface-neutral/50 animate-pulse rounded-xl border border-border-neutral" />
          ))}
        </div>
      ) : requests.length === 0 ? (
        <div className="bg-surface-neutral/50 border border-border-neutral rounded-xl p-6 mb-8 text-center">
          <p className="text-body-md text-text-secondary">No contract requests found.</p>
        </div>
      ) : (
        <div className="bg-white border border-border-neutral rounded-xl overflow-hidden mb-8">
          <table className="w-full text-left">
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

      {/* Contract History */}
      {contractHistory.length > 0 && (
        <>
          <h2 className="text-headline-sm font-heading font-bold text-text mb-4 mt-8">Contract History</h2>
          <div className="bg-white border border-border-neutral rounded-xl overflow-hidden">
            <table className="w-full text-left">
              <thead>
                <tr className="border-b border-border-neutral bg-surface-neutral/50">
                  <th className="px-6 py-3 text-label-uppercase text-text-secondary tracking-widest">Status</th>
                  <th className="px-6 py-3 text-label-uppercase text-text-secondary tracking-widest">Start Date</th>
                  <th className="px-6 py-3 text-label-uppercase text-text-secondary tracking-widest">End Date</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border-neutral">
                {contractHistory.map((c) => (
                  <tr key={c.id} className="hover:bg-surface-neutral/30 transition-colors">
                    <td className="px-6 py-4">
                      <span className="text-body-sm font-semibold text-text">{c.status}</span>
                    </td>
                    <td className="px-6 py-4">
                      <span className="text-body-sm text-text-secondary">
                        {new Date(c.startDate).toLocaleDateString()}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <span className="text-body-sm text-text-secondary">
                        {new Date(c.endDate).toLocaleDateString()}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      {/* Contract Request Modal */}
      <ContractRequestModal
        isOpen={isRequestModalOpen}
        onClose={() => setIsRequestModalOpen(false)}
        onSuccess={fetchData}
        requestType="New"
      />
    </DashboardLayout>
  );
}
