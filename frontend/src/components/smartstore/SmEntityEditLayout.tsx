import React from 'react';
import { SmStickyActionBar } from './SmStickyActionBar';
import type { SmActionItem } from './SmStickyActionBar';
import { SmVerticalTabRail } from './SmVerticalTabRail';
import type { SmTabItem } from './SmVerticalTabRail';

export interface SmEntityEditLayoutProps {
  title: string;
  subtitle?: string;
  thumbnailUrl?: string;
  onBack?: () => void;
  actions?: SmActionItem[];
  tabs: SmTabItem[];
  activeTabId: string;
  onTabChange: (tabId: string) => void;
  children: React.ReactNode;
  className?: string;
  tabRailWidth?: string;
}

export const SmEntityEditLayout: React.FC<SmEntityEditLayoutProps> = ({
  title,
  subtitle,
  thumbnailUrl,
  onBack,
  actions = [],
  tabs,
  activeTabId,
  onTabChange,
  children,
  className = '',
  tabRailWidth = '230px',
}) => {
  return (
    <div className={`flex flex-col h-full w-full bg-[#f1f3f6] text-xs text-[#212529] ${className}`}>
      {/* 1. Sticky Action Bar */}
      <SmStickyActionBar
        title={title}
        subtitle={subtitle}
        thumbnailUrl={thumbnailUrl}
        onBack={onBack}
        actions={actions}
      />

      {/* 2. Main Body: Left Vertical Tab Rail + Right Scrollable Content */}
      <div className="flex flex-1 min-h-0 overflow-hidden">
        {/* Left: Signature Vertical Tab Rail */}
        <SmVerticalTabRail
          tabs={tabs}
          activeTabId={activeTabId}
          onTabChange={onTabChange}
          width={tabRailWidth}
        />

        {/* Right: Content Area */}
        <main className="flex-1 overflow-y-auto p-6 min-w-0">
          <div className="mx-auto max-w-6xl space-y-6">{children}</div>
        </main>
      </div>
    </div>
  );
};
