import React from 'react';
import { ArrowLeft, Check, Copy, Eye, Trash2 } from 'lucide-react';

export interface SmActionItem {
  id: string;
  label: string;
  icon?: React.ReactNode;
  variant?: 'primary' | 'warning' | 'secondary' | 'outline' | 'danger';
  onClick: () => void;
  disabled?: boolean;
  loading?: boolean;
}

export interface SmStickyActionBarProps {
  title: string;
  subtitle?: string;
  thumbnailUrl?: string;
  thumbnailAlt?: string;
  onBack?: () => void;
  backTooltip?: string;
  actions?: SmActionItem[];
  children?: React.ReactNode;
  className?: string;
}

export const SmStickyActionBar: React.FC<SmStickyActionBarProps> = ({
  title,
  subtitle,
  thumbnailUrl,
  thumbnailAlt = 'Thumbnail',
  onBack,
  backTooltip = 'Back to list',
  actions = [],
  children,
  className = '',
}) => {
  return (
    <div
      className={`sticky top-0 z-30 flex items-center justify-between border-b border-[#dee2e6] bg-white px-6 py-2.5 shadow-xs ${className}`}
      style={{ minHeight: '54px' }}
    >
      {/* Left: Back button, thumbnail, title */}
      <div className="flex items-center gap-3 min-w-0 flex-1">
        {onBack && (
          <button
            type="button"
            onClick={onBack}
            title={backTooltip}
            className="flex h-8 w-8 items-center justify-center rounded border border-[#ced4da] bg-white text-[#495057] transition hover:bg-[#f1f3f6] hover:text-[#212529] cursor-pointer"
          >
            <ArrowLeft className="h-4 w-4" />
          </button>
        )}

        {thumbnailUrl && (
          <div className="flex h-9 w-14 shrink-0 items-center justify-center overflow-hidden rounded border border-[#dee2e6] bg-[#f8f9fa]">
            <img src={thumbnailUrl} alt={thumbnailAlt} className="h-full w-full object-contain" />
          </div>
        )}

        <div className="flex flex-col min-w-0">
          <h1 className="truncate text-base font-bold text-[#212529] tracking-tight">
            {title}
          </h1>
          {subtitle && (
            <span className="truncate text-[11px] text-[#6c757d]">{subtitle}</span>
          )}
        </div>
      </div>

      {/* Right: Actions bar */}
      <div className="flex items-center gap-2 shrink-0">
        {actions.map((act) => {
          let btnClasses =
            'inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded transition cursor-pointer shadow-2xs disabled:opacity-50 disabled:cursor-not-allowed ';

          switch (act.variant) {
            case 'warning':
              // Smartstore signature orange Save button (#ee9b00)
              btnClasses += 'bg-[#ee9b00] hover:bg-[#e77c00] text-white border border-[#e77c00]';
              break;
            case 'primary':
              // Smartstore signature primary blue (#1f63c9)
              btnClasses += 'bg-[#1f63c9] hover:bg-[#18509f] text-white border border-[#1a56af]';
              break;
            case 'danger':
              // Smartstore destructive red (#dc3000)
              btnClasses += 'bg-[#dc3000] hover:bg-[#be2900] text-white border border-[#be2900]';
              break;
            case 'secondary':
              // Smartstore Save and Continue Edit button (soft gray)
              btnClasses += 'bg-[#e9ecef] hover:bg-[#dee2e6] text-[#495057] border border-[#ced4da]';
              break;
            case 'outline':
            default:
              btnClasses += 'bg-white hover:bg-[#f1f3f6] text-[#495057] border border-[#ced4da]';
              break;
          }

          return (
            <button
              key={act.id}
              type="button"
              onClick={act.onClick}
              disabled={act.disabled || act.loading}
              className={btnClasses}
            >
              {act.icon}
              <span>{act.label}</span>
            </button>
          );
        })}

        {children}
      </div>
    </div>
  );
};
