import React, { useState } from 'react';
import {
  Settings, Network, FileSpreadsheet, Activity, ShieldCheck,
  Search, ChevronDown, ChevronRight
} from 'lucide-react';
import { NationalOutboxPortalPage } from './NationalOutboxPortalPage';
import { EInvoicePortalPage } from './EInvoicePortalPage';
import { LisGatewayConfigPage } from './LisGatewayConfigPage';
import { RolePermissionAdminPage } from './RolePermissionAdminPage';

type SettingsTab = 'national_gateways' | 'einvoice' | 'lis_gateway' | 'rbac';

interface SettingsBlock {
  id: string;
  title: string;
  items: Array<{
    id: SettingsTab;
    name: string;
    icon: React.ComponentType<{ className?: string }>;
  }>;
}

const SETTINGS_BLOCKS: SettingsBlock[] = [
  {
    id: 'gateways',
    title: 'Cổng Liên thông',
    items: [
      { id: 'national_gateways', name: 'Cổng BYT (M3 & Đơn thuốc QG)', icon: Network },
      { id: 'einvoice', name: 'Hóa đơn Điện tử (TT78)', icon: FileSpreadsheet }
    ]
  },
  {
    id: 'devices',
    title: 'Thiết bị & CLS',
    items: [
      { id: 'lis_gateway', name: 'LIS & Máy Xét Nghiệm', icon: Activity }
    ]
  },
  {
    id: 'security',
    title: 'Quản trị & Bảo mật',
    items: [
      { id: 'rbac', name: 'Phân quyền ACL (Admin)', icon: ShieldCheck }
    ]
  }
];

export const AdminSettingsPage: React.FC<{ initialTab?: SettingsTab }> = ({ initialTab = 'national_gateways' }) => {
  const [tab, setTab] = useState<SettingsTab>(initialTab);
  const [search, setSearch] = useState('');
  const [expandedBlockIds, setExpandedBlockIds] = useState<string[]>(['gateways', 'devices', 'security']);

  const toggleBlock = (id: string) => {
    setExpandedBlockIds(prev => prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]);
  };

  const q = search.trim().toLowerCase();

  return (
    <div className="flex flex-1 min-h-0 overflow-hidden bg-[#f8fafc]">
      {/* CỘT TRÁI: NAVIGATOR CHUẨN MASTER-DETAIL NHƯ DANH MỤC (265px) */}
      <nav
        className="border-r border-[#dee2e6] bg-white flex flex-col select-none shrink-0"
        style={{ width: '265px' }}
      >
        <div className="px-3 py-2 bg-[#f8f9fa] border-b border-[#dee2e6] flex items-center justify-between shrink-0">
          <div className="flex items-center gap-2">
            <Settings className="w-4 h-4 text-[#1f63c9]" />
            <span className="text-xs font-bold tracking-wider uppercase text-[#212529]">
              CÀI ĐẶT ADMIN
            </span>
          </div>
          <span className="text-[10px] font-mono font-semibold text-[#1f63c9] bg-blue-50 px-1.5 py-0.5 rounded border border-blue-200">
            4 mục
          </span>
        </div>

        <div className="p-2 border-b border-[#dee2e6] shrink-0">
          <div className="relative">
            <Search className="w-3.5 h-3.5 absolute left-2.5 top-2 text-[#6c757d]" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Tìm cấu hình..."
              className="w-full pl-8 pr-2.5 py-1 bg-[#f8f9fa] border border-[#ced4da] rounded text-xs text-[#212529] placeholder-[#adb5bd] focus:outline-none focus:border-[#1f63c9] focus:bg-white transition"
            />
          </div>
        </div>

        <div className="flex-1 overflow-y-auto p-1.5 space-y-1">
          {SETTINGS_BLOCKS.map(block => {
            const visibleItems = block.items.filter(it => !q || it.name.toLowerCase().includes(q));
            if (visibleItems.length === 0) return null;
            const isExpanded = q ? true : expandedBlockIds.includes(block.id);

            return (
              <div key={block.id} className="rounded-sm overflow-hidden">
                <button
                  type="button"
                  onClick={() => toggleBlock(block.id)}
                  className="w-full flex items-center justify-between px-2.5 py-2 text-xs font-semibold text-[#495057] hover:bg-[#f8f9fa] rounded transition cursor-pointer"
                >
                  <span>{block.title}</span>
                  <div className="flex items-center gap-1.5">
                    <span className="text-[10px] font-mono bg-[#f1f3f6] text-[#495057] px-1.5 py-0.2 rounded">
                      {visibleItems.length}
                    </span>
                    {isExpanded ? <ChevronDown className="w-3.5 h-3.5 text-[#868e96]" /> : <ChevronRight className="w-3.5 h-3.5 text-[#868e96]" />}
                  </div>
                </button>

                {isExpanded && (
                  <div className="mt-0.5 space-y-0.5 pl-2">
                    {visibleItems.map(item => {
                      const Icon = item.icon;
                      const isActive = tab === item.id;
                      return (
                        <button
                          key={item.id}
                          type="button"
                          onClick={() => setTab(item.id)}
                          className={`w-full flex items-center gap-2 px-2.5 py-1.5 rounded text-xs transition cursor-pointer ${
                            isActive
                              ? 'bg-[#1f63c9] text-white font-semibold shadow-2xs'
                              : 'text-[#212529] hover:bg-[#f1f3f6]'
                          }`}
                        >
                          <Icon className={`w-3.5 h-3.5 shrink-0 ${isActive ? 'text-white' : 'text-[#6c757d]'}`} />
                          <span className="truncate text-left">{item.name}</span>
                        </button>
                      );
                    })}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </nav>

      {/* CỘT PHẢI: NỘI DUNG CẤU HÌNH CHI TIẾT */}
      <main className="flex-1 flex flex-col bg-[#f8fafc] overflow-auto min-w-0">
        {tab === 'national_gateways' && <NationalOutboxPortalPage compact />}
        {tab === 'einvoice' && <EInvoicePortalPage compact />}
        {tab === 'lis_gateway' && <LisGatewayConfigPage />}
        {tab === 'rbac' && <RolePermissionAdminPage />}
      </main>
    </div>
  );
};
