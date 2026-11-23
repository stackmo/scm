import React from 'react';

export interface SmTabItem {
  id: string;
  label: string;
  icon?: React.ComponentType<{ className?: string }>;
  badge?: string | number;
  badgeVariant?: 'primary' | 'warning' | 'danger' | 'success' | 'secondary';
  disabled?: boolean;
}

export interface SmVerticalTabRailProps {
  tabs: SmTabItem[];
  activeTabId: string;
  onTabChange: (id: string) => void;
  className?: string;
  width?: string;
}

export const SmVerticalTabRail: React.FC<SmVerticalTabRailProps> = ({
  tabs,
  activeTabId,
  onTabChange,
  className = '',
  width = '240px',
}) => {
  return (
    <nav
      className={`flex flex-col shrink-0 select-none border-r border-[#dee2e6] bg-white py-2 overflow-y-auto ${className}`}
      style={{ width }}
      aria-label="Entity Tabs"
    >
      <div className="flex flex-col gap-0.5 px-1.5">
        {tabs.map((tab) => {
          const isActive = tab.id === activeTabId;
          const Icon = tab.icon;

          return (
            <button
              key={tab.id}
              type="button"
              disabled={tab.disabled}
              onClick={() => onTabChange(tab.id)}
              className={`group relative flex items-center justify-between px-3 py-2 text-xs font-medium rounded-sm transition-all cursor-pointer ${
                isActive
                  ? 'bg-[#f1f3f6] text-[#212529] font-bold shadow-2xs'
                  : 'text-[#495057] hover:bg-[#f8f9fa] hover:text-[#212529]'
              } ${tab.disabled ? 'opacity-40 cursor-not-allowed' : ''}`}
            >
              {/* Active Marker: Smartstore signature left orange line (#ee9b00) */}
              {isActive && (
                <span
                  className="absolute left-0 top-1 bottom-1 w-[3px] rounded-r bg-[#ee9b00]"
                  aria-hidden="true"
                />
              )}

              <div className="flex items-center gap-2.5 truncate pl-1">
                {Icon && (
                  <Icon
                    className={`h-4 w-4 shrink-0 transition-colors ${
                      isActive ? 'text-[#1f63c9]' : 'text-[#6c757d] group-hover:text-[#495057]'
                    }`}
                  />
                )}
                <span className="truncate">{tab.label}</span>
              </div>

              {tab.badge !== undefined && (
                <span
                  className={`ml-2 rounded-full px-1.5 py-0.2 text-[10px] font-bold ${
                    tab.badgeVariant === 'warning'
                      ? 'bg-amber-100 text-amber-800'
                      : tab.badgeVariant === 'danger'
                      ? 'bg-rose-100 text-rose-800'
                      : tab.badgeVariant === 'success'
                      ? 'bg-emerald-100 text-emerald-800'
                      : 'bg-slate-200 text-slate-700'
                  }`}
                >
                  {tab.badge}
                </span>
              )}
            </button>
          );
        })}
      </div>
    </nav>
  );
};
