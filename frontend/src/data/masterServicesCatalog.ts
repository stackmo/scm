export type PriceTier = 'standard' | 'bhyt' | 'bhtm' | 'deal';

export interface ServiceItem {
  id: string;
  code: string;
  name: string;
  category: 'kham' | 'goi_kham' | 'xet_nghiem' | 'cdha' | 'thu_thuat' | 'vaccine';
  subCategory?: string;
  defaultRoomCode?: string;
  defaultRoomName?: string;
  // Multi-tier price book
  priceStandard: number;      // Giá niêm yết tại viện
  priceBhyt: number;          // Giá quỹ BHYT thanh toán (hoặc mức định mức BYT)
  priceBhtm: number;          // Giá áp dụng Bảo hiểm thương mại
  priceDeal: number;          // Giá hợp đồng B2B / Deal Sales
  // Nếu là gói khám (Bundle)
  isPackage?: boolean;
  bundledServiceIds?: string[]; // Danh sách ID các dịch vụ con bên trong
  packageDescription?: string;
  legacyId?: number;
}

export const MASTER_SERVICES_CATALOG: ServiceItem[] = [
  // ══════════════════════════════════════════════════════════════════
  // 1. GÓI KHÁM (BUNDLES / PACKAGES)
  // ══════════════════════════════════════════════════════════════════
  {
    id: 'pkg-home-01',
    code: 'GOI_TAINHA_TQ',
    name: 'Gói Khám & Lấy Mẫu Xét Nghiệm Tại Nhà Toàn Diện',
    category: 'goi_kham',
    subCategory: 'Tại Nhà',
    defaultRoomCode: 'P.101',
    defaultRoomName: 'Đội Khám Tại Nhà',
    priceStandard: 1200000,
    priceBhyt: 0,
    priceBhtm: 1100000,
    priceDeal: 950000,
    isPackage: true,
    packageDescription: 'Bao gồm: Khám BS Gia Đình + Lấy máu tại nhà + Tổng phân tích máu + Đường huyết + Men gan + ECG tại chỗ',
    bundledServiceIds: ['srv-kham-01', 'srv-xn-01', 'srv-xn-02', 'srv-xn-03', 'srv-cdha-04']
  },
  {
    id: 'pkg-skdk-01',
    code: 'GOI_SKDK_TT32',
    name: 'Gói Khám Sức Khỏe Định Kỳ Doanh Nghiệp (TT32/BYT)',
    category: 'goi_kham',
    subCategory: 'Doanh Nghiệp',
    defaultRoomCode: 'P.101',
    defaultRoomName: 'Khu Khám Đoàn',
    priceStandard: 1450000,
    priceBhyt: 420000,
    priceBhtm: 1300000,
    priceDeal: 850000,
    isPackage: true,
    packageDescription: 'Đầy đủ danh mục Thông tư 32/2023/TT-BYT: Khám Thể lực, Nội, Ngoại, Da liễu, Mắt, TMH, RHM, CTM, Đường huyết, X-quang Ngực',
    bundledServiceIds: ['srv-kham-01', 'srv-xn-01', 'srv-xn-02', 'srv-cdha-01']
  },
  {
    id: 'pkg-tien-hon-nhan',
    code: 'GOI_TIEN_HONNHAN',
    name: 'Gói Khám & Tầm Soát Tiền Hôn Nhân (Nam/Nữ)',
    category: 'goi_kham',
    subCategory: 'Tầm Soát',
    defaultRoomCode: 'P.301',
    defaultRoomName: 'Phòng khám Sản - Phụ Khoa',
    priceStandard: 2200000,
    priceBhyt: 500000,
    priceBhtm: 2000000,
    priceDeal: 1750000,
    isPackage: true,
    packageDescription: 'Khám chuyên khoa Sản/Nam học + Siêu âm bụng/phụ khoa + Bộ xét nghiệm di truyền & truyền nhiễm',
    bundledServiceIds: ['srv-kham-03', 'srv-xn-01', 'srv-xn-06', 'srv-cdha-02']
  },

  // ══════════════════════════════════════════════════════════════════
  // 2. KHÁM CHUYÊN KHOA
  // ══════════════════════════════════════════════════════════════════
  {
    id: 'srv-kham-01',
    code: 'K_NOI_TQ',
    name: 'Khám Nội Tổng Quát & Kê đơn',
    category: 'kham',
    subCategory: 'Nội Khoa',
    defaultRoomCode: 'P.101',
    defaultRoomName: 'Phòng khám Nội Tổng Quát 1',
    priceStandard: 150000,
    priceBhyt: 42500,
    priceBhtm: 150000,
    priceDeal: 120000
  },
  {
    id: 'srv-kham-02',
    code: 'K_NHI_TQ',
    name: 'Khám Nhi Khoa & Tư vấn dinh dưỡng',
    category: 'kham',
    subCategory: 'Nhi Khoa',
    defaultRoomCode: 'P.201',
    defaultRoomName: 'Phòng khám Nhi Khoa 1',
    priceStandard: 150000,
    priceBhyt: 42500,
    priceBhtm: 150000,
    priceDeal: 120000
  },
  {
    id: 'srv-kham-03',
    code: 'K_SAN_PK',
    name: 'Khám Sản - Phụ Khoa định kỳ',
    category: 'kham',
    subCategory: 'Sản Khoa',
    defaultRoomCode: 'P.301',
    defaultRoomName: 'Phòng khám Sản - Phụ Khoa 1',
    priceStandard: 200000,
    priceBhyt: 45000,
    priceBhtm: 200000,
    priceDeal: 160000
  },
  {
    id: 'srv-kham-04',
    code: 'K_MAT_TD',
    name: 'Khám Mắt Toàn Diện & Đo Khúc Xạ',
    category: 'kham',
    subCategory: 'Mắt',
    defaultRoomCode: 'P.203',
    defaultRoomName: 'Phòng khám Mắt & Đo Khúc Xạ',
    priceStandard: 180000,
    priceBhyt: 40000,
    priceBhtm: 180000,
    priceDeal: 140000
  },
  {
    id: 'srv-kham-05',
    code: 'K_TMH_OM',
    name: 'Khám Tai Mũi Họng Ống Mềm',
    category: 'kham',
    subCategory: 'TMH',
    defaultRoomCode: 'P.204',
    defaultRoomName: 'Phòng khám Tai Mũi Họng',
    priceStandard: 200000,
    priceBhyt: 45000,
    priceBhtm: 200000,
    priceDeal: 150000
  },
  {
    id: 'srv-kham-06',
    code: 'K_VC_SL',
    name: 'Khám Sàng Lọc Trước Tiêm Chủng',
    category: 'kham',
    subCategory: 'Tiêm Chủng',
    defaultRoomCode: 'P.103',
    defaultRoomName: 'Phòng Tiêm chủng Vaccine',
    priceStandard: 50000,
    priceBhyt: 0,
    priceBhtm: 50000,
    priceDeal: 0
  },

  // ══════════════════════════════════════════════════════════════════
  // 3. XÉT NGHIỆM (LIS)
  // ══════════════════════════════════════════════════════════════════
  {
    id: 'srv-xn-01',
    code: 'XN_CTM',
    name: 'Tổng phân tích tế bào máu ngoại vi (CBC 18 chỉ số)',
    category: 'xet_nghiem',
    subCategory: 'Huyết học',
    defaultRoomCode: 'P.LAB',
    defaultRoomName: 'Phòng Xét Nghiệm',
    priceStandard: 95000,
    priceBhyt: 46000,
    priceBhtm: 95000,
    priceDeal: 75000
  },
  {
    id: 'srv-xn-02',
    code: 'XN_GLU',
    name: 'Đường huyết đói (Glucose)',
    category: 'xet_nghiem',
    subCategory: 'Sinh hóa',
    defaultRoomCode: 'P.LAB',
    defaultRoomName: 'Phòng Xét Nghiệm',
    priceStandard: 40000,
    priceBhyt: 21000,
    priceBhtm: 40000,
    priceDeal: 30000
  },
  {
    id: 'srv-xn-03',
    code: 'XN_GAN',
    name: 'Men gan AST (GOT) & ALT (GPT)',
    category: 'xet_nghiem',
    subCategory: 'Sinh hóa',
    defaultRoomCode: 'P.LAB',
    defaultRoomName: 'Phòng Xét Nghiệm',
    priceStandard: 80000,
    priceBhyt: 42000,
    priceBhtm: 80000,
    priceDeal: 60000
  },
  {
    id: 'srv-xn-04',
    code: 'XN_THAN',
    name: 'Chức năng thận (Ure, Creatinine)',
    category: 'xet_nghiem',
    subCategory: 'Sinh hóa',
    defaultRoomCode: 'P.LAB',
    defaultRoomName: 'Phòng Xét Nghiệm',
    priceStandard: 90000,
    priceBhyt: 44000,
    priceBhtm: 90000,
    priceDeal: 70000
  },
  {
    id: 'srv-xn-05',
    code: 'XN_MOMAU',
    name: 'Bộ mỡ máu (Cholesterol, Triglyceride, HDL, LDL)',
    category: 'xet_nghiem',
    subCategory: 'Sinh hóa',
    defaultRoomCode: 'P.LAB',
    defaultRoomName: 'Phòng Xét Nghiệm',
    priceStandard: 160000,
    priceBhyt: 80000,
    priceBhtm: 160000,
    priceDeal: 120000
  },
  {
    id: 'srv-xn-06',
    code: 'XN_NT_10',
    name: 'Tổng phân tích nước tiểu (10 thông số)',
    category: 'xet_nghiem',
    subCategory: 'Nước tiểu',
    defaultRoomCode: 'P.LAB',
    defaultRoomName: 'Phòng Xét Nghiệm',
    priceStandard: 50000,
    priceBhyt: 25000,
    priceBhtm: 50000,
    priceDeal: 35000
  },

  // ══════════════════════════════════════════════════════════════════
  // 4. CHẨN ĐOÁN HÌNH ẢNH (RIS/PACS)
  // ══════════════════════════════════════════════════════════════════
  {
    id: 'srv-cdha-01',
    code: 'XR_NGUC',
    name: 'Chụp X-Quang Ngực Thẳng Kỹ Thuật Số (Chest AP/PA)',
    category: 'cdha',
    subCategory: 'X-Quang',
    defaultRoomCode: 'P.104',
    defaultRoomName: 'Phòng X-Quang Kỹ Thuật Số',
    priceStandard: 140000,
    priceBhyt: 65000,
    priceBhtm: 140000,
    priceDeal: 100000
  },
  {
    id: 'srv-cdha-02',
    code: 'SA_OBUNG',
    name: 'Siêu âm Ổ bụng tổng quát Doppler màu',
    category: 'cdha',
    subCategory: 'Siêu âm',
    defaultRoomCode: 'P.205',
    defaultRoomName: 'Phòng Siêu Âm Tổng Quát',
    priceStandard: 180000,
    priceBhyt: 82000,
    priceBhtm: 180000,
    priceDeal: 130000
  },
  {
    id: 'srv-cdha-03',
    code: 'SA_THAI_4D',
    name: 'Siêu âm Thai 4D / Hình thái học thai nhi',
    category: 'cdha',
    subCategory: 'Siêu âm',
    defaultRoomCode: 'P.302',
    defaultRoomName: 'Phòng khám Sản - Phụ Khoa 2 (Thai)',
    priceStandard: 350000,
    priceBhyt: 0,
    priceBhtm: 350000,
    priceDeal: 300000
  },
  {
    id: 'srv-cdha-04',
    code: 'CDHA_ECG',
    name: 'Điện tim (ECG 12 chuyển đạo)',
    category: 'cdha',
    subCategory: 'Thăm dò chức năng',
    defaultRoomCode: 'P.101',
    defaultRoomName: 'Phòng Thăm Dò Chức Năng',
    priceStandard: 100000,
    priceBhyt: 42000,
    priceBhtm: 100000,
    priceDeal: 70000
  },

  // ══════════════════════════════════════════════════════════════════
  // 5. THỦ THUẬT & VACCINE
  // ══════════════════════════════════════════════════════════════════
  {
    id: 'srv-tt-01',
    code: 'TT_RUA_XOANG',
    name: 'Hút rửa mũi xoang / Khí dung áp lực',
    category: 'thu_thuat',
    subCategory: 'TMH',
    defaultRoomCode: 'P.204',
    defaultRoomName: 'Phòng khám Tai Mũi Họng',
    priceStandard: 120000,
    priceBhyt: 35000,
    priceBhtm: 120000,
    priceDeal: 90000
  },
  {
    id: 'srv-vc-01',
    code: 'VC_6IN1_HEXA',
    name: 'Vắc xin 6in1 Infanrix Hexa (Bỉ)',
    category: 'vaccine',
    subCategory: 'Vaccine Trẻ Em',
    defaultRoomCode: 'P.103',
    defaultRoomName: 'Phòng Tiêm chủng Vaccine',
    priceStandard: 965000,
    priceBhyt: 0,
    priceBhtm: 965000,
    priceDeal: 965000
  },
  {
    id: 'srv-vc-02',
    code: 'VC_CUM_INFLU',
    name: 'Vắc xin Cúm Mùa Tứ Giá (Vaxigrip Tetra)',
    category: 'vaccine',
    subCategory: 'Vaccine Người Lớn',
    defaultRoomCode: 'P.103',
    defaultRoomName: 'Phòng Tiêm chủng Vaccine',
    priceStandard: 350000,
    priceBhyt: 0,
    priceBhtm: 350000,
    priceDeal: 320000
  }
];

export interface PatientContextForPricing {
  isBhyt: boolean;
  bhytCode?: string;
  bhytBenefitRate?: number; // Ví dụ 80% hoặc 100%
  customerType: 'individual' | 'corporate';
  companyId?: string;
  isBhtm: boolean;
  bhtmProvider?: string;
}

/**
 * Tự động xác định Price Tier dựa vào ngữ cảnh đợt khám của bệnh nhân
 */
export function detectDefaultPriceTier(ctx: PatientContextForPricing): PriceTier {
  if (ctx.customerType === 'corporate') {
    return 'deal';
  }
  if (ctx.isBhyt && ctx.bhytCode && ctx.bhytCode.trim().length > 0) {
    return 'bhyt';
  }
  if (ctx.isBhtm) {
    return 'bhtm';
  }
  return 'standard';
}

/**
 * Tính số tiền thực thu của bệnh nhân dựa trên Price Tier
 */
export function calculateServicePrice(
  service: ServiceItem,
  tier: PriceTier,
  bhytRate: number = 0.8
): { unitPrice: number; patientPayable: number; insuranceCoverage: number; discount: number } {
  switch (tier) {
    case 'bhyt': {
      const unit = service.priceBhyt > 0 ? service.priceBhyt : service.priceStandard;
      const coverage = service.priceBhyt > 0 ? service.priceBhyt * bhytRate : 0;
      const payable = service.priceBhyt > 0 ? (service.priceBhyt * (1 - bhytRate)) : service.priceStandard;
      return {
        unitPrice: unit,
        patientPayable: Math.round(payable),
        insuranceCoverage: Math.round(coverage),
        discount: 0
      };
    }
    case 'deal': {
      // B2B: Công ty trả toàn bộ theo giá deal
      return {
        unitPrice: service.priceDeal,
        patientPayable: 0,
        insuranceCoverage: 0,
        discount: service.priceStandard - service.priceDeal
      };
    }
    case 'bhtm': {
      return {
        unitPrice: service.priceBhtm,
        patientPayable: 0, // Bảo lãnh viện phí trực tiếp
        insuranceCoverage: service.priceBhtm,
        discount: service.priceStandard - service.priceBhtm
      };
    }
    case 'standard':
    default: {
      return {
        unitPrice: service.priceStandard,
        patientPayable: service.priceStandard,
        insuranceCoverage: 0,
        discount: 0
      };
    }
  }
}
