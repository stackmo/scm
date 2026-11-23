import React, { useState, useRef, useEffect } from 'react';

export interface SmDropdownItem {
  id: string;
  label: string;
  icon?: React.ReactNode;
  onClick: () => void;
  divider?: boolean;
  danger?: boolean;
  disabled?: boolean;
}

export interface SmDropdownBubbleProps {
  trigger: React.ReactNode;
  items: SmDropdownItem[];
  header?: string;
  align?: 'left' | 'right';
  className?: string;
}

export const SmDropdownBubble: React.FC<SmDropdownBubbleProps> = ({
  trigger,
  items,
  header,
  align = 'left',
  className = '',
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen]);

  return (
    <div ref={containerRef} className={`relative inline-block ${className}`}>
      <div onClick={() => setIsOpen(!isOpen)} className="cursor-pointer">
        {trigger}
      </div>

      {isOpen && (
        <div
          className={`absolute top-[calc(100%+6px)] z-50 min-w-[200px] rounded-lg border border-[#dee2e6] bg-white py-1.5 shadow-xl ${
            align === 'right' ? 'right-0' : 'left-0'
          }`}
        >
          {/* Bubble Pointer Arrow */}
          <span
            className={`absolute -top-1.5 h-3 w-3 rotate-45 border-t border-l border-[#dee2e6] bg-white ${
              align === 'right' ? 'right-4' : 'left-4'
            }`}
          />

          {header && (
            <div className="border-b border-[#f1f3f6] px-3 py-1 text-[10px] font-bold uppercase tracking-wider text-[#6c757d]">
              {header}
            </div>
          )}

          <div className="flex flex-col">
            {items.map((item, idx) => {
              if (item.divider) {
                return <div key={`div-${idx}`} className="my-1 border-t border-[#f1f3f6]" />;
              }

              return (
                <button
                  key={item.id}
                  type="button"
                  disabled={item.disabled}
                  onClick={() => {
                    item.onClick();
                    setIsOpen(false);
                  }}
                  className={`flex w-full items-center gap-2 px-3 py-1.5 text-left text-xs font-medium transition cursor-pointer ${
                    item.disabled
                      ? 'opacity-40 cursor-not-allowed'
                      : item.danger
                      ? 'text-rose-600 hover:bg-rose-50'
                      : 'text-[#495057] hover:bg-[#f1f3f6] hover:text-[#1f63c9]'
                  }`}
                >
                  {item.icon && <span className="h-3.5 w-3.5">{item.icon}</span>}
                  <span>{item.label}</span>
                </button>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
};
