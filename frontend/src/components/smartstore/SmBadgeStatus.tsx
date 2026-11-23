import React from 'react';
import { CheckCircle2, AlertTriangle, AlertCircle, Info } from 'lucide-react';

export type SmSemanticVariant = 'primary' | 'success' | 'warning' | 'danger' | 'info' | 'neutral';

export interface SmBadgeStatusProps {
  label: string;
  variant?: SmSemanticVariant;
  className?: string;
  dot?: boolean;
}

export const SmBadgeStatus: React.FC<SmBadgeStatusProps> = ({
  label,
  variant = 'neutral',
  className = '',
  dot = false,
}) => {
  let badgeStyles = 'inline-flex items-center gap-1 px-2 py-0.5 text-[11px] font-bold rounded-sm ';

  switch (variant) {
    case 'success':
      badgeStyles += 'bg-[#e6f6f1] text-[#009e74] border border-[#009e74]/20';
      break;
    case 'warning':
      badgeStyles += 'bg-[#fef5e6] text-[#ee9b00] border border-[#ee9b00]/20';
      break;
    case 'danger':
      badgeStyles += 'bg-[#fceae6] text-[#dc3000] border border-[#dc3000]/20';
      break;
    case 'info':
      badgeStyles += 'bg-[#eaf4f3] text-[#2c8d8a] border border-[#2c8d8a]/20';
      break;
    case 'primary':
      badgeStyles += 'bg-[#e9f0fa] text-[#1f63c9] border border-[#1f63c9]/20';
      break;
    case 'neutral':
    default:
      badgeStyles += 'bg-[#f1f3f6] text-[#495057] border border-[#dee2e6]';
      break;
  }

  return (
    <span className={`${badgeStyles} ${className}`}>
      {dot && (
        <span
          className={`h-1.5 w-1.5 rounded-full ${
            variant === 'success'
              ? 'bg-[#009e74]'
              : variant === 'warning'
              ? 'bg-[#ee9b00]'
              : variant === 'danger'
              ? 'bg-[#dc3000]'
              : variant === 'info'
              ? 'bg-[#2c8d8a]'
              : variant === 'primary'
              ? 'bg-[#1f63c9]'
              : 'bg-[#6c757d]'
          }`}
        />
      )}
      <span>{label}</span>
    </span>
  );
};

export interface SmCalloutProps {
  title?: string;
  message: React.ReactNode;
  variant?: SmSemanticVariant;
  className?: string;
}

export const SmCallout: React.FC<SmCalloutProps> = ({
  title,
  message,
  variant = 'info',
  className = '',
}) => {
  let borderClass = 'border-l-4 ';
  let bgClass = 'bg-white ';
  let iconNode = <Info className="h-4 w-4 shrink-0 text-[#2c8d8a]" />;

  switch (variant) {
    case 'success':
      borderClass += 'border-[#009e74]';
      bgClass = 'bg-[#f4fbf8]';
      iconNode = <CheckCircle2 className="h-4 w-4 shrink-0 text-[#009e74]" />;
      break;
    case 'warning':
      borderClass += 'border-[#ee9b00]';
      bgClass = 'bg-[#fef9f0]';
      iconNode = <AlertTriangle className="h-4 w-4 shrink-0 text-[#ee9b00]" />;
      break;
    case 'danger':
      borderClass += 'border-[#dc3000]';
      bgClass = 'bg-[#fdf3f0]';
      iconNode = <AlertCircle className="h-4 w-4 shrink-0 text-[#dc3000]" />;
      break;
    case 'primary':
      borderClass += 'border-[#1f63c9]';
      bgClass = 'bg-[#f4f7fc]';
      iconNode = <Info className="h-4 w-4 shrink-0 text-[#1f63c9]" />;
      break;
    case 'info':
    default:
      borderClass += 'border-[#2c8d8a]';
      bgClass = 'bg-[#f3f9f9]';
      iconNode = <Info className="h-4 w-4 shrink-0 text-[#2c8d8a]" />;
      break;
  }

  return (
    <div
      className={`flex items-start gap-3 rounded-r border border-l-0 border-[#dee2e6] p-3 text-xs text-[#212529] shadow-2xs ${borderClass} ${bgClass} ${className}`}
    >
      <div className="pt-0.5">{iconNode}</div>
      <div className="flex-1">
        {title && <div className="font-bold text-[#212529] mb-0.5">{title}</div>}
        <div className="text-[#495057]">{message}</div>
      </div>
    </div>
  );
};
