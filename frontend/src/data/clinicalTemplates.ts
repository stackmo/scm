// Frontend/src/data/clinicalTemplates.ts
// Standard clinical diagnostic templates for Ultrasound, Endoscopy, and Radiology

export type SpecialtyDomain = 'ALL' | 'SPK' | 'TMH' | 'GENERAL_US';

export interface SpeedMacro {
  trigger: string;
  replacement: string;
  category: 'GENERAL' | 'OBSTETRIC' | 'GYNECOLOGY' | 'ENT' | 'CARDIAC' | 'THYROID';
}

export interface ClinicalTemplate {
  id: string;
  code: string;
  category: 'US_CARDIAC' | 'ES_ENT' | 'US_GYN' | 'US_THYROID' | 'ES_COLPO' | 'US_ABDOMEN' | 'DX_CHEST' | 'US_OB_4D' | 'US_BREAST_BIRADS' | 'US_CAROTID';
  specialty: SpecialtyDomain;
  title: string;
  modality: 'US' | 'ES' | 'DX';
  modalityLabel: string;
  procedureName: string;
  deviceModel: string;
  bodyPart: string;
  findings: string;
  conclusion: string;
  icd10: string;
  recommendations: string;
  defaultPrintLayout?: 'A4_PORTRAIT_2IMG' | 'A4_PORTRAIT_4IMG' | 'A4_PORTRAIT_1IMG' | 'A5_LANDSCAPE_2IMG';
  sampleImages?: string[];
  structuredFields?: Record<string, string | number>;
}

export const CLINICAL_TEMPLATES: ClinicalTemplate[] = [
  {
    id: 'tpl-us-cardiac',
    code: 'US-CARD-01',
    category: 'US_CARDIAC',
    specialty: 'GENERAL_US',
    defaultPrintLayout: 'A4_PORTRAIT_2IMG',
    title: 'Siêu âm Tim Doppler màu (Transthoracic Echocardiography)',
    modality: 'US',
    modalityLabel: 'Siêu Âm Tim Doppler (TTE)',
    procedureName: 'Siêu âm tim và mạch máu lớn qua thành ngực',
    deviceModel: 'GE Healthcare Vivid E95 4D Cardiovascular Ultrasound',
    bodyPart: 'HEART',
    findings: `1. Kích thước các buồng tim:
- Thất trái: LVEDD: 46 mm (35-51), LVESD: 28 mm (21-35), EF (Teichholz): 65% (>= 53%), FS: 36% (>= 27%).
- Bề dày vách liên thất (IVSd): 9 mm (6-10), thành sau thất trái (LVPWd): 9 mm (6-10).
- Nhĩ trái (LA): 32 mm (< 38).
- Gốc động mạch chủ (Ao): 29 mm (< 37).
- Thất phải (RVDd): 20 mm (< 25).
2. Chức năng van tim:
- Van 2 lá: Mép van thanh mảnh, mở tốt. Dòng hở nhẹ 1/4 sinh lý (diện tích dòng hở < 1.5 cm2). E/A = 1.1.
- Van 3 lá: Thanh mảnh, hở nhẹ 1/4. Chênh áp tâm thu qua van 3 lá: TR Vmax = 2.1 m/s, chênh áp PG = 18 mmHg.
- Áp lực động mạch phổi ước tính (PAPs): 23 mmHg (bình thường < 30 mmHg).
- Van động mạch chủ: 3 lá van mở tốt, không hẹp hở. Chênh áp qua van bình thường (Vmax = 1.3 m/s).
- Van động mạch phổi: Bình thường.
3. Thành tim và màng ngoài tim:
- Không thấy rối loạn vận động vùng thành tim lúc nghỉ.
- Không thấy dịch màng ngoài tim hay huyết khối trong các buồng tim.`,
    conclusion: 'Chức năng tâm thu thất trái bảo tồn (EF = 65%). Hở van hai lá nhẹ 1/4 sinh lý. Không tăng áp lực động mạch phổi.',
    icd10: 'I34.0 - Hở van hai lá nhẹ sinh lý',
    recommendations: 'Tái khám và siêu âm tim kiểm tra định kỳ mỗi 12 tháng. Kiểm soát huyết áp và lối sống lành mạnh.',
    structuredFields: {
      ef: 65,
      fs: 36,
      lvedd: 46,
      lvesd: 28,
      ivsd: 9,
      lvpwd: 9,
      la: 32,
      ao: 29,
      paps: 23,
      mitralRegurg: '1/4'
    }
  },
  {
    id: 'tpl-es-ent',
    code: 'ES-ENT-01',
    category: 'ES_ENT',
    specialty: 'TMH',
    defaultPrintLayout: 'A4_PORTRAIT_4IMG',
    title: 'Nội soi Tai Mũi Họng ống mềm (Video Endoscopy)',
    modality: 'ES',
    modalityLabel: 'Nội Soi Tai Mũi Họng (Video Endoscopy)',
    procedureName: 'Nội soi chẩn đoán Tai Mũi Họng bằng optic mềm/cứng',
    deviceModel: 'Olympus CV-170 EVIS EXERA III Video System',
    bodyPart: 'EAR_NOSE_THROAT',
    findings: `1. Tai phải và Tai trái:
- Ống tai ngoài hai bên thông thoáng, da ống tai sạch, không đọng dịch hay sáp bẩn.
- Màng nhĩ hai bên nguyên vẹn, sáng bóng, nón sáng phản chiếu rõ nét ở góc trước dưới, không co kéo hay ứ đọng dịch hòm nhĩ.
2. Mũi phải và Mũi trái:
- Niêm mạc hốc mũi nhạt màu, cuốn mũi dưới hai bên phù nề thoái hóa nhẹ, co hồi vừa khi đặt bấc co mạch.
- Khe giữa hai bên thông thoáng, có ít dịch nhầy trong xuất tiết, không thấy polyp hay khối u sùi.
- Vách ngăn mũi tương đối thẳng, không gai hay mào vách ngăn chèn ép cuốn.
- Khe trên và lỗ thông xoang thông thoáng.
3. Họng - Thanh quản:
- Vòm họng thông thoáng, nóc vòm sạch, hai hốc Rosenmuller không loét sùi.
- Niêm mạc họng viêm đỏ nhẹ, hai amidan khẩu cái quá phát độ II, bề mặt có ít chấm mủ bã đậu trong các hốc.
- Đáy lưỡi, rãnh lưỡi - thanh thiệt và xoang lê hai bên sạch.
- Sụn thanh thiệt mềm mại, hai dây thanh âm trắng bóng, bờ thẳng, di động tốt và khép kín khi phát âm, không hạt xơ hay polyp.`,
    conclusion: 'Viêm mũi xoang xuất tiết dị ứng - Viêm Amidan mạn tính thể quá phát độ II hốc mủ. Chưa phát hiện tổn thương ác tính vùng tai mũi họng.',
    icd10: 'J35.0 - Viêm amiđan mạn tính',
    recommendations: 'Rửa mũi hằng ngày bằng nước muối sinh lý 0.9%. Điều trị nội khoa chống dị ứng và kháng viêm theo toa bác sĩ TMH. Tránh thức uống đá lạnh.',
    structuredFields: {
      tonsilGrade: 'Độ II',
      septumDeviation: 'Không',
      tympanicMembrane: 'Nguyên vẹn',
      vocalCords: 'Di động tốt, khép kín'
    }
  },
  {
    id: 'tpl-us-gyn',
    code: 'US-GYN-01',
    category: 'US_GYN',
    specialty: 'SPK',
    defaultPrintLayout: 'A4_PORTRAIT_2IMG',
    title: 'Siêu âm Phụ khoa đầu dò âm đạo (Transvaginal Ultrasound - TVUS)',
    modality: 'US',
    modalityLabel: 'Siêu Âm Đầu Dò Phụ Khoa (TVUS)',
    procedureName: 'Siêu âm tử cung và hai buồng trứng qua đường âm đạo',
    deviceModel: 'GE Healthcare Voluson E8 Expert HDlive (RIC5-9-D probe)',
    bodyPart: 'GYNECOLOGY',
    findings: `1. Tử cung:
- Tư thế: Ngả trước.
- Kích thước tử cung: Chiều trước-sau (DAP) = 38 mm, đường kính ngang = 45 mm, chiều dài = 52 mm (trong giới hạn bình thường).
- Cấu trúc cơ tử cung: Đồng nhất, không thấy hình ảnh nhân xơ (leiomyoma) hay tuyến cơ tử cung (adenomyosis).
2. Nội mạc tử cung:
- Độ dày: 8.5 mm.
- Cấu trúc: Hình 3 lá rõ nét (Triple-line pattern), tương thích với giai đoạn tăng sinh quanh ngày phóng noãn.
- Lòng tử cung không thấy polyp hay dịch ứ đọng.
3. Buồng trứng phải:
- Kích thước: 28 x 18 mm.
- Nhu mô có một nang noãn trội kích thước 18.5 x 17 mm, vỏ mỏng, dịch trong, không vách, không chồi.
4. Buồng trứng trái:
- Kích thước: 24 x 16 mm, cấu trúc nang nhỏ ngoại vi bình thường, không u hay nang bất thường.
5. Túi cùng Douglas:
- Không có dịch tự do. Không có khối u vùng chậu bất thường.`,
    conclusion: 'Tử cung và hai buồng trứng bình thường trên siêu âm đầu dò. Buồng trứng phải có nang noãn trội 18.5mm đang phát triển tốt (chu kỳ rụng trứng bình thường).',
    icd10: 'N97.9 - Khảo sát hình thái tử cung buồng trứng bình thường',
    recommendations: 'Tư vấn thời điểm quan hệ tự nhiên theo chu kỳ nang noãn. Khám phụ khoa và tầm soát Pap smear định kỳ.',
    structuredFields: {
      uterusDap: 38,
      endometriumThickness: 8.5,
      dominantFollicleRight: 18.5,
      douglasFluid: 'Âm tính'
    }
  },
  {
    id: 'tpl-us-thyroid',
    code: 'US-THYR-01',
    category: 'US_THYROID',
    specialty: 'GENERAL_US',
    defaultPrintLayout: 'A4_PORTRAIT_2IMG',
    title: 'Siêu âm Tuyến giáp phân loại ACR TI-RADS',
    modality: 'US',
    modalityLabel: 'Siêu Âm Tuyến Giáp (ACR TI-RADS)',
    procedureName: 'Siêu âm tuyến giáp và hạch vùng cổ độ phân giải cao',
    deviceModel: 'Toshiba/Canon Aplio i800 Matrix Linear Probe 14L5',
    bodyPart: 'THYROID',
    findings: `1. Kích thước và nhu mô tuyến giáp:
- Thùy phải: 14 x 16 x 42 mm. Nhu mô nền mịn đều.
- Thùy trái: 13 x 15 x 40 mm. Nhu mô nền mịn đều.
- Eo tuyến giáp: Dày 2.6 mm, không nốt khu trú.
2. Tổn thương khu trú (Nốt tuyến giáp):
- Vị trí: 1/3 giữa thùy phải, sát bờ trước.
- Kích thước nốt: 8.2 x 6.4 mm.
- Đánh giá phân loại theo tiêu chuẩn ACR TI-RADS (2017):
  + Thành phần: Đặc hoàn toàn (+2 điểm).
  + Độ hồi âm: Giảm âm nhẹ so với nhu mô giáp (+2 điểm).
  + Hình dáng: Chiều rộng lớn hơn chiều cao - Wider-than-tall (+0 điểm).
  + Đường bờ: Bờ đều, rõ nét, không gập góc hay mở rộng ngoài giáp (+0 điểm).
  + Đốm tăng âm: Không có vôi hóa vi thể hay vôi hóa viền (+0 điểm).
  => Tổng điểm ACR TI-RADS: 4 điểm (Tương ứng ACR TI-RADS 4 - Nghi ngờ vừa).
  => Kích thước nốt 8.2 mm (< 15 mm): Chưa có chỉ định chọc hút tế bào kim nhỏ (FNA).
3. Hạch cổ hai bên:
- Khảo sát các khoang hạch cổ nhóm I, II, III, IV, V, VI hai bên không thấy hạch phì đại mất rốn hay biến dạng dạng tròn.`,
    conclusion: 'Nốt giảm âm thùy phải tuyến giáp kích thước 8.2mm, phân loại ACR TI-RADS 4. Hạch cổ hai bên bình thường.',
    icd10: 'E04.1 - Bướu giáp nhân đơn độc lành tính',
    recommendations: 'Kích thước < 15mm chưa cần chọc hút FNA theo hướng dẫn của Hội Điện quang Hoa Kỳ (ACR). Siêu âm kiểm tra lại sau 6-12 tháng để đánh giá sự phát triển kích thước.',
    structuredFields: {
      rightLobeSize: '14x16x42 mm',
      leftLobeSize: '13x15x40 mm',
      noduleSize: '8.2x6.4 mm',
      tiradsScore: 4,
      tiradsClass: 'ACR TI-RADS 4',
      fnaIndication: 'Chưa cần thiết (< 15mm)'
    }
  },
  {
    id: 'tpl-es-colpo',
    code: 'ES-COLP-01',
    category: 'ES_COLPO',
    specialty: 'SPK',
    defaultPrintLayout: 'A4_PORTRAIT_4IMG',
    title: 'Soi Cổ tử cung kỹ thuật số phóng đại (Colposcopy)',
    modality: 'ES',
    modalityLabel: 'Soi Cổ Tử Cung (Colposcopy)',
    procedureName: 'Nội soi cổ tử cung phóng đại có làm test Acetic và Lugol',
    deviceModel: 'Leisegang OptiK 2 Colposcope Digital HD System',
    bodyPart: 'CERVIX',
    findings: `1. Khảo sát sơ bộ:
- Đánh giá độ thỏa đáng: Đạt yêu cầu khảo sát (Nhìn rõ toàn bộ chu vi cổ tử cung và lỗ ngoài).
- Vùng chuyển tiếp (Transformation Zone): Phân loại Type 1 (nhìn thấy rõ ràng 100% ranh giới vảy - trụ SCJ).
2. Nghiệm pháp Axit Acetic 3-5%:
- Sau khi bôi axit acetic 3%: Xuất hiện vùng biểu mô trắng mỏng nhẹ mờ quanh lỗ cổ tử cung, ranh giới mềm mại, tan biến nhanh sau 45-60 giây.
- Không thấy hình ảnh biểu mô trắng dày đục, không có viền bờ gồ ghề.
- Mạch máu: Không có mạch máu bất thường (không chấm đáy chấm thô, không hình ảnh lát đá mosaic bất thường).
3. Nghiệm pháp Lugol (Schiller test):
- Cổ tử cung bắt màu nâu cánh gián sẫm (mahogany) đều ở vùng biểu mô vảy nguyên vẹn.
- Vùng lộ tuyến quanh lỗ trong không bắt màu i-ốt ranh giới rõ (Schiller âm tính bình thường của biểu mô trụ).
4. Đánh giá chấm điểm Reid Colposcopic Index (RCI):
- Điểm RCI = 0 điểm (Tổn thương lành tính hoặc biến đổi sinh lý).`,
    conclusion: 'Hình ảnh viêm lộ tuyến cổ tử cung diện hẹp độ I lành tính. Chưa thấy hình ảnh nghi ngờ tổn thương tiền ung thư (CIN/SIL) trên soi cổ tử cung.',
    icd10: 'N86 - Xói mòn và lộ tuyến cổ tử cung',
    recommendations: 'Vệ sinh phụ khoa đúng cách. Đặt thuốc kháng viêm phụ khoa theo đơn bác sĩ. Tái khám sau sạch kinh lần tới.',
    structuredFields: {
      tzType: 'Type 1',
      aceticAcidTest: 'Biểu mô trắng mỏng nhẹ tan nhanh',
      schillerTest: 'Bắt màu nâu đồng nhất',
      reidScore: 0
    }
  },
  {
    id: 'tpl-us-abdomen',
    code: 'US-ABD-01',
    category: 'US_ABDOMEN',
    specialty: 'GENERAL_US',
    defaultPrintLayout: 'A4_PORTRAIT_2IMG',
    title: 'Siêu âm Ổ bụng tổng quát (Abdominal Ultrasound)',
    modality: 'US',
    modalityLabel: 'Siêu Âm Ổ Bụng Tổng Quát',
    procedureName: 'Siêu âm ổ bụng tổng quát gan, mật, tụy, lách, thận',
    deviceModel: 'GE Healthcare Voluson E8 Expert HDlive',
    bodyPart: 'ABDOMEN',
    findings: `1. Gan: Kích thước trong giới hạn bình thường, bờ đều, chủ mô gan đồng nhất, hồi âm bình thường. Không thấy nốt khu trú hay sỏi đường mật trong gan.
2. Túi mật: Căng đều, thành mỏng d = 2.0 mm. Lòng túi mật có hình ảnh sỏi đơn độc d = 8.5 mm, bóng lưng rõ, di động theo tư thế bệnh nhân. Đường mật ngoài gan không giãn (ống mật chủ d = 4.2 mm).
3. Tụy: Kích thước và cấu trúc bình thường, ống tụy chính không giãn.
4. Lách: Kích thước bình thường, nhu mô đều.
5. Hai thận: Kích thước bình thường, phân biệt vỏ tủy rõ. Không có sỏi, không ứ nước đài bể thận hai bên.
6. Bàng quang: Thành mỏng nhẵn, không thấy sỏi hay u sùi.
7. Tiền liệt tuyến (ở nam) / Tử cung phần phụ (ở nữ): Bình thường theo lứa tuổi. Không có dịch tự do trong khoang màng bụng.`,
    conclusion: 'Sỏi túi mật đơn độc kích thước 8.5mm, chưa có biến chứng viêm túi mật cấp. Các cơ quan khác trong ổ bụng chưa phát hiện bất thường.',
    icd10: 'K80.2 - Sỏi túi mật không kèm viêm túi mật',
    recommendations: 'Chế độ ăn giảm chất béo, hạn chế dầu mỡ và đồ chiên rán. Khám chuyên khoa Ngoại tiêu hóa theo dõi định kỳ mỗi 6 tháng.',
    structuredFields: {
      gallbladderStone: '8.5 mm',
      cholecystitisSign: 'Không',
      commonBileDuct: '4.2 mm',
      ascites: 'Âm tính'
    }
  },
  {
    id: 'tpl-dx-chest',
    code: 'DX-CHEST-01',
    category: 'DX_CHEST',
    specialty: 'GENERAL_US',
    defaultPrintLayout: 'A4_PORTRAIT_1IMG',
    title: 'X-Quang Ngực thẳng độ phân giải cao (Chest AP/PA)',
    modality: 'DX',
    modalityLabel: 'X-Quang Kỹ Thuật Số (DR)',
    procedureName: 'Chụp X-quang ngực thẳng kỹ thuật số độ phân giải cao',
    deviceModel: 'Shimadzu RadSpeed Pro Digital X-ray System',
    bodyPart: 'CHEST',
    findings: `- Phế trường hai bên sáng đều, không thấy nốt mờ đơn độc, tổn thương thâm nhiễm đông đặc hay hang lao nhu mô phổi.
- Rốn phổi hai bên đậm độ và kích thước bình thường.
- Bóng tim kích thước trong giới hạn bình thường, chỉ số tim/lồng ngực (CTR) = 0.45 (< 0.50). Quai động mạch chủ không phồng vôi hóa.
- Góc tâm hoành và sườn hoành hai bên nhọn, sáng rõ, không thấy hình mờ tù góc sườn hoành (không tràn dịch màng phổi).
- Vòm hoành hai bên liên tục, trơn láng.
- Khung xương lồng ngực và mô mềm thành ngực không gãy xẹp hay bất thường vôi hóa.`,
    conclusion: 'Hiện tại chưa thấy hình ảnh tổn thương viêm phổi, lao phổi tiến triển hay tràn dịch màng phổi trên phim chụp X-quang ngực thẳng.',
    icd10: 'R05 - Ho kéo dài, không đặc hiệu',
    recommendations: 'Theo dõi lâm sàng, điều trị nội khoa triệu chứng. Chụp kiểm tra lại sau 10-14 ngày nếu cơn ho tiếp tục kéo dài.',
    structuredFields: {
      ctr: 0.45,
      costophrenicAngles: 'Nhọn sáng rõ',
      infiltrates: 'Không có'
    }
  },
  {
    id: 'tpl-us-ob-4d',
    code: 'US-OB-4D',
    category: 'US_OB_4D',
    specialty: 'SPK',
    defaultPrintLayout: 'A4_PORTRAIT_4IMG',
    title: 'Siêu âm Thai 4D Khảo sát Hình thái học (Hadlock 4 thông số)',
    modality: 'US',
    modalityLabel: 'Siêu Âm Sản 4D Hình Thái (Hadlock)',
    procedureName: 'Siêu âm hình thái học thai nhi quý II mốc 20-24 tuần',
    deviceModel: 'GE Healthcare Voluson E10 BT20 High-End 4D Matrix',
    bodyPart: 'OBSTETRICS',
    findings: `1. Số lượng thai & ngôi thai: 01 thai sống trong tử cung, ngôi đầu, tim thai 146 lần/phút đều rõ.
2. Đo đạc sinh trắc học thai nhi (Hadlock Formula):
- Đường kính lưỡng đỉnh (BPD): 52 mm (Tương đương thai 21 tuần 6 ngày).
- Chu vi đầu (HC): 195 mm.
- Chu vi vòng bụng (AC): 172 mm.
- Chiều dài xương đùi (FL): 38 mm.
- Cân nặng thai ước tính (EFW): 465 gram (Bách phân vị thứ 50 theo INTERGROWTH-21st).
- Chiều dài xương mũi: 6.8 mm (bình thường > 4.5 mm). Nếp gấp da gáy (NF): 3.8 mm (< 6 mm).
3. Khảo sát hình thái chi tiết:
- Não thất bên: D = 6.2 mm (< 10 mm). Hố sau bình thường, vách trong suốt hiện diện.
- Tim 4 buồng cân đối, hai đường ra đại động mạch bắt chéo bình thường, không thông liên thất.
- Môi trên liên tục, không sứt môi chẻ vòm.
- Bụng: Dạ dày, bàng quang hiện diện, hai thận phân biệt rõ. Thành bụng đóng kín.
- Tứ chi: Đầy đủ 2 tay, 2 chân, đủ ngón, vận động tốt.
4. Phần phụ của thai:
- Vị trí bánh nhau: Mặt trước nhóm 1, độ trưởng thành Grannum độ 0, không bong nhau.
- Chỉ số ối (AFI): 4 góc ối cộng lại AFI = 135 mm (Bình thường: 80 - 180 mm).`,
    conclusion: 'Một thai sống trong tử cung 22 tuần phát triển tốt theo bách phân vị chuẩn. Chưa phát hiện dị tật hình thái học lớn trên siêu âm mốc 22 tuần.',
    icd10: 'Z36.8 - Khám sàng lọc trước sinh hình thái học',
    recommendations: 'Tái khám định kỳ sau 4 tuần. Tiêm ngừa uốn ván mũi 1. Làm nghiệm pháp dung nạp đường huyết (OGTT) mốc 24-28 tuần.',
    structuredFields: {
      bpd: 52,
      hc: 195,
      ac: 172,
      fl: 38,
      efw: 465,
      gaWeeks: 22,
      afi: 135
    }
  },
  {
    id: 'tpl-us-breast-birads',
    code: 'US-BRST-01',
    category: 'US_BREAST_BIRADS',
    specialty: 'SPK',
    defaultPrintLayout: 'A4_PORTRAIT_2IMG',
    title: 'Siêu âm Tuyến Vú hai bên phân loại ACR BI-RADS',
    modality: 'US',
    modalityLabel: 'Siêu Âm Vú (ACR BI-RADS)',
    procedureName: 'Siêu âm tuyến vú và hạch nách hai bên đầu dò Linear cao tần',
    deviceModel: 'Toshiba/Canon Aplio i800 Matrix Linear Probe 18L7',
    bodyPart: 'BREAST',
    findings: `1. Tuyến vú Phải:
- Da và mô dưới da bình thường, núm vú không co kéo.
- Nhu mô vú giàu mô tuyến sợi, phân bố đều.
- Vị trí 10 giờ, cách núm vú 2.5cm: Có một nốt hồi âm kém (hypoechoic), hình bầu dục (oval), giới hạn rõ, bờ đều, trục song song với mặt da (wider-than-tall), kích thước 12 x 7 mm. Bên trong không có vôi hóa, phía sau tăng âm nhẹ. Khảo sát Doppler mạch máu: Không tăng sinh mạch máu bất thường.
=> Phân loại BI-RADS vú Phải: BI-RADS 3 (Tổn thương khả năng cao lành tính, nguy cơ ác tính < 2% - Nghĩ u xơ tuyến vú Fibroadenoma).
2. Tuyến vú Trái:
- Cấu trúc mô tuyến bình thường, không thấy khối u hay nốt vi vôi hóa khu trú.
- Các ống dẫn sữa không giãn.
=> Phân loại BI-RADS vú Trái: BI-RADS 1 (Bình thường).
3. Hạch nách hai bên:
- Hạch nách hai bên không phì đại, còn rốn hạch sáng rõ, vỏ mỏng bình thường.`,
    conclusion: 'U xơ tuyến vú phải kích thước 12x7mm phân loại ACR BI-RADS 3. Tuyến vú trái bình thường BI-RADS 1.',
    icd10: 'N60.2 - U xơ tuyến vú (Fibroadenoma)',
    recommendations: 'Siêu âm vú kiểm tra lại sau 6 tháng để theo dõi kích thước. Tự khám vú hàng tháng sau khi sạch kinh.',
    structuredFields: {
      rightNodule: '12x7 mm (10h)',
      biradsRight: 'BI-RADS 3',
      biradsLeft: 'BI-RADS 1',
      axillaryLymphNodes: 'Bình thường'
    }
  },
  {
    id: 'tpl-us-carotid',
    code: 'US-CAROT-01',
    category: 'US_CAROTID',
    specialty: 'GENERAL_US',
    defaultPrintLayout: 'A4_PORTRAIT_2IMG',
    title: 'Siêu âm Doppler Động mạch Cảnh & Đốt sống hai bên',
    modality: 'US',
    modalityLabel: 'Siêu Âm Doppler Mạch Máu Cảnh',
    procedureName: 'Siêu âm Doppler màu hệ động mạch cảnh ngoài sọ và động mạch đốt sống',
    deviceModel: 'GE Healthcare Vivid E95 Vascular Preset',
    bodyPart: 'CAROTID_ARTERY',
    findings: `1. Động mạch cảnh chung (CCA), phình cảnh (Bulb) và cảnh trong (ICA) hai bên:
- Lớp nội - trung mạc (IMT): Đo tại CCA phải = 0.72 mm, CCA trái = 0.75 mm (Bình thường < 0.9 mm).
- Khảo sát mảng xơ vữa: Không thấy mảng xơ vữa gây hẹp lòng mạch ở cả hai bên.
- Vận tốc dòng chảy đỉnh tâm thu (PSV) động mạch cảnh trong (ICA):
  + ICA Phải: PSV = 74 cm/s, EDV = 28 cm/s, tỉ số ICA/CCA PSV = 1.1 (< 2.0).
  + ICA Trái: PSV = 78 cm/s, EDV = 30 cm/s, tỉ số ICA/CCA PSV = 1.2 (< 2.0).
  => Không có hiện tượng hẹp lòng động mạch cảnh trong hai bên theo tiêu chuẩn NASCET/ECST.
2. Động mạch đốt sống (VA) hai bên:
- Chiều dòng chảy hướng lên não bình thường, phổ Doppler kháng lực thấp (Low resistance waveform).
- Đường kính: VA phải d = 3.2 mm, VA trái d = 3.6 mm. Dòng chảy thông suốt.`,
    conclusion: 'Không thấy mảng xơ vữa gây hẹp động mạch cảnh trong hai bên. Dòng chảy động mạch cảnh và động mạch đốt sống hai bên thông thoáng trong giới hạn bình thường.',
    icd10: 'I65.2 - Khảo sát xơ vữa động mạch cảnh ngoài sọ bình thường',
    recommendations: 'Kiểm soát tốt chỉ số mỡ máu (Lipid panel), huyết áp và đường huyết. Siêu âm kiểm tra lại sau 12 tháng.',
    structuredFields: {
      imtRight: 0.72,
      imtLeft: 0.75,
      psvIcaRight: 74,
      psvIcaLeft: 78,
      stenosisGrade: 'Không hẹp (< 50%)'
    }
  }
];

export const SPEED_MACROS: SpeedMacro[] = [
  { trigger: 'gnt', category: 'GENERAL', replacement: 'Gan: Cấu trúc echo dày đồng nhất, giảm âm vùng thấp, bờ đều, kích thước không to.' },
  { trigger: 'tmbt', category: 'GENERAL', replacement: 'Túi mật: Kích thước bình thường, thành mỏng đều, lòng túi mật thuần trạng, không thấy sỏi.' },
  { trigger: 'tbt', category: 'GENERAL', replacement: 'Hai thận: Vị trí, kích thước bình thường. Phân biệt vỏ tủy rõ. Đài bể thận không giãn, không có sỏi.' },
  { trigger: 'lbt', category: 'GENERAL', replacement: 'Lách: Cấu trúc echo đồng nhất, kích thước trong giới hạn bình thường.' },
  { trigger: 'tqlbt', category: 'GENERAL', replacement: 'Tụy: Kích thước và cấu trúc echo bình thường, ống tụy không giãn.' },
  { trigger: 'bqlbt', category: 'GENERAL', replacement: 'Bàng quang: Thành mỏng, lòng bàng quang trống âm, không thấy sỏi hay u sùi.' },
  { trigger: 'tcbt', category: 'GYNECOLOGY', replacement: 'Tử cung: Tư thế ngả trước, kích thước bình thường, cấu trúc cơ tử cung đồng nhất. Nội mạc tử cung mỏng đều.' },
  { trigger: 'btbt', category: 'GYNECOLOGY', replacement: 'Hai buồng trứng: Kích thước bình thường, cấu trúc nang nhỏ ngoại vi sinh lý, không thấy khối u hay nang bất thường.' },
  { trigger: 'mnhbt', category: 'ENT', replacement: 'Màng nhĩ: Màng nhĩ hai bên nguyên vẹn, sáng bóng, nón sáng rõ nét, không đọng dịch hòm nhĩ.' },
  { trigger: 'dthbt', category: 'ENT', replacement: 'Dây thanh: Hai dây thanh âm trắng bóng, bờ thẳng, di động tốt và khép kín khi phát âm, không hạt xơ hay polyp.' },
  { trigger: 'tgbt', category: 'THYROID', replacement: 'Tuyến giáp: Hai thùy và eo giáp kích thước bình thường, nhu mô mịn đồng nhất, không thấy nốt khu trú.' }
];
