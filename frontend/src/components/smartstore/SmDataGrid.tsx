import React, { useState, useMemo } from 'react';
import {
  RotateCw,
  ChevronsLeft,
  ChevronLeft,
  ChevronRight,
  ChevronsRight,
  Settings,
  Trash2,
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
  Filter,
  Plus,
  Check,
  X,
  Search,
  Download,
  Edit2,
  SlidersHorizontal,
} from 'lucide-react';

export type SmSortDirection = 'asc' | 'desc' | null;

export interface SmDataGridColumn<T> {
  id: string;
  header: string;
  accessor?: (row: T) => React.ReactNode;
  width?: string;
  align?: 'left' | 'center' | 'right';
  sortable?: boolean;
  filterable?: boolean;
  filterPlaceholder?: string;
}

export interface SmRowCommand<T> {
  id: string;
  label: string;
  icon?: React.ReactNode;
  variant?: 'primary' | 'danger' | 'secondary' | 'outline';
  onClick: (row: T) => void;
}

export interface SmDataGridProps<T> {
  columns: SmDataGridColumn<T>[];
  data: T[];
  totalCount?: number;
  pageIndex?: number;
  pageSize?: number;
  pageSizeOptions?: number[];
  onPageChange?: (page: number) => void;
  onPageSizeChange?: (size: number) => void;
  onReload?: () => void;
  selectedIds?: (string | number)[];
  onSelectionChange?: (selectedIds: (string | number)[]) => void;
  getRowId?: (row: T) => string | number;
  batchDeleteLabel?: string;
  onBatchDelete?: (selectedIds: (string | number)[]) => void;
  emptyText?: string;
  className?: string;
  title?: string;
  // Toolbar additions
  onAddNew?: () => void;
  addNewLabel?: string;
  onSaveChanges?: () => void;
  hasUnsavedChanges?: boolean;
  onCancelChanges?: () => void;
  onExport?: () => void;
  searchPlaceholder?: string;
  onQuickSearch?: (term: string) => void;
  searchPanelContent?: React.ReactNode;
  rowCommands?: (row: T) => SmRowCommand<T>[];
  onRowClick?: (row: T) => void;
  activeRowId?: string | number | null;
  extraHeaderActions?: React.ReactNode;
  striped?: boolean;
  hoverable?: boolean;
}

export function SmDataGrid<T>({
  columns,
  data,
  totalCount = data.length,
  pageIndex = 1,
  pageSize = 25,
  pageSizeOptions = [10, 25, 50, 100],
  onPageChange,
  onPageSizeChange,
  onReload,
  selectedIds = [],
  onSelectionChange,
  getRowId,
  batchDeleteLabel = 'Delete selected',
  onBatchDelete,
  emptyText = 'No data',
  className = '',
  title,
  onAddNew,
  addNewLabel = 'Add New...',
  onSaveChanges,
  hasUnsavedChanges = false,
  onCancelChanges,
  onExport,
  searchPlaceholder = 'Search in grid...',
  onQuickSearch,
  searchPanelContent,
  rowCommands,
  onRowClick,
  activeRowId,
  extraHeaderActions,
  striped = true,
  hoverable = true,
}: SmDataGridProps<T>) {
  const [selected, setSelected] = useState<(string | number)[]>(selectedIds);
  const [isSearchPanelOpen, setIsSearchPanelOpen] = useState<boolean>(false);
  const [quickSearchTerm, setQuickSearchTerm] = useState<string>('');
  const [sortColumn, setSortColumn] = useState<string | null>(null);
  const [sortDirection, setSortDirection] = useState<SmSortDirection>(null);
  const [columnFilters, setColumnFilters] = useState<Record<string, string>>({});

  const handleSelectAll = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!getRowId) return;
    if (e.target.checked) {
      const allIds = data.map((d) => getRowId(d));
      setSelected(allIds);
      onSelectionChange?.(allIds);
    } else {
      setSelected([]);
      onSelectionChange?.([]);
    }
  };

  const handleSelectRow = (e: React.MouseEvent, id: string | number) => {
    e.stopPropagation();
    let next: (string | number)[];
    if (selected.includes(id)) {
      next = selected.filter((s) => s !== id);
    } else {
      next = [...selected, id];
    }
    setSelected(next);
    onSelectionChange?.(next);
  };

  const handleSort = (columnId: string) => {
    if (sortColumn !== columnId) {
      setSortColumn(columnId);
      setSortDirection('asc');
    } else if (sortDirection === 'asc') {
      setSortDirection('desc');
    } else {
      setSortColumn(null);
      setSortDirection(null);
    }
  };

  const handleColumnFilterChange = (colId: string, value: string) => {
    setColumnFilters((prev) => ({
      ...prev,
      [colId]: value,
    }));
  };

  // Local filtered data if column filters are present
  const displayData = useMemo(() => {
    let result = [...data];

    // Quick search
    if (quickSearchTerm.trim()) {
      const term = quickSearchTerm.toLowerCase();
      result = result.filter((item) =>
        Object.values(item as any).some((val) =>
          String(val).toLowerCase().includes(term)
        )
      );
    }

    // Column inline filters
    const activeFilterEntries = Object.entries(columnFilters).filter(
      ([, v]) => v.trim() !== ''
    );
    if (activeFilterEntries.length > 0) {
      result = result.filter((item) => {
        return activeFilterEntries.every(([colId, filterVal]) => {
          const val = (item as any)[colId];
          return (
            val !== undefined &&
            String(val).toLowerCase().includes(filterVal.toLowerCase())
          );
        });
      });
    }

    // Sorting
    if (sortColumn && sortDirection) {
      result.sort((a, b) => {
        const valA = (a as any)[sortColumn];
        const valB = (b as any)[sortColumn];
        if (valA === valB) return 0;
        if (valA == null) return 1;
        if (valB == null) return -1;
        if (sortDirection === 'asc') {
          return valA > valB ? 1 : -1;
        } else {
          return valA < valB ? 1 : -1;
        }
      });
    }

    return result;
  }, [data, quickSearchTerm, columnFilters, sortColumn, sortDirection]);

  const totalPages = Math.ceil(totalCount / pageSize) || 1;
  const startItem = totalCount === 0 ? 0 : (pageIndex - 1) * pageSize + 1;
  const endItem = Math.min(pageIndex * pageSize, totalCount);

  return (
    <div
      className={`datagrid-root flex flex-col border border-[#dee2e6] bg-white rounded-sm shadow-2xs ${className}`}
    >
      {/* 1. Smartstore Grid Toolbar (.dg-toolbar) */}
      <div className="flex flex-wrap items-center justify-between border-b border-[#dee2e6] bg-[#f8f9fa] px-3 py-2 text-xs gap-2">
        {/* Left Toolbar Groups */}
        <div className="flex items-center gap-1.5 flex-wrap">
          {searchPanelContent && (
            <button
              type="button"
              onClick={() => setIsSearchPanelOpen(!isSearchPanelOpen)}
              title="Toggle Search Panel"
              className={`flex h-7 items-center gap-1.5 px-2.5 rounded border text-xs font-medium transition cursor-pointer ${
                isSearchPanelOpen
                  ? 'bg-[#1f63c9] text-white border-[#1a56af]'
                  : 'bg-white text-[#495057] border-[#ced4da] hover:bg-[#e9ecef]'
              }`}
            >
              <Filter className="h-3.5 w-3.5" />
              <span className="hidden sm:inline">Lọc nâng cao</span>
            </button>
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

          {hasUnsavedChanges && onSaveChanges && (
            <button
              type="button"
              onClick={onSaveChanges}
              className="inline-flex h-7 items-center gap-1.5 rounded bg-[#009e74] px-3 text-xs font-semibold text-white transition hover:bg-[#008763] cursor-pointer shadow-2xs"
            >
              <Check className="h-3.5 w-3.5" />
              <span>Save Changes</span>
            </button>
          )}

          {hasUnsavedChanges && onCancelChanges && (
            <button
              type="button"
              onClick={onCancelChanges}
              className="inline-flex h-7 items-center gap-1 rounded bg-white px-2.5 text-xs font-medium text-[#6c757d] border border-[#ced4da] transition hover:bg-[#e9ecef] cursor-pointer"
            >
              <X className="h-3.5 w-3.5" />
              <span>Cancel</span>
            </button>
          )}

          {onBatchDelete && selected.length > 0 && (
            <button
              type="button"
              onClick={() => onBatchDelete(selected)}
              className="inline-flex h-7 items-center gap-1.5 rounded border border-[#dc3000] bg-[#dc3000] px-3 text-xs font-semibold text-white transition hover:bg-[#be2900] cursor-pointer shadow-2xs"
            >
              <Trash2 className="h-3.5 w-3.5" />
              <span>{batchDeleteLabel} ({selected.length})</span>
            </button>
          )}

          {title && (
            <span className="ml-2 font-bold text-[#212529] hidden md:inline">
              {title}
            </span>
          )}
        </div>

        {/* Right Toolbar Groups: Quick Search & Export */}
        <div className="flex items-center gap-2">
          {/* Quick Search */}
          <div className="relative">
            <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-[#adb5bd]" />
            <input
              type="text"
              placeholder={searchPlaceholder}
              value={quickSearchTerm}
              onChange={(e) => {
                setQuickSearchTerm(e.target.value);
                onQuickSearch?.(e.target.value);
              }}
              className="h-7 w-44 sm:w-56 rounded border border-[#ced4da] bg-white pl-8 pr-2.5 text-xs text-[#212529] placeholder-[#adb5bd] transition focus:border-[#1f63c9] focus:outline-none focus:ring-1 focus:ring-[#1f63c9]/20"
            />
            {quickSearchTerm && (
              <button
                type="button"
                onClick={() => setQuickSearchTerm('')}
                className="absolute right-2 top-1/2 -translate-y-1/2 text-[#adb5bd] hover:text-[#495057]"
              >
                <X className="h-3 w-3" />
              </button>
            )}
          </div>

          {onExport && (
            <button
              type="button"
              onClick={onExport}
              title="Xuất Excel / CSV"
              className="flex h-7 items-center gap-1 rounded border border-[#ced4da] bg-white px-2.5 text-xs font-medium text-[#495057] transition hover:bg-[#e9ecef] cursor-pointer"
            >
              <Download className="h-3.5 w-3.5" />
              <span className="hidden sm:inline">Export</span>
            </button>
          )}

          {extraHeaderActions}
        </div>
      </div>

      {/* 2. Collapsible Search Panel (.dg-search) */}
      {isSearchPanelOpen && searchPanelContent && (
        <div className="border-b border-[#dee2e6] bg-[#f1f3f6] p-4 text-xs">
          <div className="flex items-center justify-between pb-2 mb-3 border-b border-[#dee2e6]">
            <div className="font-bold text-[#495057] flex items-center gap-1.5">
              <SlidersHorizontal className="h-4 w-4 text-[#1f63c9]" />
              <span>Bộ lọc tìm kiếm nâng cao (Search Panel)</span>
            </div>
            <button
              type="button"
              onClick={() => setIsSearchPanelOpen(false)}
              className="text-[#6c757d] hover:text-[#212529]"
            >
              <X className="h-4 w-4" />
            </button>
          </div>
          <div>{searchPanelContent}</div>
        </div>
      )}

      {/* 3. Main DataGrid Table */}
      <div className="dg-table-wrapper overflow-x-auto flex-1 min-h-0 bg-white">
        <table className="w-full border-collapse text-left text-xs text-[#212529]">
          <thead>
            {/* Header Row */}
            <tr className="border-b border-[#dee2e6] bg-[#f8f9fa] font-bold text-[#495057] select-none">
              {getRowId && (
                <th className="w-10 px-3 py-2 text-center border-r border-[#dee2e6]">
                  <input
                    type="checkbox"
                    checked={data.length > 0 && selected.length === data.length}
                    onChange={handleSelectAll}
                    className="rounded border-[#ced4da] text-[#1f63c9] focus:ring-0 cursor-pointer"
                  />
                </th>
              )}

              {columns.map((col) => {
                const isSorted = sortColumn === col.id;
                return (
                  <th
                    key={col.id}
                    onClick={() => col.sortable !== false && handleSort(col.id)}
                    className={`px-3 py-2 border-r border-[#dee2e6] last:border-r-0 transition-colors ${
                      col.sortable !== false ? 'cursor-pointer hover:bg-[#e9ecef]' : ''
                    } ${col.align === 'center' ? 'text-center' : col.align === 'right' ? 'text-right' : 'text-left'}`}
                    style={{ width: col.width }}
                  >
                    <div className={`inline-flex items-center gap-1 ${col.align === 'center' ? 'justify-center' : col.align === 'right' ? 'justify-end' : 'justify-start'}`}>
                      <span>{col.header}</span>
                      {col.sortable !== false && (
                        <span className="text-[#adb5bd]">
                          {isSorted && sortDirection === 'asc' ? (
                            <ArrowUp className="h-3 w-3 text-[#1f63c9]" />
                          ) : isSorted && sortDirection === 'desc' ? (
                            <ArrowDown className="h-3 w-3 text-[#1f63c9]" />
                          ) : (
                            <ArrowUpDown className="h-3 w-3 opacity-40" />
                          )}
                        </span>
                      )}
                    </div>
                  </th>
                );
              })}

              {rowCommands && (
                <th className="w-24 px-3 py-2 text-center border-r border-[#dee2e6]">
                  Thao tác
                </th>
              )}
            </tr>

            {/* Smartstore Column Inline Filters Row */}
            <tr className="border-b border-[#dee2e6] bg-[#f8f9fa]">
              {getRowId && <th className="border-r border-[#dee2e6] px-1 py-1" />}
              {columns.map((col) => (
                <th key={`filter-${col.id}`} className="px-2 py-1 border-r border-[#dee2e6] last:border-r-0 font-normal">
                  {col.filterable !== false ? (
                    <input
                      type="text"
                      placeholder={col.filterPlaceholder || 'Lọc...'}
                      value={columnFilters[col.id] || ''}
                      onChange={(e) => handleColumnFilterChange(col.id, e.target.value)}
                      className="w-full rounded border border-[#ced4da] bg-white px-2 py-0.5 text-[11px] text-[#495057] placeholder-[#adb5bd] outline-none transition focus:border-[#1f63c9] focus:ring-1 focus:ring-[#1f63c9]/20"
                    />
                  ) : null}
                </th>
              ))}
              {rowCommands && <th className="border-r border-[#dee2e6] px-1 py-1" />}
            </tr>
          </thead>

          <tbody className="divide-y divide-[#dee2e6]">
            {displayData.length === 0 ? (
              <tr className="dg-no-data">
                <td
                  colSpan={columns.length + (getRowId ? 1 : 0) + (rowCommands ? 1 : 0)}
                  className="px-4 py-12 text-center text-xs text-[#6c757d]"
                >
                  <div className="flex flex-col items-center justify-center gap-1.5">
                    <span className="font-semibold">{emptyText}</span>
                    {(quickSearchTerm || Object.keys(columnFilters).length > 0) && (
                      <span className="text-[11px] text-[#adb5bd]">
                        Không tìm thấy bản ghi khớp với bộ lọc hiện tại.
                      </span>
                    )}
                  </div>
                </td>
              </tr>
            ) : (
              displayData.map((row, idx) => {
                const id = getRowId ? getRowId(row) : idx;
                const isChecked = selected.includes(id);
                const isActive = activeRowId !== undefined && activeRowId === id;
                const isEven = idx % 2 === 1;

                let rowBg = 'bg-white';
                if (isActive) {
                  rowBg = 'bg-[#e7f1ff] font-medium';
                } else if (isChecked) {
                  rowBg = 'bg-[#f1f3f6]';
                } else if (striped && isEven) {
                  rowBg = 'bg-[#fcfdfe]';
                }

                return (
                  <tr
                    key={String(id)}
                    onClick={() => onRowClick?.(row)}
                    className={`transition-colors border-b border-[#dee2e6] ${rowBg} ${
                      hoverable ? 'hover:bg-[#f1f7fc]' : ''
                    } ${onRowClick ? 'cursor-pointer' : ''}`}
                  >
                    {getRowId && (
                      <td
                        className="px-3 py-2 text-center border-r border-[#dee2e6]"
                        onClick={(e) => e.stopPropagation()}
                      >
                        <input
                          type="checkbox"
                          checked={isChecked}
                          onChange={(e) => handleSelectRow(e as any, id)}
                          className="rounded border-[#ced4da] text-[#1f63c9] focus:ring-0 cursor-pointer"
                        />
                      </td>
                    )}

                    {columns.map((col) => (
                      <td
                        key={col.id}
                        className={`px-3 py-2 border-r border-[#dee2e6] last:border-r-0 ${
                          col.align === 'center'
                            ? 'text-center'
                            : col.align === 'right'
                            ? 'text-right'
                            : 'text-left'
                        }`}
                      >
                        {col.accessor ? col.accessor(row) : (row as any)[col.id]}
                      </td>
                    ))}

                    {rowCommands && (
                      <td
                        className="px-2 py-1.5 text-center border-r border-[#dee2e6]"
                        onClick={(e) => e.stopPropagation()}
                      >
                        <div className="flex items-center justify-center gap-1">
                          {rowCommands(row).map((cmd) => (
                            <button
                              key={cmd.id}
                              type="button"
                              onClick={() => cmd.onClick(row)}
                              className={`inline-flex items-center gap-1 rounded px-2 py-0.5 text-[11px] font-semibold border transition cursor-pointer ${
                                cmd.variant === 'danger'
                                  ? 'border-rose-300 bg-white text-rose-600 hover:bg-rose-50'
                                  : cmd.variant === 'primary'
                                  ? 'border-[#1f63c9] bg-[#1f63c9] text-white hover:bg-[#18509f]'
                                  : 'border-[#ced4da] bg-white text-[#495057] hover:bg-[#f1f3f6] hover:text-[#1f63c9]'
                              }`}
                            >
                              {cmd.icon}
                              <span>{cmd.label}</span>
                            </button>
                          ))}
                        </div>
                      </td>
                    )}
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>

      {/* 4. Smartstore Pager Footer (.dg-pager) */}
      <div className="flex flex-wrap items-center justify-between border-t border-[#dee2e6] bg-[#f8f9fa] px-3 py-1.5 text-xs text-[#6c757d] gap-2">
        {/* Left: Refresh + Page navigation */}
        <div className="flex items-center gap-1">
          {onReload && (
            <button
              type="button"
              onClick={onReload}
              title="Refresh grid"
              className="flex h-6 w-6 items-center justify-center rounded border border-[#ced4da] bg-white text-[#495057] transition hover:bg-[#e9ecef] cursor-pointer mr-2 shadow-2xs"
            >
              <RotateCw className="h-3 w-3" />
            </button>
          )}

          <button
            type="button"
            disabled={pageIndex <= 1}
            onClick={() => onPageChange?.(1)}
            title="First page"
            className="flex h-6 w-6 items-center justify-center rounded border border-[#ced4da] bg-white text-[#495057] transition hover:bg-[#e9ecef] disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
          >
            <ChevronsLeft className="h-3 w-3" />
          </button>
          <button
            type="button"
            disabled={pageIndex <= 1}
            onClick={() => onPageChange?.(pageIndex - 1)}
            title="Previous page"
            className="flex h-6 w-6 items-center justify-center rounded border border-[#ced4da] bg-white text-[#495057] transition hover:bg-[#e9ecef] disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
          >
            <ChevronLeft className="h-3 w-3" />
          </button>

          <span className="px-2 text-[11px] font-semibold text-[#495057]">
            Trang {pageIndex} / {totalPages}
          </span>

          <button
            type="button"
            disabled={pageIndex >= totalPages}
            onClick={() => onPageChange?.(pageIndex + 1)}
            title="Next page"
            className="flex h-6 w-6 items-center justify-center rounded border border-[#ced4da] bg-white text-[#495057] transition hover:bg-[#e9ecef] disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
          >
            <ChevronRight className="h-3 w-3" />
          </button>
          <button
            type="button"
            disabled={pageIndex >= totalPages}
            onClick={() => onPageChange?.(totalPages)}
            title="Last page"
            className="flex h-6 w-6 items-center justify-center rounded border border-[#ced4da] bg-white text-[#495057] transition hover:bg-[#e9ecef] disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
          >
            <ChevronsRight className="h-3 w-3" />
          </button>
        </div>

        {/* Right: Page Size Selector + Item Counter + Settings */}
        <div className="flex items-center gap-3">
          {onPageSizeChange && (
            <div className="flex items-center gap-1.5 text-[11px]">
              <span className="hidden sm:inline">Hiển thị:</span>
              <select
                value={pageSize}
                onChange={(e) => onPageSizeChange(parseInt(e.target.value, 10))}
                className="h-6 rounded border border-[#ced4da] bg-white px-1.5 text-[11px] font-medium text-[#495057] outline-none cursor-pointer"
              >
                {pageSizeOptions.map((sz) => (
                  <option key={sz} value={sz}>
                    {sz} / trang
                  </option>
                ))}
              </select>
            </div>
          )}

          <span className="text-[11px] font-medium text-[#6c757d]">
            Displaying items {startItem}-{endItem} of {totalCount}
          </span>

          <button
            type="button"
            title="Grid Settings"
            className="flex h-6 w-6 items-center justify-center rounded border border-[#ced4da] bg-white text-[#6c757d] transition hover:bg-[#e9ecef] hover:text-[#212529] cursor-pointer"
          >
            <Settings className="h-3 w-3" />
          </button>
        </div>
      </div>
    </div>
  );
}
