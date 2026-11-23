import {
  UserPlus, HeartPulse, CreditCard, BarChart3, Settings, TrendingUp,
  Users, Key, Monitor, Package, Pill, Building2, Server,
  Award, GitFork, FileDiff, FileText, Syringe, Activity, ScanLine,
  Database, BookOpen, Stethoscope, Landmark, Layout, SlidersHorizontal
} from 'lucide-react';
import React from 'react';

export interface FlyoutMenuItem {
  id: string;
  title: string;
  subtitle?: string;
  icon?: React.ComponentType<any>;
}

export interface SubMenuItem {
  id: string;
  title: string;
  icon: React.ComponentType<any>;
  flyoutItems?: FlyoutMenuItem[];
}

export interface MenuItem {
  id: string;
  title: string;
  icon: React.ComponentType<any>;
  subItems?: SubMenuItem[];
}

export const menuConfig: MenuItem[] = [
  {
    id: 'reception_group',
    title: 'Tiếp đón',
    icon: UserPlus,
    subItems: [
      { id: 'tiep_nhan', title: 'Tiếp nhận & Hàng đợi', icon: UserPlus },
      { id: 'patients', title: 'Danh sách bệnh nhân & Lịch hẹn', icon: Users },
      { id: 'the_thanh_vien', title: 'Thẻ thành viên & Ưu đãi', icon: Award },
      { id: 'man_hinh_hang_doi', title: 'TV Kiosk hàng đợi', icon: Monitor }
    ]
  },
  {
    id: 'clinical_group',
    title: 'Khám Bệnh',
    icon: HeartPulse,
    subItems: [
      { id: 'phong_kham', title: 'Khám bệnh ngoại trú', icon: HeartPulse },
      { id: 'medical_records', title: 'Bệnh án điện tử HSBA', icon: FileText },
      { id: 'chuyen_kham_chuyen_khoa', title: 'Chuyển khám chuyên khoa', icon: GitFork },
      { id: 'vaccine_clinical', title: 'Khám & Tư vấn vaccine', icon: Syringe }
    ]
  },
  {
    id: 'paraclinical_group',
    title: 'Cận lâm sàng',
    icon: Activity,
    subItems: [
      { id: 'chan_doan_hinh_anh', title: 'CĐHA/PACS/DICOM', icon: ScanLine },
      { id: 'can_lam_sang_dieu_duong', title: 'Xét nghiệm & Thủ thuật ĐD', icon: Activity }
    ]
  },
  {
    id: 'billing_group',
    title: 'Thu ngân',
    icon: CreditCard,
    subItems: [
      { id: 'thu_ngan', title: 'Thu viện phí, Gói Vaccine & VietQR', icon: CreditCard },
      { id: 'hoa_don_dien_tu', title: 'Hóa đơn FAST ERP', icon: FileText },
      { id: 'bien_ban_dieu_chinh', title: 'Biên bản điều chỉnh BB_*', icon: FileDiff }
    ]
  },
  {
    id: 'pharmacy_group',
    title: 'Nhà thuốc & Kho',
    icon: Package,
    subItems: [
      { id: 'nha_thuoc', title: 'Nhà thuốc Bán toa & Lẻ', icon: Pill },
      { id: 'inventory', title: 'Kho dược & Lô hạn Odoo', icon: Package },
      { id: 'dieu_chinh_kho', title: 'Điều chỉnh tồn kho', icon: FileDiff },
      { id: 'du_tru_thuoc', title: 'Dự trù thuốc', icon: TrendingUp }
    ]
  },
  {
    id: 'corporate_group',
    title: 'Khám đoàn B2B',
    icon: Building2,
    subItems: [
      { id: 'corporate_checkup', title: 'Khám đoàn doanh nghiệp B2B & TT14', icon: Building2 }
    ]
  },
  {
    id: 'system_group',
    title: 'Hệ thống & Báo cáo',
    icon: Server,
    subItems: [
      {
        id: 'master_catalogs',
        title: 'Danh mục Hệ thống (Master Data)',
        icon: Database,
        flyoutItems: [
          { id: 'master_catalogs_demographics', title: 'Hành chính & Nhân khẩu', subtitle: 'Đối tượng, giới tính, dân tộc, địa bàn', icon: Users },
          { id: 'master_catalogs_clinical', title: 'Chuyên môn Lâm sàng & CLS', subtitle: 'ICD-10, mẫu bệnh án, tốc ký, tiền sử', icon: HeartPulse },
          { id: 'master_catalogs_occupational', title: 'KSK Doanh nghiệp & M3', subtitle: 'Mẫu M3 192 trường, TT32, kết luận', icon: FileText },
          { id: 'master_catalogs_financial', title: 'Tài chính, Bảng giá & HĐĐT', subtitle: 'Bảng giá dịch vụ, nguồn chi trả, TT78', icon: CreditCard },
          { id: 'master_catalogs_staff', title: 'Nhân sự & Bác sĩ (CCHN)', subtitle: 'Bác sĩ, điều dưỡng, CCHN, chức danh', icon: Stethoscope },
          { id: 'master_catalogs_system_config', title: 'Cấu hình & Chuỗi PK', subtitle: 'Chuỗi phòng khám, cơ sở chi nhánh, ERP, TTS', icon: Settings }
        ]
      },
      { id: 'operations_dashboard', title: 'Điều phối vận hành & Tải', icon: Activity },
      { id: 'bao_cao', title: 'Báo cáo doanh thu & S2-a', icon: BarChart3 },
      { id: 'phan_quyen', title: 'Phân quyền ACL & Chi nhánh', icon: Key },
      { id: 'nhan_vien', title: 'Nhân sự & Chấm công', icon: Users },
      {
        id: 'smartstore_samples',
        title: 'Mẫu SmartStore & Controls (Dev)',
        icon: Layout,
        flyoutItems: [
          { id: 'design_system', title: 'Thư viện Mẫu (Design System)', subtitle: 'Components, badges, forms chuẩn SmartStore', icon: Layout },
          { id: 'smartstore_controls', title: 'SmartStore REA Controls Catalog', subtitle: 'DataGrid, TabRail, TreeView demo', icon: SlidersHorizontal }
        ]
      }
    ]
  }
];
