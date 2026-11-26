# Design Contract: [Tên Module / Tính Năng]

> Copy template này vào `docs/designs/<slug>.md` trước khi viết code implementation cho bất kỳ tính năng phức tạp hoặc có giao diện người dùng.

---

## 1. Metadata

| Field | Value |
|-------|-------|
| **Story Reference** | STORY-XXX (nếu có) |
| **Ngày tạo** | YYYY-MM-DD |
| **Loại thiết kế** | Logic / API Contract / UI Component / Data Model |
| **Status** | Draft / Approved / Implemented |

---

## 2. Logic & Interface Contract (Bắt buộc cho Backend / Core Logic)

### 2.1. Module & Data Flow
- **Trách nhiệm module (Single Responsibility)**: _Mô tả ngắn gọn module làm gì và KHÔNG làm gì._
- **Data Flow**: `Input Data -> [Validation] -> [Domain Logic / State Machine] -> [Persistence / Side Effect] -> Output Data`

### 2.2. Type & Interface Definition
```typescript
// Định nghĩa kiểu dữ liệu chặt chẽ (Strict Types / Data Transfer Objects)
interface RequestPayload {
  // các trường bắt buộc & tùy chọn
}

interface ResponseResult {
  success: boolean;
  data?: unknown;
  error?: ErrorDetail;
}

// Bảng mã lỗi (Error Taxonomy) - Không dùng exception chung chung
type ErrorCode = 
  | 'INVALID_INPUT'
  | 'UNAUTHORIZED'
  | 'RESOURCE_NOT_FOUND'
  | 'STATE_CONFLICT';
```

### 2.3. State Machine & Invariants (Nếu có chuyển trạng thái)
- **Các trạng thái hợp lệ**: `IDLE -> PROCESSING -> COMPLETED | FAILED`
- **Bất biến (Invariants)**:
  - *Bất biến 1*: Không chuyển trạng thái nếu thiếu điều kiện X.
  - *Bất biến 2*: Kết quả với cùng một đầu vào phải là tất định (deterministic).

---

## 3. UI / UX Design Specification (Bắt buộc nếu có Frontend / Web)

> **Nguyên tắc**: Không tạo giao diện sơ sài (HTML 2005). Phải hiện đại, đồng bộ và có trạng thái tương tác hoàn chỉnh.

### 3.1. Design Tokens
- **Bảng màu (Palette)**:
  - Background: e.g., `#0f172a` (Slate 900) hoặc Dark Mode glassmorphism
  - Surface/Card: e.g., `#1e293b` với border `rgba(255,255,255,0.08)`
  - Primary Accent: e.g., `#38bdf8` (Sky 400) hoặc Brand Primary
  - Text: High-contrast `#f8fafc` (Primary), `#94a3b8` (Secondary/Muted)
- **Typography**: `Inter`, `Roboto`, hoặc `Outfit` (font weights: 400, 500, 600, 700).
- **Layout & Spacing**: Hệ thống lưới (Grid 8px: 8, 16, 24, 32px), bo góc `rounded-lg` / `rounded-xl`.

### 3.2. UI State Coverage (Đủ 4 trạng thái)
1. **Loading State**: Skeleton loading hoặc spinner tinh tế, không làm vỡ layout.
2. **Empty State**: Hình minh họa/icon + thông điệp hướng dẫn hành động tiếp theo.
3. **Error State**: Thông báo lỗi cụ thể, có nút "Thử lại" (Retry).
4. **Active / Populated State**: Hiển thị dữ liệu kèm hover effects, micro-animations mượt mà.

---

## 4. Edge Cases & Boundary Conditions

- [ ] Input là null, undefined, rỗng, hoặc vượt giới hạn số lượng?
- [ ] Network timeout / API chậm thì UI và logic phản ứng thế nào?
- [ ] Đụng độ dữ liệu đồng thời (Race condition / Concurrent access)?

---

## 5. Design Gate Review

- [ ] Interface / Schema đã được review rõ ràng trước khi code ruột hàm?
- [ ] BDD Spec đã bám sát các kịch bản trong Design này?
- [ ] Không có placeholder hoặc hardcoded values?
