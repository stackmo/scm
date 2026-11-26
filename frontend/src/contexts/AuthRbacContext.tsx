import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';

export interface EffectiveTabPermission {
  moduleKey: string;
  tabKey: string;
  canView: boolean;
  canCreate: boolean;
  canEdit: boolean;
  canDelete: boolean;
  canSign: boolean;
  canPrint: boolean;
  canFinalize: boolean;
}

export interface AssignedRoom {
  roomCode: string;
  roomName: string;
  roomType: number;
}

export type PermissionActionName = 'View' | 'Create' | 'Edit' | 'Delete' | 'Sign' | 'Print' | 'Finalize';

export interface CurrentDoctorContextData {
  isAuthenticated: boolean;
  userId?: string;
  userName: string;
  fullName: string;
  roleCode: string;
  actorGroup: number;
  staffId?: string;
  staffCode: string;
  branchCode: string;
  primarySpecialty: string;
  secondarySpecialties: string[];
  canOrderCls: boolean;
  canPrescribe: boolean;
  canFinalizeHealthCheckup: boolean;
  canViewFullClinicalHistory: boolean;
  hasValidLicense: boolean;
  licenseNo?: string;
  defaultClinicalTabKey: string;
  assignedRooms: AssignedRoom[];
  effectivePermissions: Record<string, EffectiveTabPermission>;
  permissionVersion: number;
}

export interface SeededAccountItem {
  id: string;
  userName: string;
  fullName: string;
  role: string;
  actorGroup: number;
  branchCode: string;
  staffId?: string;
  staffCode?: string;
  primarySpecialty?: string;
  assignedRoomIds?: string;
  canOrderCls?: boolean;
  canPrescribe?: boolean;
  canFinalizeHealthCheckup?: boolean;
  isActive: boolean;
}

export const ACTOR_GROUP_LABELS: Record<number, string> = {
  0: 'Quản trị Phòng khám (Admin)',
  1: 'BS Nội Tổng quát / Kết luận HS',
  2: 'BS Chuyên khoa lẻ',
  3: 'BS Chẩn đoán Hình ảnh',
  4: 'BS Xét nghiệm / LIS',
  5: 'KTV CLS / Điều dưỡng'
};

const STORAGE_USER_ID = 'scm_auth_user_id';
const STORAGE_USER_NAME = 'scm_auth_user_name';
const STORAGE_ROLE_CODE = 'scm_current_role';
const STORAGE_BRANCH_CODE = 'scm_active_branch';

export function buildAuthHeaders(extra?: HeadersInit): Headers {
  const headers = new Headers(extra || {});
  const userId = localStorage.getItem(STORAGE_USER_ID);
  const userName = localStorage.getItem(STORAGE_USER_NAME);
  const branchCode = localStorage.getItem(STORAGE_BRANCH_CODE) || 'CNQ1';
  const roleCode = localStorage.getItem(STORAGE_ROLE_CODE) || 'doctor';

  if (userId) headers.set('X-User-Id', userId);
  if (userName) headers.set('X-User-Name', userName);
  headers.set('X-Branch-Code', branchCode);
  headers.set('X-User-Role', roleCode);
  return headers;
}

export async function apiFetch(input: RequestInfo | URL, init: RequestInit = {}): Promise<Response> {
  const headers = buildAuthHeaders(init.headers);
  if (init.body && typeof init.body === 'string' && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }
  return fetch(input, { ...init, headers });
}

interface AuthRbacContextValue {
  doctorContext: CurrentDoctorContextData | null;
  accounts: SeededAccountItem[];
  loadingAuth: boolean;
  login: (userName: string, password: string) => Promise<{ success: boolean; message: string }>;
  switchAccount: (userName: string) => Promise<{ success: boolean; message: string }>;
  refreshContext: () => Promise<void>;
  hasPermission: (tabKey: string, action: PermissionActionName) => boolean;
  canExamineSpecialty: (tabKeyOrSpecialty: string) => boolean;
}

const AuthRbacContext = createContext<AuthRbacContextValue | undefined>(undefined);

const TAB_TO_SPECIALTY_ALIASES: Record<string, string[]> = {
  internal: ['internalmedicine', 'noi', 'noitongquat', 'noikhoa'],
  tab_internal: ['internalmedicine', 'noi', 'noitongquat', 'noikhoa'],
  surgery: ['surgery', 'ngoai', 'ngoaikhoa', 'ngoaitongquat'],
  eye: ['ophthalmology', 'mat', 'nhankhoa', 'matnhankhoa'],
  ent: ['otorhinolaryngology', 'taimuihong', 'tmh', 'ent'],
  dental: ['dentomaxillofacial', 'ranghammat', 'rhm', 'dental'],
  dermatology: ['dermatology', 'dalieu'],
  obgyn: ['obstetricsgynecology', 'sanphukhoa', 'san', 'obgyn']
};

function normalizeSpecialtyText(val: string): string {
  return (val || '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/đ/g, 'd')
    .replace(/Đ/g, 'd')
    .toLowerCase()
    .replace(/[^a-z0-9]/g, '');
}

export const AuthRbacProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [doctorContext, setDoctorContext] = useState<CurrentDoctorContextData | null>(null);
  const [accounts, setAccounts] = useState<SeededAccountItem[]>([]);
  const [loadingAuth, setLoadingAuth] = useState(true);

  const persistContext = (ctx: CurrentDoctorContextData) => {
    if (ctx.userId) localStorage.setItem(STORAGE_USER_ID, ctx.userId);
    if (ctx.userName) localStorage.setItem(STORAGE_USER_NAME, ctx.userName);
    if (ctx.roleCode) localStorage.setItem(STORAGE_ROLE_CODE, ctx.roleCode);
    setDoctorContext(ctx);
  };

  const refreshContext = useCallback(async () => {
    try {
      const [usersRes, ctxRes] = await Promise.all([
        apiFetch('/api/master-data/users'),
        apiFetch('/api/rbac/my-context')
      ]);

      if (usersRes.ok) {
        const list = await usersRes.json();
        setAccounts(Array.isArray(list) ? list : []);
      }

      if (ctxRes.ok) {
        const ctx: CurrentDoctorContextData = await ctxRes.json();
        if (ctx.isAuthenticated) {
          persistContext(ctx);
          return;
        }
      }

      // First visit: auto-sign in with default Lead Doctor bs.noi.chanh so demo works immediately
      const storedUser = localStorage.getItem(STORAGE_USER_NAME) || 'bs.noi.chanh';
      const loginRes = await fetch('/api/rbac/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userName: storedUser, password: 'Scm@2026' })
      });
      if (loginRes.ok) {
        const data = await loginRes.json();
        if (data?.success && data?.context) {
          persistContext(data.context);
        }
      }
    } catch {
      // Offline / backend not running fallback stays graceful
    } finally {
      setLoadingAuth(false);
    }
  }, []);

  useEffect(() => {
    refreshContext();
  }, [refreshContext]);

  const login = useCallback(async (userName: string, password: string) => {
    try {
      const res = await fetch('/api/rbac/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userName, password })
      });
      const data = await res.json();
      if (res.ok && data?.success && data?.context) {
        persistContext(data.context);
        return { success: true, message: data.message || 'Đăng nhập thành công' };
      }
      return { success: false, message: data?.message || 'Đăng nhập thất bại' };
    } catch (err: any) {
      return { success: false, message: err?.message || 'Không kết nối được máy chủ' };
    }
  }, []);

  const switchAccount = useCallback(async (userName: string) => {
    return login(userName, 'Scm@2026');
  }, [login]);

  const hasPermission = useCallback((tabKey: string, action: PermissionActionName) => {
    if (!doctorContext?.isAuthenticated) return false;
    const perm = doctorContext.effectivePermissions?.[tabKey];
    if (!perm) return false;
    switch (action) {
      case 'View': return perm.canView;
      case 'Create': return perm.canCreate;
      case 'Edit': return perm.canEdit;
      case 'Delete': return perm.canDelete;
      case 'Sign': return perm.canSign;
      case 'Print': return perm.canPrint;
      case 'Finalize': return perm.canFinalize;
      default: return false;
    }
  }, [doctorContext]);

  const canExamineSpecialty = useCallback((tabKeyOrSpecialty: string) => {
    if (!doctorContext?.isAuthenticated) return false;
    if (doctorContext.actorGroup === 0) return true; // ClinicalAdmin override
    if ([3, 4, 5].includes(doctorContext.actorGroup)) return false; // Radiology/Lab/Tech cannot examine clinical

    const aliases = TAB_TO_SPECIALTY_ALIASES[tabKeyOrSpecialty] || [normalizeSpecialtyText(tabKeyOrSpecialty)];
    if (doctorContext.actorGroup === 1 && aliases.includes('internalmedicine')) return true;

    const mine = [doctorContext.primarySpecialty, ...(doctorContext.secondarySpecialties || [])]
      .filter(Boolean)
      .map(normalizeSpecialtyText);

    return mine.some(m => aliases.includes(m));
  }, [doctorContext]);

  return (
    <AuthRbacContext.Provider value={{
      doctorContext,
      accounts,
      loadingAuth,
      login,
      switchAccount,
      refreshContext,
      hasPermission,
      canExamineSpecialty
    }}>
      {children}
    </AuthRbacContext.Provider>
  );
};

export function useAuthRbac(): AuthRbacContextValue {
  const ctx = useContext(AuthRbacContext);
  if (!ctx) {
    throw new Error('useAuthRbac must be used inside AuthRbacProvider');
  }
  return ctx;
}
