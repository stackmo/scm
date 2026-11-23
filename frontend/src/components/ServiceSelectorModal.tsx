import React, { useState, useEffect, useRef, useMemo } from 'react';

export interface ServiceGroupItem {
  id: string;
  groupCode: string;
  groupName: string;
  serviceType: number;
  sortOrder: number;
  isActive: boolean;
  serviceCount: number;
}

export interface MedicalServiceItem {
  id: string;
  serviceCode: string;
  serviceName: string;
  serviceGroupId: string;
  serviceGroupName?: string;
  serviceGroupCode?: string;
  unit: string;
  standardPrice: number;
  insurancePrice: number;
  corporatePrice: number;
  legacyId?: number;
  isActive?: boolean;
}

export type ServiceSelectionMode = 'Chon' | 'TuyChon' | 'LamThem';

export interface SelectedServiceEntry {
  service: MedicalServiceItem;
  mode: ServiceSelectionMode;
  isSpecimenOutsource: boolean;
}

interface ServiceSelectorModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (selected: SelectedServiceEntry[]) => void;
  initialSelected?: SelectedServiceEntry[];
}

export const ServiceSelectorModal: React.FC<ServiceSelectorModalProps> = ({
  isOpen,
  onClose,
  onConfirm,
  initialSelected = []
}) => {
  // 1. Hooks - Always declared unconditionally at top level (L1_REACT_HOOK_ORDER)
  const searchInputRef = useRef<HTMLInputElement>(null);
  
  const [serviceGroups, setServiceGroups] = useState<ServiceGroupItem[]>([]);
  const [services, setServices] = useState<MedicalServiceItem[]>([]);
  const [selectedGroupId, setSelectedGroupId] = useState<string>('ALL');
  const [searchText, setSearchText] = useState<string>('');
  const [debouncedSearch, setDebouncedSearch] = useState<string>('');
  
  // Selection mode & filters (VSC standard)
  const [activeSelectionMode, setActiveSelectionMode] = useState<ServiceSelectionMode>('TuyChon');
  const [isSpecimenOutsource, setIsSpecimenOutsource] = useState<boolean>(false);
  const [onlyActiveFilter, setOnlyActiveFilter] = useState<boolean>(true);
  
  // Map of selected items: key = service.id
  const [selectedMap, setSelectedMap] = useState<Map<string, SelectedServiceEntry>>(new Map());

  // Live counters per mode (VSC Live Counters: Chon=Red, TuyChon=Green, LamThem=Blue)
  const countChon = useMemo(() => {
    let count = 0;
    selectedMap.forEach(v => { if (v.mode === 'Chon') count++; });
    return count;
  }, [selectedMap]);

  const countTuyChon = useMemo(() => {
    let count = 0;
    selectedMap.forEach(v => { if (v.mode === 'TuyChon') count++; });
    return count;
  }, [selectedMap]);

  const countLamThem = useMemo(() => {
    let count = 0;
    selectedMap.forEach(v => { if (v.mode === 'LamThem') count++; });
    return count;
  }, [selectedMap]);
  
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  const prevIsOpenRef = useRef<boolean>(false);

  // Sync initialSelected only when opening
  useEffect(() => {
    if (isOpen && !prevIsOpenRef.current) {
      const map = new Map<string, SelectedServiceEntry>();
      if (initialSelected && initialSelected.length > 0) {
        initialSelected.forEach(entry => {
          map.set(entry.service.id, entry);
        });
      }
      setSelectedMap(map);
    }
    prevIsOpenRef.current = isOpen;
  }, [isOpen]);

  // Debounce search input 300ms (L1_SEARCH_UX)
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchText);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchText]);

  // Fetch Service Groups & Services
  const fetchData = async () => {
    setLoading(true);
    setError(null);
    try {
      const [grpRes, srvRes] = await Promise.all([
        fetch('/api/master-data/service-groups'),
        fetch('/api/master-data/services')
      ]);

      if (!grpRes.ok || !srvRes.ok) {
        throw new Error('Không thể tải danh mục dịch vụ kỹ thuật từ máy chủ.');
      }

      const grpData: ServiceGroupItem[] = await grpRes.json();
      const srvData: MedicalServiceItem[] = await srvRes.json();

      setServiceGroups(grpData);
      setServices(srvData);
    } catch (err: any) {
      setError(err.message || 'Đã xảy ra lỗi khi kết nối dữ liệu.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isOpen) {
      fetchData();
    }
  }, [isOpen]);

  // L1_MODAL_ESCAPE: Escape key handler
  useEffect(() => {
    if (!isOpen) return;
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  // L1_MODAL_SCROLL_LOCK: Body scroll lock
  useEffect(() => {
    if (isOpen) {
      const originalOverflow = document.body.style.overflow;
      document.body.style.overflow = 'hidden';
      return () => {
        document.body.style.overflow = originalOverflow;
      };
    }
  }, [isOpen]);

  // L1_MODAL_FOCUS: Auto-focus first input
  useEffect(() => {
    if (isOpen && !loading) {
      setTimeout(() => {
        searchInputRef.current?.focus();
      }, 50);
    }
  }, [isOpen, loading]);

  // Filtered Services List
  const filteredServices = useMemo(() => {
    return services.filter(srv => {
      // Filter by Group
      if (selectedGroupId !== 'ALL' && srv.serviceGroupId !== selectedGroupId) {
        return false;
      }
      // Filter by Active
      if (onlyActiveFilter && srv.isActive === false) {
        return false;
      }
      // Filter by Search Text
      if (debouncedSearch.trim()) {
        const query = debouncedSearch.trim().toLowerCase();
        const codeMatch = srv.serviceCode.toLowerCase().includes(query);
        const nameMatch = srv.serviceName.toLowerCase().includes(query);
        const legacyMatch = srv.legacyId?.toString().includes(query) || false;
        if (!codeMatch && !nameMatch && !legacyMatch) {
          return false;
        }
      }
      return true;
    });
  }, [services, selectedGroupId, onlyActiveFilter, debouncedSearch]);

  // Selection toggle
  const handleToggleService = (srv: MedicalServiceItem) => {
    setSelectedMap(prev => {
      const next = new Map(prev);
      if (next.has(srv.id)) {
        next.delete(srv.id);
      } else {
        next.set(srv.id, {
          service: srv,
          mode: activeSelectionMode,
          isSpecimenOutsource
        });
      }
      return next;
    });
  };

  // Select all in current filtered view
  const handleSelectAllFiltered = (checked: boolean) => {
    setSelectedMap(prev => {
      const next = new Map(prev);
      if (checked) {
        filteredServices.forEach(srv => {
          if (!next.has(srv.id)) {
            next.set(srv.id, {
              service: srv,
              mode: activeSelectionMode,
              isSpecimenOutsource
            });
          }
        });
      } else {
        filteredServices.forEach(srv => {
          next.delete(srv.id);
        });
      }
      return next;
    });
  };

  // Reset selected
  const handleResetSelected = () => {
    setSelectedMap(new Map());
  };

  // Confirm
  const handleConfirm = () => {
    onConfirm(Array.from(selectedMap.values()));
    onClose();
  };

  if (!isOpen) return null;

  const allFilteredSelected = filteredServices.length > 0 && 
    filteredServices.every(s => selectedMap.has(s.id));

  const totalCount = services.length;

  return (
    <div 
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4"
      onClick={(e) => {
        // L1_MODAL_BACKDROP: dismiss on backdrop click
        if (e.target === e.currentTarget) {
          onClose();
        }
      }}
    >
      <div 
        className="w-full max-w-6xl h-[88vh] bg-slate-100 rounded-lg shadow-2xl flex flex-col border border-slate-300 overflow-hidden"
        role="dialog"
        aria-modal="true"
        aria-labelledby="vsc-service-modal-title"
      >
        {/* Window Title Bar (VSC Desktop Style) */}
        <div className="bg-gradient-to-r from-slate-200 via-slate-100 to-slate-200 border-b border-slate-300 px-4 py-2 flex items-center justify-between select-none">
          <div className="flex items-center gap-2">
            <span className="w-5 h-5 rounded-full bg-red-600 text-white font-bold text-xs flex items-center justify-center shadow-xs">
              H
            </span>
            <span id="vsc-service-modal-title" className="font-semibold text-slate-800 text-sm tracking-wide">
              DỊCH VỤ — PHÂN THEO NHÓM CHUYÊN KHOA BỘ Y TẾ
            </span>
            <span className="text-xs bg-emerald-100 text-emerald-800 border border-emerald-300 px-2 py-0.5 rounded-full font-medium ml-2">
              Đã chọn: {selectedMap.size} dịch vụ
            </span>
          </div>
          <button
            onClick={onClose}
            className="w-6 h-6 flex items-center justify-center rounded text-slate-500 hover:text-red-600 hover:bg-slate-200 transition-colors font-bold text-sm cursor-pointer"
            title="Đóng cửa sổ (Esc)"
          >
            ✕
          </button>
        </div>

        {/* Modal Main Content: 2-Column Split */}
        <div className="flex-1 flex overflow-hidden">
          {/* LEFT PANE: NHÓM DỊCH VỤ */}
          <div className="w-72 bg-white border-r border-slate-300 flex flex-col shrink-0">
            <div className="bg-slate-100 px-3 py-2 border-b border-slate-300 font-bold text-xs uppercase tracking-wider text-slate-700 flex items-center justify-between">
              <span>NHÓM DỊCH VỤ</span>
              <span className="text-slate-500 font-mono text-[11px]">({serviceGroups.length})</span>
            </div>

            <div className="flex-1 overflow-y-auto divide-y divide-slate-100 p-1">
              {/* Option: Tất cả nhóm */}
              <button
                type="button"
                onClick={() => setSelectedGroupId('ALL')}
                className={`w-full text-left px-3 py-2 rounded text-xs flex items-center justify-between transition-colors cursor-pointer ${
                  selectedGroupId === 'ALL'
                    ? 'bg-indigo-600 text-white font-semibold shadow-xs'
                    : 'text-slate-700 hover:bg-slate-100'
                }`}
              >
                <span>Tất cả nhóm dịch vụ</span>
                <span className={`text-[10px] px-1.5 py-0.5 rounded-full font-mono ${
                  selectedGroupId === 'ALL' ? 'bg-indigo-700 text-indigo-100' : 'bg-slate-200 text-slate-600'
                }`}>
                  {totalCount}
                </span>
              </button>

              {/* Group List */}
              {serviceGroups.map(grp => {
                const isSelected = selectedGroupId === grp.id;
                return (
                  <button
                    key={grp.id}
                    type="button"
                    onClick={() => setSelectedGroupId(grp.id)}
                    className={`w-full text-left px-3 py-2 rounded text-xs flex items-center justify-between transition-colors cursor-pointer my-0.5 ${
                      isSelected
                        ? 'bg-indigo-600 text-white font-semibold shadow-xs'
                        : 'text-slate-700 hover:bg-slate-100'
                    }`}
                  >
                    <span className="truncate pr-2" title={grp.groupName}>
                      {grp.groupName}
                    </span>
                    <span className={`text-[10px] px-1.5 py-0.5 rounded-full font-mono shrink-0 ${
                      isSelected ? 'bg-indigo-700 text-indigo-100' : 'bg-slate-200 text-slate-600'
                    }`}>
                      {grp.serviceCount}
                    </span>
                  </button>
                );
              })}
            </div>
          </div>

          {/* RIGHT PANE: DANH SÁCH DỊCH VỤ */}
          <div className="flex-1 flex flex-col bg-white overflow-hidden">
            {/* Top Selected Services Summary Board (Chuẩn VSC Desktop Highlight Màu Sắc) */}
            <div className="bg-white border-b border-slate-300 p-3 shrink-0 min-h-[90px] max-h-[140px] overflow-y-auto">
              {selectedMap.size === 0 ? (
                <div className="h-full flex items-center justify-center text-xs text-slate-400 italic py-3 select-none">
                  Chưa có dịch vụ nào được chọn. Chọn chế độ (Chọn / Tùy Chọn / Làm Thêm) bên dưới rồi tích vào bảng để thêm.
                </div>
              ) : (
                <div className="grid grid-cols-2 lg:grid-cols-3 gap-x-4 gap-y-1 text-xs">
                  {Array.from(selectedMap.values()).map(entry => {
                    let colorClass = 'text-red-600';
                    if (entry.mode === 'TuyChon') colorClass = 'text-emerald-700';
                    else if (entry.mode === 'LamThem') colorClass = 'text-blue-700';

                    return (
                      <div
                        key={entry.service.id}
                        onClick={() => handleToggleService(entry.service)}
                        className="flex items-center space-x-1.5 group cursor-pointer hover:bg-slate-50 p-1 rounded transition select-none truncate"
                        title="Click để bỏ chọn dịch vụ này"
                      >
                        <span className={`w-1.5 h-1.5 rounded-full shrink-0 ${
                          entry.mode === 'Chon' ? 'bg-red-600' : entry.mode === 'TuyChon' ? 'bg-emerald-600' : 'bg-blue-600'
                        }`} />
                        <span className={`font-semibold truncate transition group-hover:line-through ${colorClass}`}>
                          {entry.service.serviceName}
                        </span>
                        <span className="text-slate-400 group-hover:text-red-500 text-[10px] shrink-0 font-bold ml-auto pl-1">
                          ✕
                        </span>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>

            {/* Top Toolbar / Filter Banner */}
            <div className="bg-slate-100 border-b border-slate-300 p-2.5 flex flex-col gap-2">
              <div className="text-center font-bold text-xs uppercase text-slate-700 tracking-wide">
                DANH SÁCH DỊCH VỤ TẠI PHÒNG KHÁM ({filteredServices.length})
              </div>

              <div className="flex flex-wrap items-center gap-3 text-xs">
                {/* Search Box */}
                <div className="flex-1 min-w-[240px] relative flex items-center">
                  <input
                    ref={searchInputRef}
                    type="text"
                    value={searchText}
                    onChange={(e) => setSearchText(e.target.value)}
                    placeholder="Tìm theo mã hoặc tên dịch vụ (FDI/BYT)..."
                    className="w-full bg-white border border-slate-300 rounded px-2.5 py-1 text-xs text-slate-800 placeholder-slate-400 focus:outline-hidden focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                  />
                  {searchText && (
                    <button
                      type="button"
                      onClick={() => setSearchText('')}
                      className="absolute right-7 text-slate-400 hover:text-slate-600 px-1 font-bold cursor-pointer"
                      title="Xóa tìm kiếm"
                    >
                      ✕
                    </button>
                  )}
                  <span className="absolute right-2 text-slate-400">
                    🔍
                  </span>
                </div>

                {/* Filter [ ] Loại */}
                <label className="flex items-center gap-1 cursor-pointer select-none text-slate-700">
                  <input
                    type="checkbox"
                    checked={onlyActiveFilter}
                    onChange={(e) => setOnlyActiveFilter(e.target.checked)}
                    className="rounded text-indigo-600 focus:ring-indigo-500"
                  />
                  <span>Loại (Khả dụng)</span>
                </label>

                {/* Radio ( ) Chọn (Đỏ) */}
                <label className="flex items-center gap-1 cursor-pointer select-none text-red-600 font-semibold">
                  <input
                    type="radio"
                    name="selectionMode"
                    value="Chon"
                    checked={activeSelectionMode === 'Chon'}
                    onChange={() => setActiveSelectionMode('Chon')}
                    className="text-red-600 focus:ring-red-500"
                  />
                  <span>Chọn</span>
                </label>

                {/* Radio (o) Tùy Chọn (Xanh lá) */}
                <label className="flex items-center gap-1 cursor-pointer select-none text-emerald-600 font-semibold">
                  <input
                    type="radio"
                    name="selectionMode"
                    value="TuyChon"
                    checked={activeSelectionMode === 'TuyChon'}
                    onChange={() => setActiveSelectionMode('TuyChon')}
                    className="text-emerald-600 focus:ring-emerald-500"
                  />
                  <span>Tùy Chọn</span>
                </label>

                {/* Radio ( ) Làm Thêm (Xanh dương) */}
                <label className="flex items-center gap-1 cursor-pointer select-none text-blue-600 font-semibold">
                  <input
                    type="radio"
                    name="selectionMode"
                    value="LamThem"
                    checked={activeSelectionMode === 'LamThem'}
                    onChange={() => setActiveSelectionMode('LamThem')}
                    className="text-blue-600 focus:ring-blue-500"
                  />
                  <span>Làm Thêm</span>
                </label>

                {/* Checkbox [ ] Lấy Máu Ngoài */}
                <label className="flex items-center gap-1 cursor-pointer select-none text-indigo-700 font-semibold bg-indigo-50 border border-indigo-200 px-2 py-0.5 rounded">
                  <input
                    type="checkbox"
                    checked={isSpecimenOutsource}
                    onChange={(e) => setIsSpecimenOutsource(e.target.checked)}
                    className="rounded text-indigo-600 focus:ring-indigo-500"
                  />
                  <span>Lấy Máu Ngoài</span>
                </label>
              </div>
            </div>

            {/* Table Area (with 4 UI States) */}
            <div className="flex-1 overflow-auto">
              {loading && (
                <div className="h-full flex flex-col items-center justify-center p-8 text-slate-500">
                  <div className="w-8 h-8 border-4 border-indigo-500 border-t-transparent rounded-full animate-spin mb-3"></div>
                  <span className="text-xs">Đang tải danh mục dịch vụ từ SQL Server...</span>
                </div>
              )}

              {error && (
                <div className="h-full flex flex-col items-center justify-center p-8 text-rose-600">
                  <span className="text-2xl mb-2">⚠️</span>
                  <span className="text-sm font-semibold mb-3">{error}</span>
                  <button
                    onClick={fetchData}
                    className="px-3 py-1 bg-rose-100 hover:bg-rose-200 text-rose-800 rounded text-xs font-semibold cursor-pointer border border-rose-300"
                  >
                    Thử lại
                  </button>
                </div>
              )}

              {!loading && !error && filteredServices.length === 0 && (
                <div className="h-full flex flex-col items-center justify-center p-8 text-slate-400">
                  <span className="text-3xl mb-2">🔍</span>
                  <span className="text-xs font-medium">Không tìm thấy dịch vụ nào phù hợp với bộ lọc.</span>
                  <span className="text-[11px] text-slate-500 mt-1">Thử thay đổi nhóm hoặc từ khóa tìm kiếm.</span>
                </div>
              )}

              {!loading && !error && filteredServices.length > 0 && (
                <table className="w-full text-left border-collapse text-xs">
                  <thead className="sticky top-0 bg-slate-200 text-slate-700 font-semibold border-b border-slate-300 z-10 select-none shadow-xs">
                    <tr>
                      <th className="py-2 px-2.5 w-10 text-center">
                        <input
                          type="checkbox"
                          checked={allFilteredSelected}
                          onChange={(e) => handleSelectAllFiltered(e.target.checked)}
                          className="rounded text-indigo-600 focus:ring-indigo-500 cursor-pointer"
                          title="Chọn / Bỏ chọn toàn bộ trong danh sách"
                        />
                      </th>
                      <th className="py-2 px-2 w-28">Mã Dịch Vụ</th>
                      <th className="py-2 px-3">Tên Dịch Vụ</th>
                      <th className="py-2 px-3 w-28 text-right">Đơn Giá VNĐ</th>
                      <th className="py-2 px-2 w-16 text-center">ID</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {filteredServices.map((srv, idx) => {
                      const selectedEntry = selectedMap.get(srv.id);
                      const isSelected = !!selectedEntry;
                      const mode = selectedEntry?.mode;

                      // Row Text Color based on Selection Mode (VSC Standard)
                      let textColorClass = 'text-slate-800';
                      let badge = null;
                      if (isSelected) {
                        if (mode === 'Chon') {
                          textColorClass = 'text-red-600 font-semibold';
                          badge = <span className="text-[9px] bg-red-100 text-red-700 px-1 rounded ml-1.5">CHỌN</span>;
                        } else if (mode === 'TuyChon') {
                          textColorClass = 'text-emerald-700 font-semibold';
                          badge = <span className="text-[9px] bg-emerald-100 text-emerald-800 px-1 rounded ml-1.5">TÙY CHỌN</span>;
                        } else if (mode === 'LamThem') {
                          textColorClass = 'text-blue-700 font-semibold';
                          badge = <span className="text-[9px] bg-blue-100 text-blue-800 px-1 rounded ml-1.5">LÀM THÊM</span>;
                        }
                      }

                      return (
                        <tr
                          key={srv.id}
                          onClick={() => handleToggleService(srv)}
                          className={`cursor-pointer transition-colors select-none ${
                            idx % 2 === 0 ? 'bg-white' : 'bg-slate-50/50'
                          } ${
                            isSelected ? 'bg-indigo-50/60 hover:bg-indigo-50' : 'hover:bg-slate-100/80'
                          }`}
                        >
                          <td className="py-1.5 px-2.5 text-center">
                            <input
                              type="checkbox"
                              checked={isSelected}
                              onChange={(e) => {
                                e.stopPropagation();
                                handleToggleService(srv);
                              }}
                              className="rounded text-indigo-600 focus:ring-indigo-500 cursor-pointer"
                            />
                          </td>
                          <td className="py-1.5 px-2 font-mono text-[11px] text-slate-600">
                            {srv.serviceCode}
                          </td>
                          <td className={`py-1.5 px-3 ${textColorClass}`}>
                            <div className="flex items-center">
                              <span className="truncate" title={srv.serviceName}>
                                {srv.serviceName}
                              </span>
                              {badge}
                              {selectedEntry?.isSpecimenOutsource && (
                                <span className="text-[9px] bg-purple-100 text-purple-700 px-1 rounded ml-1 border border-purple-200">
                                  MẪU NGOÀI
                                </span>
                              )}
                            </div>
                          </td>
                          <td className="py-1.5 px-3 font-mono text-right text-slate-700 font-medium">
                            {srv.standardPrice > 0 
                              ? srv.standardPrice.toLocaleString('vi-VN') + ' đ'
                              : '—'
                            }
                          </td>
                          <td className="py-1.5 px-2 text-center font-mono text-[11px] text-slate-400">
                            {srv.legacyId ?? '—'}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              )}
            </div>
          </div>
        </div>

        {/* FOOTER ACTION BAR (VSC Standard Buttons & Legend) */}
        <div className="bg-gradient-to-r from-slate-200 via-slate-100 to-slate-200 border-t border-slate-300 px-4 py-2 flex items-center justify-between select-none">
          {/* Left Button */}
          <button
            type="button"
            onClick={handleResetSelected}
            className="px-3 py-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded border border-slate-300 text-xs font-semibold transition-colors cursor-pointer shadow-2xs"
          >
            Reset Dữ Liệu Chọn
          </button>

          {/* Center Action Buttons */}
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={handleConfirm}
              className="px-5 py-1.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded text-xs font-bold transition-colors cursor-pointer shadow-xs flex items-center gap-1.5"
            >
              <span>Lưu & Thoát</span>
              <span className="text-[10px] bg-indigo-700 px-1.5 py-0.2 rounded-full">
                {selectedMap.size}
              </span>
            </button>
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-1.5 bg-white hover:bg-slate-100 text-slate-700 rounded border border-slate-300 text-xs font-semibold transition-colors cursor-pointer"
            >
              Thoát
            </button>
          </div>

          {/* Right Legend with Live Counters */}
          <div className="flex items-center gap-4 text-xs font-bold">
            <span className="text-red-600 flex items-center gap-1">
              <span className="w-2.5 h-2.5 rounded-full bg-red-600"></span>
              Chọn : {countChon}
            </span>
            <span className="text-emerald-600 flex items-center gap-1">
              <span className="w-2.5 h-2.5 rounded-full bg-emerald-600"></span>
              Tùy Chọn : {countTuyChon}
            </span>
            <span className="text-blue-600 flex items-center gap-1">
              <span className="w-2.5 h-2.5 rounded-full bg-blue-600"></span>
              Làm Thêm : {countLamThem}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
};
