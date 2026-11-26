import React, { useEffect, useState, useCallback } from 'react';
import {
  ShieldCheck, Users, ClipboardList, Save, RefreshCw, AlertTriangle, CheckCircle2, Lock
} from 'lucide-react';
import { ACTOR_GROUP_LABELS, apiFetch, useAuthRbac } from '../contexts/AuthRbacContext';

interface RoleRuleItem {
  id: string;
  branchId?: string | null;
  roleCode: string;
  actorGroup: number;
  moduleKey: string;
  tabKey: string;
  canView: boolean;
  canCreate: boolean;
  canEdit: boolean;
  canDelete: boolean;
  canSign: boolean;
  canPrint: boolean;
  canFinalize: boolean;
  permissionVersion: number;
}

interface StaffCapabilityItem {
  staffId: string;
  userId?: string;
  userName: string;
  staffCode: string;
  fullName: string;
  roleCode: string;
  actorGroup: number;
  primarySpecialty: string;
  secondarySpecialties: string;
  practiceCertificateCode?: string;
  canOrderCls: boolean;
  canPrescribe: boolean;
  canFinalizeHealthCheckup: boolean;
  canViewFullClinicalHistory: boolean;
  assignedRoomIds: string;
  isActive: boolean;
}

interface AuditLogItem {
  id: string;
  userName: string;
  branchCode: string;
  action: string;
  entityName: string;
  entityId: string;
  maskedDetails: string;
  details: string;
  timestampUtc: string;
}

const TAB_LABELS: Record<string, string> = {
  tab_queue_room: 'Hàng đợi buồng khám',
  tab_history: 'Lịch sử bệnh án cũ',
  tab_vital_signs: 'Sinh hiệu & Thể lực',
  tab_internal: 'Khám Nội tổng quát',
  tab_specialty_own: 'Chuyên khoa của tôi',
  tab_specialty_other: 'Chuyên khoa khác',
  tab_order_cls: 'Lập chỉ định CLS',
  tab_imaging_capture: 'Chẩn đoán hình ảnh',
  tab_lab_lis: 'Xét nghiệm LIS',
  tab_prescription: 'Kê đơn thuốc',
  tab_lead_summary: 'Tổng kết & Ký HS (BS Nội)',
  tab_admin_acl: 'Quản trị Phân quyền ACL'
};

export const RolePermissionAdminPage: React.FC = () => {
  const { doctorContext, refreshContext } = useAuthRbac();
  const [subTab, setSubTab] = useState<'matrix' | 'staff' | 'audit'>('staff');
  const [rules, setRules] = useState<RoleRuleItem[]>([]);
  const [staffs, setStaffs] = useState<StaffCapabilityItem[]>([]);
  const [auditLogs, setAuditLogs] = useState<AuditLogItem[]>([]);
  const [selectedGroup, setSelectedGroup] = useState<number>(2);
  const [saving, setSaving] = useState(false);
  const [banner, setBanner] = useState<{ type: 'ok' | 'err'; text: string } | null>(null);

  const loadAll = useCallback(async () => {
    try {
      const [mRes, sRes, aRes] = await Promise.all([
        apiFetch('/api/rbac/matrix'),
        apiFetch('/api/rbac/staff-capabilities'),
        apiFetch('/api/rbac/audit-logs?limit=60')
      ]);
      if (mRes.ok) setRules(await mRes.json());
      if (sRes.ok) setStaffs(await sRes.json());
      if (aRes.ok) setAuditLogs(await aRes.json());
    } catch (e: any) {
      setBanner({ type: 'err', text: e?.message || 'Lỗi tải dữ liệu RBAC' });
    }
  }, []);

  useEffect(() => {
    loadAll();
  }, [loadAll]);

  const toggleRuleField = (id: string, field: keyof RoleRuleItem) => {
    setRules(prev => prev.map(r => r.id === id ? { ...r, [field]: !r[field] } : r));
  };

  const handleSaveMatrix = async () => {
    setSaving(true);
    setBanner(null);
    try {
      const groupRules = rules.filter(r => r.actorGroup === selectedGroup);
      const res = await apiFetch('/api/rbac/matrix', {
        method: 'PUT',
        body: JSON.stringify({ branchId: null, rules: groupRules })
      });
      const body = await res.json();
      if (!res.ok) {
        setBanner({ type: 'err', text: body?.error || 'Không có quyền cập nhật Ma trận RBAC (HTTP ' + res.status + ')' });
        return;
      }
      setBanner({ type: 'ok', text: `Đã lưu Ma trận phân quyền cho [${ACTOR_GROUP_LABELS[selectedGroup]}] và xóa cache RBAC.` });
      await refreshContext();
      await loadAll();
    } finally {
      setSaving(false);
    }
  };

  const handleSaveStaff = async (st: StaffCapabilityItem) => {
    setSaving(true);
    setBanner(null);
    try {
      const res = await apiFetch(`/api/rbac/staff-capabilities/${st.staffId}`, {
        method: 'PUT',
        body: JSON.stringify({
          actorGroup: st.actorGroup,
          primarySpecialty: st.primarySpecialty,
          secondarySpecialties: st.secondarySpecialties,
          canOrderCls: st.canOrderCls,
          canPrescribe: st.canPrescribe,
          canFinalizeHealthCheckup: st.canFinalizeHealthCheckup,
          canViewFullClinicalHistory: st.canViewFullClinicalHistory,
          assignedRoomIds: st.assignedRoomIds
        })
      });
      const body = await res.json();
      if (!res.ok) {
        setBanner({ type: 'err', text: body?.error || 'Từ chối cập nhật (chỉ Admin mới được sửa năng lực BS)' });
        return;
      }
      setBanner({ type: 'ok', text: `Đã cập nhật phân quyền & phòng khám cho ${st.fullName}.` });
      await refreshContext();
      await loadAll();
    } finally {
      setSaving(false);
    }
  };

  const updateStaffLocal = (staffId: string, patch: Partial<StaffCapabilityItem>) => {
    setStaffs(prev => prev.map(s => s.staffId === staffId ? { ...s, ...patch } : s));
  };

  const isAdmin = doctorContext?.actorGroup === 0 ||
    ['super_admin', 'admin_chain', 'admin_branch', 'it_admin'].includes(doctorContext?.roleCode || '');

  return (
    <div className="flex-1 overflow-auto bg-[#f4f6f9] p-4 space-y-4">
      <div className="bg-white border border-[#dee2e6] rounded-lg p-4 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-base font-bold text-[#18509f] flex items-center gap-2">
            <ShieldCheck className="w-5 h-5" />
            <span>Phân Quyền Bác Sĩ 2 Tầng, Phạm Vi Chuyên Khoa & Nhật Ký Bảo Mật</span>
          </h2>
          <p className="text-xs text-[#6c757d] mt-0.5">
            Tài khoản hiện tại: <strong>{doctorContext?.fullName}</strong> ({doctorContext?.userName}) — Nhóm: <strong>{ACTOR_GROUP_LABELS[doctorContext?.actorGroup || 0]}</strong>
          </p>
        </div>
        <button
          type="button"
          onClick={loadAll}
          className="px-3 py-1.5 text-xs bg-white border border-[#ced4da] hover:bg-[#f1f3f6] rounded flex items-center gap-1.5 cursor-pointer"
        >
          <RefreshCw className="w-3.5 h-3.5" /> Làm mới
        </button>
      </div>

      {!isAdmin && (
        <div className="p-3 bg-amber-50 border border-amber-300 rounded-lg text-xs text-amber-900 flex items-center gap-2">
          <Lock className="w-4 h-4 shrink-0 text-amber-700" />
          <span>Bạn đang đăng nhập bằng tài khoản chuyên môn (không phải Quản trị viên). Server sẽ chặn lưu thay đổi phân quyền (HTTP 403). Hãy chuyển sang tài khoản <strong>admin.cnq1</strong> ở góc trên bên phải để cấu hình.</span>
        </div>
      )}

      {banner && (
        <div className={`p-3 rounded-lg border text-xs flex items-center gap-2 ${
          banner.type === 'ok' ? 'bg-emerald-50 border-emerald-300 text-emerald-900' : 'bg-rose-50 border-rose-300 text-rose-900'
        }`}>
          {banner.type === 'ok' ? <CheckCircle2 className="w-4 h-4 shrink-0" /> : <AlertTriangle className="w-4 h-4 shrink-0" />}
          <span>{banner.text}</span>
        </div>
      )}

      <div className="flex gap-2 border-b border-[#dee2e6] bg-white px-3 pt-2 rounded-t-lg">
        <button
          type="button"
          onClick={() => setSubTab('staff')}
          className={`px-3 py-2 text-xs font-semibold border-b-2 flex items-center gap-1.5 cursor-pointer ${
            subTab === 'staff' ? 'border-[#1f63c9] text-[#1f63c9]' : 'border-transparent text-[#6c757d]'
          }`}
        >
          <Users className="w-3.5 h-3.5" /> Phạm vi Chuyên khoa & Buồng khám (Staff)
        </button>
        <button
          type="button"
          onClick={() => setSubTab('matrix')}
          className={`px-3 py-2 text-xs font-semibold border-b-2 flex items-center gap-1.5 cursor-pointer ${
            subTab === 'matrix' ? 'border-[#1f63c9] text-[#1f63c9]' : 'border-transparent text-[#6c757d]'
          }`}
        >
          <ShieldCheck className="w-3.5 h-3.5" /> Ma trận Nhóm Quyền × Tab × 7 Thao tác
        </button>
        <button
          type="button"
          onClick={() => setSubTab('audit')}
          className={`px-3 py-2 text-xs font-semibold border-b-2 flex items-center gap-1.5 cursor-pointer ${
            subTab === 'audit' ? 'border-[#1f63c9] text-[#1f63c9]' : 'border-transparent text-[#6c757d]'
          }`}
        >
          <ClipboardList className="w-3.5 h-3.5" /> Nhật ký Vi phạm & Mở khóa ({auditLogs.length})
        </button>
      </div>

      {subTab === 'staff' && (
        <div className="bg-white border border-[#dee2e6] rounded-b-lg overflow-auto">
          <table className="w-full text-xs border-collapse">
            <thead className="bg-[#f8f9fa] text-[#495057] border-b border-[#dee2e6]">
              <tr>
                <th className="p-2.5 text-left">Tài khoản / Nhân sự</th>
                <th className="p-2.5 text-left">Nhóm Bác sĩ</th>
                <th className="p-2.5 text-left">Chuyên khoa chính</th>
                <th className="p-2.5 text-left">Buồng khám gán</th>
                <th className="p-2.5 text-center">Lập CĐ</th>
                <th className="p-2.5 text-center">Kê toa</th>
                <th className="p-2.5 text-center">Kết luận HS</th>
                <th className="p-2.5 text-center">Xem LS cũ</th>
                <th className="p-2.5 text-right">Lưu</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#eef0f3]">
              {staffs.map(st => (
                <tr key={st.staffId} className="hover:bg-[#f8fbff]">
                  <td className="p-2.5">
                    <div className="font-bold text-[#212529]">{st.fullName}</div>
                    <div className="text-[11px] font-mono text-[#1f63c9]">{st.userName || st.staffCode} • CCHN: {st.practiceCertificateCode || '—'}</div>
                  </td>
                  <td className="p-2.5">
                    <select
                      value={st.actorGroup}
                      onChange={e => updateStaffLocal(st.staffId, { actorGroup: Number(e.target.value) })}
                      className="p-1.5 border border-[#ced4da] rounded bg-white text-xs"
                    >
                      {Object.entries(ACTOR_GROUP_LABELS).map(([k, v]) => (
                        <option key={k} value={Number(k)}>{v}</option>
                      ))}
                    </select>
                  </td>
                  <td className="p-2.5">
                    <input
                      type="text"
                      value={st.primarySpecialty}
                      onChange={e => updateStaffLocal(st.staffId, { primarySpecialty: e.target.value })}
                      className="w-36 p-1.5 border border-[#ced4da] rounded text-xs"
                      placeholder="Ophthalmology..."
                    />
                  </td>
                  <td className="p-2.5">
                    <input
                      type="text"
                      value={st.assignedRoomIds}
                      onChange={e => updateStaffLocal(st.staffId, { assignedRoomIds: e.target.value })}
                      className="w-32 p-1.5 border border-[#ced4da] rounded font-mono text-xs"
                      placeholder="P.101, P.TK01"
                    />
                  </td>
                  <td className="p-2.5 text-center">
                    <input type="checkbox" checked={st.canOrderCls} onChange={e => updateStaffLocal(st.staffId, { canOrderCls: e.target.checked })} />
                  </td>
                  <td className="p-2.5 text-center">
                    <input type="checkbox" checked={st.canPrescribe} onChange={e => updateStaffLocal(st.staffId, { canPrescribe: e.target.checked })} />
                  </td>
                  <td className="p-2.5 text-center">
                    <input type="checkbox" checked={st.canFinalizeHealthCheckup} onChange={e => updateStaffLocal(st.staffId, { canFinalizeHealthCheckup: e.target.checked })} />
                  </td>
                  <td className="p-2.5 text-center">
                    <input type="checkbox" checked={st.canViewFullClinicalHistory} onChange={e => updateStaffLocal(st.staffId, { canViewFullClinicalHistory: e.target.checked })} />
                  </td>
                  <td className="p-2.5 text-right">
                    <button
                      type="button"
                      disabled={saving}
                      onClick={() => handleSaveStaff(st)}
                      className="px-2.5 py-1 bg-[#1f63c9] hover:bg-[#1852a6] text-white rounded text-[11px] font-semibold cursor-pointer inline-flex items-center gap-1"
                    >
                      <Save className="w-3 h-3" /> Lưu
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {subTab === 'matrix' && (
        <div className="bg-white border border-[#dee2e6] rounded-b-lg p-4 space-y-3">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="flex flex-wrap gap-1.5">
              {Object.entries(ACTOR_GROUP_LABELS).map(([k, v]) => (
                <button
                  key={k}
                  type="button"
                  onClick={() => setSelectedGroup(Number(k))}
                  className={`px-3 py-1.5 rounded text-xs font-semibold border cursor-pointer ${
                    selectedGroup === Number(k)
                      ? 'bg-[#1f63c9] text-white border-[#1f63c9]'
                      : 'bg-[#f8f9fa] text-[#495057] border-[#ced4da]'
                  }`}
                >
                  {v}
                </button>
              ))}
            </div>
            <button
              type="button"
              disabled={saving}
              onClick={handleSaveMatrix}
              className="px-3 py-1.5 bg-[#0f766e] hover:bg-[#115e59] text-white rounded text-xs font-bold flex items-center gap-1.5 cursor-pointer"
            >
              <Save className="w-3.5 h-3.5" /> Lưu Ma trận Nhóm này
            </button>
          </div>

          <table className="w-full text-xs border-collapse border border-[#dee2e6]">
            <thead className="bg-[#f8f9fa]">
              <tr>
                <th className="p-2 text-left border-b border-[#dee2e6]">Màn hình / Tab</th>
                <th className="p-2 text-center border-b border-[#dee2e6]">Xem</th>
                <th className="p-2 text-center border-b border-[#dee2e6]">Tạo</th>
                <th className="p-2 text-center border-b border-[#dee2e6]">Sửa</th>
                <th className="p-2 text-center border-b border-[#dee2e6]">Hủy/Xóa</th>
                <th className="p-2 text-center border-b border-[#dee2e6]">Ký số</th>
                <th className="p-2 text-center border-b border-[#dee2e6]">In phiếu</th>
                <th className="p-2 text-center border-b border-[#dee2e6]">Kết luận HS</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#eef0f3]">
              {rules.filter(r => r.actorGroup === selectedGroup).map(r => (
                <tr key={r.id} className="hover:bg-[#f8fbff]">
                  <td className="p-2 font-semibold">
                    <div>{TAB_LABELS[r.tabKey] || r.tabKey}</div>
                    <div className="text-[10px] font-mono text-[#6c757d]">{r.tabKey}</div>
                  </td>
                  {(['canView', 'canCreate', 'canEdit', 'canDelete', 'canSign', 'canPrint', 'canFinalize'] as const).map(f => (
                    <td key={f} className="p-2 text-center">
                      <input type="checkbox" checked={Boolean(r[f])} onChange={() => toggleRuleField(r.id, f)} />
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {subTab === 'audit' && (
        <div className="bg-white border border-[#dee2e6] rounded-b-lg overflow-auto">
          <table className="w-full text-xs border-collapse">
            <thead className="bg-[#f8f9fa] border-b border-[#dee2e6]">
              <tr>
                <th className="p-2.5 text-left">Thời gian (UTC)</th>
                <th className="p-2.5 text-left">Người thực hiện</th>
                <th className="p-2.5 text-left">Hành động</th>
                <th className="p-2.5 text-left">Đối tượng</th>
                <th className="p-2.5 text-left">Chi tiết</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#eef0f3]">
              {auditLogs.map(log => (
                <tr key={log.id} className="hover:bg-[#f8fbff]">
                  <td className="p-2.5 font-mono text-[11px]">{new Date(log.timestampUtc).toLocaleString('vi-VN')}</td>
                  <td className="p-2.5 font-semibold">{log.userName} ({log.branchCode})</td>
                  <td className="p-2.5">
                    <span className={`px-2 py-0.5 rounded font-mono text-[10px] font-bold ${
                      log.action.includes('DENIED') || log.action.includes('VIOLATION')
                        ? 'bg-rose-100 text-rose-800'
                        : log.action.includes('UNLOCK')
                          ? 'bg-amber-100 text-amber-800'
                          : 'bg-blue-100 text-blue-800'
                    }`}>
                      {log.action}
                    </span>
                  </td>
                  <td className="p-2.5 font-mono text-[11px]">{log.entityName} #{log.entityId}</td>
                  <td className="p-2.5 text-[#495057]">{log.details || log.maskedDetails}</td>
                </tr>
              ))}
              {auditLogs.length === 0 && (
                <tr>
                  <td colSpan={5} className="p-6 text-center text-[#6c757d]">Chưa có nhật ký vi phạm / mở khóa nào.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};
