import React, { useState } from 'react';
import {
  Search,
  Plus,
  RefreshCw,
  Download,
  X,
  Maximize2,
  Minimize2,
  ChevronRight,
  Sliders,
  Check,
} from 'lucide-react';
import type { SmTabItem } from './SmVerticalTabRail';

export interface SmMasterDetailLayoutProps {
  title: string;
  subtitle?: string;
  titleIcon?: React.ReactNode;
  // Sub-tabs (horizontal)
  tabs?: SmTabItem[];
  activeTabId?: string;
  onTabChange?: (tabId: string) => void;
  // Header Actions
  headerActions?: React.ReactNode;
  onAddNew?: () => void;
  addNewLabel?: string;
  onExport?: () => void;
  onReload?: () => void;
  // Quick Search
  searchPlaceholder?: string;
  searchValue?: string;
  onSearchChange?: (val: string) => void;
  // Main Content (Master Pane, usually Smartstore DataGrid)
  children: React.ReactNode;
  // Detail Drawer / Pane
  detailOpen?: boolean;
  onCloseDetail?: () => void;
  detailTitle?: string;
  detailSubtitle?: string;
  detailContent?: React.ReactNode;
  detailWidth?: string;
  onSaveDetail?: () => void;
  isSavingDetail?: boolean;
  saveDetailLabel?: string;
  detailHeaderActions?: React.ReactNode;
  // Footer
  footerSummary?: React.ReactNode;
  className?: string;
}

export const SmMasterDetailLayout: React.FC<SmMasterDetailLayoutProps> = ({
  title,
  subtitle,
  titleIcon,
  tabs,
  activeTabId,
  onTabChange,
  headerActions,
  onAddNew,
  addNewLabel = 'Thêm mới',
  onExport,
  onReload,
  searchPlaceholder = 'Tìm kiếm trong danh sách...',
  searchValue,
  onSearchChange,
  children,
  detailOpen = false,
  onCloseDetail,
  detailTitle = 'Chi tiết bản ghi',
  detailSubtitle,
  detailContent,
  detailWidth = '480px',
  onSaveDetail,
  isSavingDetail = false,
  saveDetailLabel = 'Save',
  detailHeaderActions,
  footerSummary,
  className = '',
}) => {
  const [isDetailExpanded, setIsDetailExpanded] = useState<boolean>(false);

  return (
    <div
      className={`flex flex-col h-[calc(100vh-72px)] w-full overflow-hidden bg-[#f1f3f6] text-xs text-[#212529] ${className}`}
    >
      {/* 1. Smartstore Section Header (.section-header) */}
      <div className="flex flex-col border-b border-[#dee2e6] bg-white px-5 py-2.5 shadow-2xs shrink-0 gap-2">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          {/* Title + Icon + Subtitle */}
          <div className="flex items-center gap-3 min-w-0">
            {titleIcon && (
              <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded bg-[#e9f0fa] text-[#1f63c9] border border-[#1f63c9]/20">
                {titleIcon}
              </div>
            )}
            <div className="truncate">
              <h1 className="truncate text-base font-bold text-[#212529] tracking-tight">
                {title}
              </h1>
              {subtitle && (
                <p className="truncate text-[11px] text-[#6c757d]">{subtitle}</p>
              )}
            </div>
          </div>

          {/* Right Toolbar / Quick Search */}
          <div className="flex items-center gap-2 flex-wrap">
            {onSearchChange !== undefined && (
              <div className="relative w-52 sm:w-64">
                <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-[#adb5bd]" />
                <input
                  type="text"
                  placeholder={searchPlaceholder}
                  value={searchValue || ''}
                  onChange={(e) => onSearchChange(e.target.value)}
                  className="h-7 w-full rounded border border-[#ced4da] bg-white pl-8 pr-2.5 text-xs text-[#212529] placeholder-[#adb5bd] transition focus:border-[#1f63c9] focus:outline-none focus:ring-1 focus:ring-[#1f63c9]/20"
                />
              </div>
            )}

            {onAddNew && (
              <button
                type="button"
                onClick={onAddNew}
                className="inline-flex h-7 items-center gap-1.5 rounded bg-[#1f63c9] px-3 text-xs font-semibold text-white transition hover:bg-[#18509f] cursor-pointer shadow-2xs"
              >
                <Plus className="h-3.5 w-3.5" />
                <span>{addNewLabel}</span>
              </button>
            )}

            {onExport && (
              <button
                type="button"
                onClick={onExport}
                title="Xuất file"
                className="flex h-7 items-center gap-1 rounded border border-[#ced4da] bg-white px-2.5 text-xs font-medium text-[#495057] transition hover:bg-[#e9ecef] cursor-pointer"
              >
                <Download className="h-3.5 w-3.5" />
                <span className="hidden sm:inline">Export</span>
              </button>
            )}

            {onReload && (
              <button
                type="button"
                onClick={onReload}
                title="Tải lại dữ liệu"
                className="flex h-7 w-7 items-center justify-center rounded border border-[#ced4da] bg-white text-[#495057] transition hover:bg-[#e9ecef] cursor-pointer"
              >
                <RefreshCw className="h-3.5 w-3.5" />
              </button>
            )}

            {headerActions}
          </div>
        </div>

        {/* 2. Smartstore Horizontal Sub-Tabs (.sm-nav-tabs) */}
        {tabs && tabs.length > 0 && (
          <div className="flex items-center gap-1 border-t border-[#f1f3f6] pt-1.5 -mb-2 overflow-x-auto scrollbar-none">
            {tabs.map((tab) => {
              const isActive = activeTabId === tab.id;
              const Icon = tab.icon;

              return (
                <button
                  key={tab.id}
                  type="button"
                  disabled={tab.disabled}
                  onClick={() => onTabChange?.(tab.id)}
                  className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-t border-t border-x transition-all cursor-pointer whitespace-nowrap ${
                    isActive
                      ? 'bg-white text-[#1f63c9] border-[#dee2e6] border-b-white -mb-px font-bold shadow-2xs'
                      : 'bg-[#f8f9fa] text-[#6c757d] border-transparent hover:text-[#212529] hover:bg-[#e9ecef]'
                  } ${tab.disabled ? 'opacity-40 cursor-not-allowed' : ''}`}
                >
                  {Icon && <Icon className={`h-3.5 w-3.5 ${isActive ? 'text-[#1f63c9]' : 'text-[#6c757d]'}`} />}
                  <span>{tab.label}</span>
                  {tab.badge !== undefined && (
                    <span
                      className={`ml-1 rounded-full px-1.5 py-0.2 text-[10px] font-bold ${
                        isActive
                          ? 'bg-[#1f63c9] text-white'
                          : 'bg-[#ced4da] text-[#495057]'
                      }`}
                    >
                      {tab.badge}
                    </span>
                  )}
                </button>
              );
            })}
          </div>
        )}
      </div>

      {/* 3. Core Workspace: Split Master (Left) + Smartstore Detail Drawer (Right) */}
      <div className="flex flex-1 min-h-0 overflow-hidden relative">
        {/* Master Pane */}
        <main
          className={`flex-1 overflow-y-auto p-4 transition-all duration-200 ${
            detailOpen && isDetailExpanded ? 'hidden' : 'block'
          }`}
        >
          {children}
        </main>

        {/* Smartstore Detail Drawer / Pane */}
        {detailOpen && (
          <aside
            className={`flex flex-col border-l border-[#dee2e6] bg-white transition-all duration-200 shadow-lg shrink-0 ${
              isDetailExpanded ? 'w-full' : ''
            }`}
            style={{ width: isDetailExpanded ? '100%' : detailWidth }}
          >
            {/* Detail Sticky Header */}
            <div className="flex items-center justify-between border-b border-[#dee2e6] bg-[#f8f9fa] px-4 py-2.5 shrink-0">
              <div className="min-w-0 pr-2">
                <h3 className="truncate text-xs font-bold text-[#212529]">
                  {detailTitle}
                </h3>
                {detailSubtitle && (
                  <p className="truncate text-[10px] text-[#6c757d]">{detailSubtitle}</p>
                )}
              </div>

              <div className="flex items-center gap-1.5 shrink-0">
                {/* Save Button (Smartstore Signature Orange #ee9b00) */}
                {onSaveDetail && (
                  <button
                    type="button"
                    onClick={onSaveDetail}
                    disabled={isSavingDetail}
                    className="inline-flex h-7 items-center gap-1 rounded bg-[#ee9b00] px-3 text-xs font-semibold text-white transition hover:bg-[#e77c00] cursor-pointer shadow-2xs disabled:opacity-50"
                  >
                    <Check className="h-3.5 w-3.5" />
                    <span>{isSavingDetail ? 'Saving...' : saveDetailLabel}</span>
                  </button>
                )}

                {detailHeaderActions}

                {/* Expand / Minimize */}
                <button
                  type="button"
                  onClick={() => setIsDetailExpanded(!isDetailExpanded)}
                  title={isDetailExpanded ? 'Thu nhỏ' : 'Toàn màn hình'}
                  className="flex h-7 w-7 items-center justify-center rounded border border-[#ced4da] bg-white text-[#6c757d] transition hover:bg-[#e9ecef] cursor-pointer"
                >
                  {isDetailExpanded ? (
                    <Minimize2 className="h-3.5 w-3.5" />
                  ) : (
                    <Maximize2 className="h-3.5 w-3.5" />
                  )}
                </button>

                {/* Close Button */}
                {onCloseDetail && (
                  <button
                    type="button"
                    onClick={onCloseDetail}
                    title="Đóng chi tiết"
                    className="flex h-7 w-7 items-center justify-center rounded border border-[#ced4da] bg-white text-[#dc3000] transition hover:bg-rose-50 cursor-pointer"
                  >
                    <X className="h-3.5 w-3.5" />
                  </button>
                )}
              </div>
            </div>

            {/* Detail Scrollable Body */}
            <div className="flex-1 overflow-y-auto p-4 scrollbar-thin">
              {detailContent}
            </div>
          </aside>
        )}
      </div>

      {/* 4. Footer Summary */}
      {footerSummary && (
        <footer className="flex items-center justify-between border-t border-[#dee2e6] bg-white px-4 py-1.5 text-xs text-[#6c757d] shrink-0 shadow-2xs">
          {footerSummary}
        </footer>
      )}
    </div>
  );
};
