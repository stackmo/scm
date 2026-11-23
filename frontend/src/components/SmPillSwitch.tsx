import React from 'react';

export interface SmPillSwitchProps {
  checked: boolean;
  onChange: (checked: boolean) => void;
  label?: string;
  disabled?: boolean;
  className?: string;
}

export const SmPillSwitch: React.FC<SmPillSwitchProps> = ({
  checked,
  onChange,
  label,
  disabled = false,
  className = '',
}) => {
  return (
    <div className={`inline-flex items-center gap-2.5 ${className}`}>
      {label && <span className="text-xs font-semibold text-[#495057] select-none">{label}</span>}
      <button
        type="button"
        role="switch"
        aria-checked={checked}
        disabled={disabled}
        onClick={() => !disabled && onChange(!checked)}
        className={`relative inline-flex h-6 w-14 shrink-0 items-center rounded-full transition-colors cursor-pointer select-none border-none focus:outline-none focus:ring-2 focus:ring-[#1f63c9]/30 ${
          disabled ? 'opacity-50 cursor-not-allowed' : ''
        } ${checked ? 'bg-[#1f63c9]' : 'bg-[#6c757d]'}`}
      >
        <span
          className={`inline-block h-4 w-4 transform rounded-full bg-white shadow-sm transition-transform duration-150 ${
            checked ? 'translate-x-8' : 'translate-x-1'
          }`}
        />
        <span
          className={`absolute text-[10px] font-black uppercase text-white select-none tracking-wider ${
            checked ? 'left-2' : 'right-2'
          }`}
        >
          {checked ? 'ON' : 'OFF'}
        </span>
      </button>
    </div>
  );
};
