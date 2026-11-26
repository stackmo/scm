import { apiFetch } from '../contexts/AuthRbacContext';
import {
  QueueTicket,
  QueueBoard,
  ClinicRoomLoad,
  QueueAssignment,
  CheckinRequest,
  WalkinRegistrationRequest,
  ReceptionRosterItem
} from '../types/queueing';

const API_BASE = '/api';

export interface BranchContractItem {
  id: string;
  companyId: string;
  companyName: string;
  companyTaxCode?: string;
  contractNumber: string;
  status: number;
  employeeCount?: number;
}

export interface MasterMedicalServiceItem {
  id: string;
  serviceCode: string;
  serviceName: string;
  serviceGroupName: string;
  serviceGroupCode: string;
  unit: string;
  standardPrice: number;
  insurancePrice: number;
  corporatePrice: number;
  isActive: boolean;
}

export interface B2bPackageSummaryItem {
  id: string;
  code: string;
  name: string;
  companyId?: string | null;
  contractId?: string | null;
  services: Array<{
    id: string;
    serviceId: string;
    serviceCode: string;
    serviceName: string;
    originalPrice: number;
    dealPrice: number;
  }>;
}

export const queueApiService = {
  // Check-in (Quét QR CCCD/BHYT hoặc nhập thông tin)
  async checkin(data: CheckinRequest): Promise<QueueTicket> {
    const res = await apiFetch(`${API_BASE}/reception/checkin`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    if (!res.ok) {
      const err = await res.text();
      throw new Error(err || 'Tiếp nhận thất bại');
    }
    return res.json();
  },

  // Đăng ký & tiếp nhận Khách lẻ vãng lai thực tế vào PostgreSQL
  async registerWalkin(data: WalkinRegistrationRequest): Promise<QueueTicket> {
    const res = await apiFetch(`${API_BASE}/reception/walkin`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    if (!res.ok) {
      const err = await res.text();
      throw new Error(err || 'Đăng ký khách lẻ thất bại');
    }
    return res.json();
  },

  // Nhận bàn giao danh sách nhân viên B2B vào quầy tiếp nhận
  async handoffB2B(contractId: string, employeeIds?: string[], autoIssueTickets = false): Promise<ReceptionRosterItem[]> {
    const res = await apiFetch(`${API_BASE}/reception/b2b-handoff`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ contractId, employeeIds, autoIssueTickets })
    });
    if (!res.ok) {
      const err = await res.text();
      throw new Error(err || 'Bàn giao B2B thất bại');
    }
    return res.json();
  },

  // Lấy danh sách hợp đồng B2B thật
  async getB2bContracts(): Promise<BranchContractItem[]> {
    const res = await apiFetch(`${API_BASE}/b2b/contracts`);
    if (!res.ok) return [];
    return res.json();
  },

  // Lấy danh mục dịch vụ thật từ DB
  async getMedicalServices(): Promise<MasterMedicalServiceItem[]> {
    const res = await apiFetch(`${API_BASE}/master-data/services`);
    if (!res.ok) return [];
    return res.json();
  },

  // Lấy danh mục gói khám thật từ DB
  async getPackages(): Promise<B2bPackageSummaryItem[]> {
    const res = await apiFetch(`${API_BASE}/b2b/packages`);
    if (!res.ok) return [];
    return res.json();
  },

  // Lấy danh sách vé hoạt động trong ngày
  async getActiveTickets(): Promise<QueueTicket[]> {
    const res = await apiFetch(`${API_BASE}/reception/tickets`);
    if (!res.ok) throw new Error('Không thể tải danh sách vé');
    return res.json();
  },

  // Lấy chi tiết 1 vé
  async getTicket(id: string): Promise<QueueTicket> {
    const res = await apiFetch(`${API_BASE}/reception/tickets/${id}`);
    if (!res.ok) throw new Error('Không tìm thấy vé');
    return res.json();
  },

  // Lấy danh sách tiếp nhận (Roster) theo Đợt khám / Hợp đồng hoặc Nhật ký hôm nay
  async getReceptionRoster(contractId?: string, keyword?: string): Promise<ReceptionRosterItem[]> {
    const params = new URLSearchParams();
    if (contractId) params.append('contractId', contractId);
    if (keyword) params.append('keyword', keyword);
    const qs = params.toString() ? `?${params.toString()}` : '';
    const res = await apiFetch(`${API_BASE}/reception/roster${qs}`);
    if (!res.ok) throw new Error('Không thể tải danh sách tiếp nhận');
    return res.json();
  },

  // Lấy bảng thông tin hàng đợi tổng hợp (QueueBoard)
  async getQueueBoard(): Promise<QueueBoard> {
    const res = await apiFetch(`${API_BASE}/queueing/board`);
    if (!res.ok) throw new Error('Không thể tải bảng hàng đợi');
    return res.json();
  },

  // Lấy danh sách tải các buồng khám
  async getRoomLoads(): Promise<ClinicRoomLoad[]> {
    const res = await apiFetch(`${API_BASE}/queueing/rooms`);
    if (!res.ok) throw new Error('Không thể tải danh sách buồng khám');
    return res.json();
  },

  // Bác sĩ gọi bệnh nhân tiếp theo vào phòng
  async callNextPatient(roomCode: string): Promise<{
    hasPatient: boolean;
    assignment?: QueueAssignment;
    roomLoad?: ClinicRoomLoad;
    message: string;
  }> {
    const res = await apiFetch(`${API_BASE}/queueing/room/${encodeURIComponent(roomCode)}/next`);
    if (!res.ok) throw new Error('Gọi bệnh nhân thất bại');
    return res.json();
  },

  // Hoàn tất lượt khám tại buồng
  async completeAssignment(assignmentId: string, notes?: string): Promise<QueueAssignment> {
    const res = await apiFetch(`${API_BASE}/queueing/assignment/${assignmentId}/complete`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ notes })
    });
    if (!res.ok) throw new Error('Không thể hoàn tất lượt khám');
    return res.json();
  },

  // Tự động chuyển bệnh nhân sang phòng tối ưu tiếp theo
  async autoAdvance(ticketId: string): Promise<QueueAssignment | null> {
    const res = await apiFetch(`${API_BASE}/queueing/advance/${ticketId}`, {
      method: 'POST'
    });
    if (!res.ok) return null;
    return res.json();
  }
};
