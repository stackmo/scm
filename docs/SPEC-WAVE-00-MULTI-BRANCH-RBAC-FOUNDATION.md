# ĐẶC TẢ KIẾN TRÚC & PHÂN QUYỀN: MULTI-BRANCH & RBAC IDENTITY FOUNDATION (WAVE 0)

> **Mã đặc tả:** `SPEC-WAVE-00` | **Trạng thái:** `APPROVED & BASELINE`  
> **Cấp độ kiến trúc:** Nền móng tối thượng (Wave 0 Invariant)  
> **Dự án:** SmartClinic (SPK Clinic OS & SCM) | **Phạm vi Memora:** `project:smartclinic`  
> **Ngày phê duyệt:** 2026-11-24 | **Người phê duyệt:** CEO / Solution Architect  

---

## 1. BỐI CẢNH & VỊ TRÍ CHIẾN LƯỢC (WAVE 0 INVARIANT)

Trong hệ thống phòng khám đa chi nhánh (Multi-Branch Clinic Chain) và trạm làm việc y tế chuyên sâu:
- **Chi nhánh (Branch) và Phân quyền (RBAC)** KHÔNG PHẢI là một tính năng phụ trợ thêm vào sau, mà là **Ranh giới cô lập dữ liệu (Context Boundary)** bao bọc toàn bộ các nghiệp vụ y tế.
- Nếu không có định danh Chi nhánh và Phân quyền ngay từ Wave 0, toàn bộ các Wave tiếp theo (Tiếp đón, Bàn khám EMR, Kho dược, Viện phí) sẽ bị ô nhiễm dữ liệu chéo (Cross-tenant / Cross-branch data leakage), vi phạm tính pháp lý của Bộ Y Tế và đổ vỡ kiểm kê tài chính.
- Do đó, **Wave 0: Multi-Branch & RBAC Identity Foundation** được định vị là **NỀN MÓNG TIÊN QUYẾT BẮT BUỘC**, phải hoàn thiện và chốt chặn trước khi mở rộng các Wave sau.

---

## 2. KIẾN TRÚC ĐỊNH DANH 4 TẦNG (4-TIER IDENTITY HIERARCHY)

Mọi yêu cầu (Request) và phiên làm việc (Session) trong SmartClinic đều gắn với mô hình phân cấp:

- **Tầng 1 - Tenant / Nền tảng (Platform Level):** Quản lý khách hàng thuê SaaS, gói dịch vụ hệ thống.
- **Tầng 2 - Chuỗi Cơ sở / Tổng công ty (Clinic Chain Level):** Quản lý Master Data dùng chung (ICD-10, Danh mục hoạt chất Dược, Danh mục KSK mẫu M3, Khung giá dịch vụ cơ bản).
- **Tầng 3 - Chi nhánh / Phòng khám Vật lý (Branch Level - BẮT BUỘC KHI TÁC NGHIỆP):**
  - Giấy phép hoạt động (GPHĐ) riêng của Sở Y Tế / Bộ Y Tế.
  - Bác sĩ chịu trách nhiệm chuyên môn kỹ thuật riêng.
  - Kho dược vật lý riêng, két tiền thu ngân riêng.
  - Mã định danh liên thông Cổng Đơn thuốc Quốc gia / BHYT riêng.
- **Tầng 4 - Buồng khám / Ca trực / Người dùng (Workstation & Shift Level):**
  - Bác sĩ / Điều dưỡng / Lễ tân / Thu ngân làm việc tại buồng khám cụ thể trong ca trực.

---

## 3. MA TRẬN PHÂN QUYỀN RBAC (ROLE-BASED ACCESS CONTROL)

Hệ thống phân định 2 nhóm người dùng: Nhóm Quản trị (Central Governance) và Nhóm Tác nghiệp Cơ sở (Branch Operations).

### 3.1. Cấp Quản trị (Central Governance)

- **Super Admin (Quản trị viên Toàn Nền tảng SaaS):**
  - Quản trị hạ tầng kỹ thuật, cấu hình cơ sở dữ liệu và Tenant.
  - Bật/tắt Feature Flags theo gói hợp đồng.
  - Cấu hình tài khoản liên thông Cổng Quốc gia cấp hệ thống.
  - Toàn quyền xem Audit Log PII theo Nghị định 13/2023/NĐ-CP.
- **Clinic Chain Admin (Quản trị viên Chuỗi Phòng Khám):**
  - Sở hữu và ban hành Master Data toàn chuỗi: Danh mục kỹ thuật, giá dịch vụ, danh mục thuốc.
  - Quản lý danh bạ nhân sự, cấp chứng chỉ hành nghề (CCHN), phân bổ bác sĩ về các cơ sở.
  - Xem báo cáo tài chính tổng hợp đa chi nhánh thời gian thực.
  - Tạo lệnh điều chuyển dược phẩm liên chi nhánh.
  - Có quyền chọn ngữ cảnh `ALL` (Toàn chuỗi) hoặc chuyển đổi nhanh qua **Branch Switcher** trên thanh Header.

### 3.2. Cấp Tác nghiệp Cơ sở (Branch Operations - Bắt buộc gắn BranchContext)

- **Branch Admin (Quản trị viên Chi nhánh / Giám đốc Cơ sở):**
  - Quản lý lịch trực, phân bổ buồng khám vật lý tại cơ sở mình phụ trách.
  - Giám sát độ bão hòa hàng đợi tại sảnh và điều phối bác sĩ khám tăng cường.
  - Phê duyệt hủy phiếu thu/hóa đơn trong ca trực tại cơ sở.
- **Doctor (Bác sĩ Chuyên khoa):**
  - Toàn quyền tại buồng khám được phân công: Thăm khám, chỉ định CLS, kê đơn thuốc, chẩn đoán ICD-10.
  - Ký số hồ sơ bệnh án với mã CCHN cá nhân gắn với GPHĐ của chi nhánh.
  - Chỉ xem danh sách bệnh nhân và lịch hẹn thuộc chi nhánh đang trực.
- **Receptionist (Lễ tân Tiếp đón):**
  - Tiếp đón bệnh nhân, đăng ký thông tin hành chính, cấp số thứ tự (STT) hàng đợi.
  - Điều khiển gọi loa TTS phòng chờ chỉ trong phạm vi mạng LAN của chi nhánh.
- **Cashier (Thu ngân Viện phí):**
  - Thu tiền viện phí, in biên lai, phát hành mã VietQR động theo tài khoản ngân hàng của chi nhánh.
  - Chốt ca tiền mặt cuối ngày nộp về két cơ sở.
- **Pharmacist (Dược sĩ Nhà thuốc Cơ sở):**
  - Duyệt đơn thuốc ngoại trú, cấp phát thuốc thực tế, trừ tồn kho tại tủ thuốc của chi nhánh.
  - Kiểm kê kho dược chi nhánh và đối soát với ERP Odoo.

---

## 4. MA TRẬN PHỤ THUỘC & BÁN KÍNH ẢNH HƯỞNG (BLAST RADIUS & WAVE DEPENDENCY)

Việc chốt chặn Wave 0 tác động trực tiếp và bắt buộc các Wave kế tiếp phải tuân thủ các Invariants sau:

- **Ảnh hưởng đến Wave 1 (Tiếp đón, Hàng đợi & Lấy mẫu - WF 03/03A):**
  - Entity `QueueTicket` và `PhlebotomyOrder` bắt buộc có thuộc tính `BranchId` và `BranchCode`.
  - Loa thông báo TTS và màn hình TV Kiosk chỉ lắng nghe WebSocket / SignalR channel của `branch_{branchCode}`.
  - Nghiêm cấm hiển thị bệnh nhân của chi nhánh khác vào hàng đợi của chi nhánh hiện tại.
- **Ảnh hưởng đến Wave 2 (Lâm sàng EMR, Buồng khám & Gói Khám B2B):**
  - Hồ sơ bệnh án (`MedicalRecord`) bắt buộc lưu vết `BranchId`, `DoctorId`, `LicenseNo` tại thời điểm khám.
  - Gói khám B2B có thể áp dụng chính sách giá theo từng chi nhánh (Location-based Pricing).
  - Đơn thuốc xuất ra cổng Dược Quốc gia phải kèm Mã cơ sở KBCB của chi nhánh.
- **Ảnh hưởng đến Wave 3 (Kho Dược, Nhà thuốc & Đồng bộ Outbox Odoo):**
  - Tồn kho dược phẩm quản lý theo cấp độ 2 tầng: `Warehouse (Branch)` -> `Location (Tủ thuốc / Quầy bán)`.
  - Lệnh chuyển kho nội bộ (`InterBranchTransfer`) phải có xác nhận 2 đầu (Chi nhánh xuất - Chi nhánh nhập).
  - `SyncOutbox` gửi sang Odoo kèm `company_id` tương ứng với chi nhánh.
- **Ảnh hưởng đến Wave 4 (Thu ngân, Viện phí, VietQR & FAST ERP):**
  - Mỗi chi nhánh cấu hình một tài khoản thụ hưởng VietQR riêng hoặc tiểu khoản định danh.
  - Hóa đơn điện tử xuất qua FAST ERP sử dụng dải ký hiệu hóa đơn và mẫu số đăng ký thuế của chi nhánh tương ứng.

---

## 5. TIÊU CHÍ NGHIỆM THU KIẾN TRÚC (ACCEPTANCE CRITERIA)

- **`AC-0.1` (Interactive Branch Switcher):** Trên Top Navbar luôn hiển thị chi nhánh hiện tại. Tài khoản có quyền quản trị chuỗi có thể mở dropdown chuyển đổi giữa các chi nhánh hoặc chọn `ALL` (Toàn chuỗi) trong 1 cú click; toast thông báo lập tức phản hồi.
- **`AC-0.2` (Role Simulation & Display):** Header hiển thị avatar, họ tên và vai trò hiện tại. Hỗ trợ dropdown giả lập vai trò (Role Switcher) phục vụ kiểm thử và phân quyền nhanh.
- **`AC-0.3` (Mandatory Branch at Login):** Mọi tài khoản nhân sự cơ sở khi đăng nhập qua `Login.tsx` bắt buộc phải chọn chi nhánh trước khi vào hệ thống; hệ thống lưu trữ `selectedBranch` trong `AuthContext` và `localStorage`.
- **`AC-0.4` (Context Invariant for API):** Toàn bộ API gọi từ Frontend xuống Backend phải tự động đính kèm Header `X-Branch-Code` hoặc Claim trong Bearer Token.
- **`AC-0.5` (Zero Cross-Branch Leakage):** Truy vấn dữ liệu tác nghiệp (bệnh nhân chờ, đơn thuốc, tồn kho) tại chi nhánh A tuyệt đối không được lẫn dữ liệu của chi nhánh B.
- **`AC-0.6` (Immutable Master Data Guard):** Chỉ tài khoản có vai trò `super_admin` hoặc `admin_chain` mới có quyền thêm/sửa/xóa Master Data dùng chung.
