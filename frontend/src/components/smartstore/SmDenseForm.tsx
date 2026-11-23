import React from 'react';

export interface SmFormFieldProps {
  label: string;
  hint?: string;
  required?: boolean;
  error?: string;
  layout?: 'horizontal' | 'vertical';
  children: React.ReactNode;
  className?: string;
  labelWidth?: string;
}

export const SmFormField: React.FC<SmFormFieldProps> = ({
  label,
  hint,
  required,
  error,
  layout = 'horizontal',
  children,
  className = '',
  labelWidth = '220px',
}) => {
  if (layout === 'vertical') {
    return (
      <div className={`flex flex-col gap-1 ${className}`}>
        <label className="text-xs font-semibold text-[#495057] flex items-center gap-1">
          <span>{label}</span>
          {required && <span className="text-rose-500">*</span>}
          {hint && <span className="text-[11px] font-normal text-[#6c757d]">({hint})</span>}
        </label>
        <div>{children}</div>
        {error && <span className="text-[11px] text-rose-600 font-medium">{error}</span>}
      </div>
    );
  }

  // Smartstore adminRow horizontal layout
  return (
    <div
      className={`flex flex-col md:flex-row md:items-center py-1.5 border-b border-[#f1f3f6] last:border-b-0 ${className}`}
    >
      <div
        className="shrink-0 pr-4 md:text-right text-xs font-semibold text-[#495057] select-none"
        style={{ width: labelWidth }}
      >
        <label className="inline-flex items-center gap-1">
          <span>{label}</span>
          {required && <span className="text-rose-500">*</span>}
        </label>
        {hint && <div className="text-[10px] font-normal text-[#6c757d]">{hint}</div>}
      </div>

      <div className="flex-1 min-w-0 mt-1 md:mt-0">
        <div className="max-w-xl">{children}</div>
        {error && <span className="text-[11px] text-rose-600 font-medium block mt-0.5">{error}</span>}
      </div>
    </div>
  );
};

export interface SmDenseFormProps {
  children: React.ReactNode;
  className?: string;
  title?: string;
}

export const SmDenseForm: React.FC<SmDenseFormProps> = ({
  children,
  className = '',
  title,
}) => {
  return (
    <div className={`rounded-sm border border-[#dee2e6] bg-white p-5 shadow-2xs ${className}`}>
      {title && (
        <h2 className="text-sm font-bold text-[#212529] border-b border-[#dee2e6] pb-2 mb-3">
          {title}
        </h2>
      )}
      <div className="flex flex-col divide-y divide-[#f1f3f6]">{children}</div>
    </div>
  );
};

export interface SmInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  error?: boolean;
}

export const SmInput = React.forwardRef<HTMLInputElement, SmInputProps>(
  ({ className = '', error, ...props }, ref) => {
    return (
      <input
        ref={ref}
        className={`w-full rounded border px-2.5 py-1.5 text-xs text-[#212529] placeholder-[#adb5bd] transition focus:outline-none focus:ring-1 ${
          error
            ? 'border-rose-500 focus:border-rose-500 focus:ring-rose-200'
            : 'border-[#ced4da] bg-white hover:border-[#adb5bd] focus:border-[#1f63c9] focus:ring-[#1f63c9]/20'
        } ${className}`}
        {...props}
      />
    );
  }
);
SmInput.displayName = 'SmInput';

export interface SmSelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  error?: boolean;
}

export const SmSelect = React.forwardRef<HTMLSelectElement, SmSelectProps>(
  ({ className = '', error, children, ...props }, ref) => {
    return (
      <select
        ref={ref}
        className={`w-full rounded border px-2.5 py-1.5 text-xs text-[#212529] transition focus:outline-none focus:ring-1 ${
          error
            ? 'border-rose-500 focus:border-rose-500 focus:ring-rose-200'
            : 'border-[#ced4da] bg-white hover:border-[#adb5bd] focus:border-[#1f63c9] focus:ring-[#1f63c9]/20'
        } ${className}`}
        {...props}
      >
        {children}
      </select>
    );
  }
);
SmSelect.displayName = 'SmSelect';

export interface SmTextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  error?: boolean;
}

export const SmTextarea = React.forwardRef<HTMLTextAreaElement, SmTextareaProps>(
  ({ className = '', error, ...props }, ref) => {
    return (
      <textarea
        ref={ref}
        className={`w-full rounded border px-2.5 py-1.5 text-xs text-[#212529] placeholder-[#adb5bd] transition focus:outline-none focus:ring-1 ${
          error
            ? 'border-rose-500 focus:border-rose-500 focus:ring-rose-200'
            : 'border-[#ced4da] bg-white hover:border-[#adb5bd] focus:border-[#1f63c9] focus:ring-[#1f63c9]/20'
        } ${className}`}
        {...props}
      />
    );
  }
);
SmTextarea.displayName = 'SmTextarea';
