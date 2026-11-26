// ==============================================================
// B2B API Service: Kết nối REST API Backend & LocalStorage Resilient Fallback
// Chuẩn OPC Definition of Done & Clean Architecture SmartClinic
// ==============================================================

import { B2BCompanyMaster, B2BCompanyPackage, PackageServiceItem, PackageTargetGroup, MASTER_COMPANIES, SCM_B2B_COMPANIES_STORAGE_KEY, SCM_B2B_PACKAGES_STORAGE_KEY, SCM_WAVE04_HANDOFF_KEY } from '../pages/B2BPackageManagementPage';

const API_BASE = '/api/b2b';

export const b2bApiService = {
  // ── 1. DOANH NGHIỆP & TIẾP ĐẦU NGỮ PREFIX (B2B COMPANIES) ──

  async getCompanies(): Promise<B2BCompanyMaster[]> {
    try {
      const res = await fetch(`${API_BASE}/companies`, { signal: AbortSignal.timeout(3000) });
      if (res.ok) {
        const data = await res.json();
        if (Array.isArray(data) && data.length > 0) {
          const mapped: B2BCompanyMaster[] = data.map(c => ({
            id: c.id || c.Id,
            code: c.code || c.Code,
            name: c.name || c.Name,
            taxCode: c.taxCode || c.TaxCode || '',
            address: c.address || c.Address || '',
            contactPerson: c.contactPerson || c.ContactPerson || '',
            phone: c.contactPhone || c.ContactPhone || '',
            contactEmail: c.contactEmail || c.ContactEmail || ''
          }));
          // Sync sang local cache
          localStorage.setItem(SCM_B2B_COMPANIES_STORAGE_KEY, JSON.stringify(mapped));
          return mapped;
        }
      }
    } catch {
      // Graceful fallback: Backend offline hoặc timeout -> dùng LocalStorage
    }

    try {
      const saved = localStorage.getItem(SCM_B2B_COMPANIES_STORAGE_KEY);
      if (saved) return JSON.parse(saved);
    } catch (e) {
      console.error('Lỗi đọc companies từ LocalStorage', e);
    }
    return MASTER_COMPANIES;
  },

  async saveCompany(company: B2BCompanyMaster): Promise<B2BCompanyMaster> {
    let savedOnline = false;
    try {
      const isEdit = company.id && !company.id.startsWith('comp-');
      const url = isEdit ? `${API_BASE}/companies/${company.id}` : `${API_BASE}/companies`;
      const method = isEdit ? 'PUT' : 'POST';

      const payload = {
        code: company.code.trim().toUpperCase(),
        name: company.name.trim(),
        taxCode: company.taxCode,
        address: company.address,
        contactPerson: company.contactPerson,
        contactPhone: company.phone,
        contactEmail: company.contactEmail?.trim() || '',
        creditLimit: 100000000,
        isActive: true
      };

      const res = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
        signal: AbortSignal.timeout(3000)
      });

      if (res.ok) {
        const data = await res.json();
        company = {
          ...company,
          id: data.id || company.id
        };
        savedOnline = true;
      }
    } catch {
      // Backend offline -> Tiếp tục lưu vào LocalStorage
    }

    // Luôn lưu bền vững vào LocalStorage (Chống mất dữ liệu khi F5)
    try {
      const companies = await this.getCompanies();
      const existingIdx = companies.findIndex(c => c.id === company.id || c.code === company.code);
      let updated: B2BCompanyMaster[];
      if (existingIdx >= 0) {
        updated = [...companies];
        updated[existingIdx] = company;
      } else {
        updated = [company, ...companies];
      }
      localStorage.setItem(SCM_B2B_COMPANIES_STORAGE_KEY, JSON.stringify(updated));
    } catch (e) {
      console.error('Lỗi ghi company vào LocalStorage', e);
    }

    return company;
  },

  async deleteCompany(id: string): Promise<boolean> {
    try {
      await fetch(`${API_BASE}/companies/${id}`, {
        method: 'DELETE',
        signal: AbortSignal.timeout(3000)
      });
    } catch {
      // Fallback
    }

    try {
      const companies = await this.getCompanies();
      const updated = companies.filter(c => c.id !== id);
      localStorage.setItem(SCM_B2B_COMPANIES_STORAGE_KEY, JSON.stringify(updated));
      return true;
    } catch {
      return false;
    }
  },

  // ── 2. GÓI KHÁM DOANH NGHIỆP (B2B PACKAGES) ──

  async getPackages(): Promise<B2BCompanyPackage[]> {
    try {
      const res = await fetch(`${API_BASE}/packages`, { signal: AbortSignal.timeout(3000) });
      if (res.ok) {
        const data = await res.json();
        if (Array.isArray(data) && data.length > 0) {
          const companies = await this.getCompanies();
          const mapped: B2BCompanyPackage[] = data.map(p => {
            const comp = companies.find(c => c.id === p.companyId);
            return {
              id: p.id || p.Id,
              code: p.code || p.Code,
              name: p.name || p.Name,
              companyId: p.companyId || p.CompanyId || '',
              companyCode: comp?.code || '',
              companyName: comp?.name || p.companyName || 'Doanh Nghiệp B2B',
              contractNumber: p.contractNumber || `HĐ-${p.code || 'B2B'}`,
              contractType: p.contractType === 1 ? 'PhatSinh' : 'TronGoi',
              status: p.status === 3 ? 'ChuyenDSKham' : p.status === 2 ? 'NhapDSKH' : p.status === 1 ? 'Duyet' : 'KhoiTao',
              year: p.createdAt ? new Date(p.createdAt).getFullYear() : 2026,
              startDate: '2026-01-01',
              endDate: '2026-12-31',
              isOutsourceExam: false,
              isBilingualReport: false,
              targetGroups: (p.targetGroups || []).map((tg: any) => ({
                id: tg.id,
                code: tg.code || '',
                name: tg.name || '',
                orderIndex: tg.orderIndex || 1,
                isActive: tg.isActive ?? true
              })),
              services: (p.services || []).map((s: any) => ({
                id: s.id,
                packageId: p.id,
                serviceId: s.serviceId,
                targetGroupId: s.packageTargetGroupId || s.targetGroupId || undefined,
                serviceCode: s.serviceCode || '',
                serviceName: s.serviceName || '',
                category: 'Khám Lâm Sàng',
                orderIndex: s.orderIndex || 1,
                groupChoiceCount: s.groupChoiceCount || 1,
                registerType: s.registerType || 1,
                isSpecimenOutsource: s.isSpecimenOutsource || false,
                originalPrice: s.originalPrice || 0,
                dealPrice: s.dealPrice || s.originalPrice || 0,
                floorPrice: s.floorPrice || 0,
                isFloorPriceApproved: s.isFloorPriceApproved || false
              }))
            };
          });
          localStorage.setItem(SCM_B2B_PACKAGES_STORAGE_KEY, JSON.stringify(mapped));
          return mapped;
        }
      }
    } catch {
      // Fallback LocalStorage
    }

    try {
      const saved = localStorage.getItem(SCM_B2B_PACKAGES_STORAGE_KEY);
      if (saved) return JSON.parse(saved);
    } catch (e) {
      console.error('Lỗi đọc packages từ LocalStorage', e);
    }
    return [];
  },

  async savePackage(pkg: B2BCompanyPackage): Promise<B2BCompanyPackage> {
    let persistedPkg = pkg;
    try {
      const isEdit = pkg.id && !pkg.id.startsWith('pkg-') && pkg.id.length === 36;
      const url = isEdit ? `${API_BASE}/packages/${pkg.id}` : `${API_BASE}/packages`;
      const method = isEdit ? 'PUT' : 'POST';

      const payload = {
        code: pkg.code,
        name: pkg.name,
        companyId: pkg.companyId && pkg.companyId.length === 36 ? pkg.companyId : null,
        contractId: null,
        contractType: pkg.contractType === 'PhatSinh' ? 1 : 0,
        isNationalSyncRequired: true,
        nationalTemplateCode: 'M3'
      };

      const res = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
        signal: AbortSignal.timeout(3000)
      });
      if (res.ok) {
        const saved = await res.json();
        persistedPkg = { ...pkg, id: saved.id || saved.Id || pkg.id };
      }

      if (persistedPkg.id && persistedPkg.id.length === 36) {
        for (const tg of (pkg.targetGroups || [])) {
          if (!tg.id || tg.id.length !== 36) {
            await fetch(`${API_BASE}/packages/${persistedPkg.id}/target-groups`, {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({
                packageId: persistedPkg.id,
                code: tg.code,
                name: tg.name,
                orderIndex: tg.orderIndex || 1,
                isActive: tg.isActive ?? true
              })
            }).catch(() => {});
          }
        }

        const newServices = (pkg.services || []).filter(s => !s.id || s.id.length !== 36);
        if (newServices.length > 0) {
          const svcRes = await fetch(`${API_BASE}/packages/${persistedPkg.id}/services/batch`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
              packageId: persistedPkg.id,
              services: newServices.map(s => ({
                packageId: persistedPkg.id,
                serviceId: s.serviceId && s.serviceId.length === 36 ? s.serviceId : '00000000-0000-0000-0000-000000000001',
                serviceCode: s.serviceCode,
                serviceName: s.serviceName,
                originalPrice: s.originalPrice,
                dealPrice: s.dealPrice,
                floorPrice: s.floorPrice,
                quantity: 1,
                orderIndex: s.orderIndex || 1,
                registerType: s.registerType || 1,
                isSpecimenOutsource: s.isSpecimenOutsource || false
              }))
            })
          }).catch(() => null);
          if (svcRes && svcRes.ok) {
            const addedDtos = await svcRes.json();
            if (Array.isArray(addedDtos)) {
              const updatedServices = (pkg.services || []).map(s => {
                const matched = addedDtos.find((d: any) => d.serviceCode === s.serviceCode && (!s.id || s.id.length !== 36));
                return matched ? { ...s, id: matched.id || s.id, packageId: persistedPkg.id } : s;
              });
              persistedPkg = { ...persistedPkg, services: updatedServices };
            }
          }
        }
      }
    } catch {
      // Fallback
    }

    // Luôn lưu bền vững vào LocalStorage
    try {
      const list = await this.getPackages();
      const idx = list.findIndex(p => p.id === persistedPkg.id || p.code === persistedPkg.code);
      let updated: B2BCompanyPackage[];
      if (idx >= 0) {
        updated = [...list];
        updated[idx] = persistedPkg;
      } else {
        updated = [persistedPkg, ...list];
      }
      localStorage.setItem(SCM_B2B_PACKAGES_STORAGE_KEY, JSON.stringify(updated));
    } catch (e) {
      console.error('Lỗi ghi package vào LocalStorage', e);
    }

    return persistedPkg;
  },

  async deletePackage(id: string): Promise<boolean> {
    try {
      await fetch(`${API_BASE}/packages/${id}`, {
        method: 'DELETE',
        signal: AbortSignal.timeout(3000)
      });
    } catch {
      // Fallback
    }

    try {
      const list = await this.getPackages();
      const updated = list.filter(p => p.id !== id);
      localStorage.setItem(SCM_B2B_PACKAGES_STORAGE_KEY, JSON.stringify(updated));
      return true;
    } catch {
      return false;
    }
  },

  // ── 2.1 DANH SÁCH NHÂN VIÊN ĐĂNG KÝ TRONG GÓI KHÁM ──

  async getPackageEmployees(packageId: string): Promise<any[]> {
    try {
      const res = await fetch(`/api/b2b/packages/${packageId}/employees`, { signal: AbortSignal.timeout(3000) });
      if (res.ok) {
        const data = await res.json();
        if (Array.isArray(data)) return data;
      }
    } catch {
      // Graceful fallback
    }
    return [];
  },

  // ── 3. CHUYỂN TIẾP QUY TRÌNH (HANDOFF SANG WAVE 4 KIOSK TIẾP ĐÓN) ──

  async handoffToWave4Kiosk(pkg: B2BCompanyPackage, employees: any[]): Promise<{ count: number; queueKey: string }> {
    const handoffPayload = {
      packageId: pkg.id,
      packageCode: pkg.code,
      packageName: pkg.name,
      companyId: pkg.companyId,
      companyName: pkg.companyName,
      status: 'ReadyForCheckin',
      handedOverAt: new Date().toISOString(),
      employees: employees.map(emp => ({
        id: emp.id,
        code: emp.code,
        name: emp.name,
        idNumber: emp.idNumber,
        birthYear: emp.birthYear,
        gender: emp.gender,
        department: emp.department,
        packageCode: pkg.code,
        companyName: pkg.companyName,
        isSampleCollected: emp.isSampleCollected || false
      }))
    };

    // 1. Lưu vào queue roster của Kiosk
    localStorage.setItem(SCM_WAVE04_HANDOFF_KEY, JSON.stringify(handoffPayload));

    // 2. Thử gọi API Kiosk nếu có backend tiếp nhận
    try {
      await fetch('/api/reception/b2b-handoff', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(handoffPayload),
        signal: AbortSignal.timeout(3000)
      });
    } catch {
      // Offline fallback ok
    }

    return {
      count: employees.length,
      queueKey: SCM_WAVE04_HANDOFF_KEY
    };
  }
};
