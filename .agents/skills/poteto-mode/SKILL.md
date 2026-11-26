---
name: poteto-mode
description: Activate Lauren Tan's autonomous engineering mode, orchestrating Playbook selection, task decomposition, isolated worker execution, and strict verification loops.
---

# SKILL: POTETO-MODE (LAUREN TAN ORCHESTRATION)

Kích hoạt chế độ vận hành kỹ thuật khắt khe theo phong cách Lauren Tan (@poteto). Dùng khi người dùng bắt đầu tính năng bằng `/poteto-mode` hoặc yêu cầu làm việc theo chuẩn SpaceX/Lauren Tan.

## 🎯 Khi Nào Kích Hoạt?
- Khi nhận mục tiêu phức tạp hoặc tính năng mới cần kỷ luật cao.
- Khi người dùng gõ `/poteto-mode [mục tiêu]` hoặc `/goal [mục tiêu]`.

## 🔄 Quy Trình 4 Pha Khép Kín

### 1. Phân Tích & Chọn Playbook (Planning)
- Đọc codebase hiện tại để xác định loại bài toán (Feature, Refactoring, Bug Fix, Performance).
- Áp dụng nguyên tắc **Codebase as Memory**: Đọc 3 file mẫu tương tự.
- Lập danh sách tác động tối thiểu: Giới hạn $\le 3$ files cần sửa.
- Xác định lệnh kiểm thử dự án (`npm test`, `.\scripts\verify.ps1`).

### 2. Dọn Dẹp Trước Khi Thêm (Subtract Before You Add)
- Kiểm tra các file mục tiêu để loại bỏ dead code, logic dư thừa trước khi viết code mới.
- Áp dụng **Laziness Protocol**: Giữ diff nhỏ nhất, tái sử dụng tối đa type và hàm tiện ích có sẵn.

### 3. Vòng Lặp Tự Kiểm Định Cục Bộ (Verification Loop)
- Mở Terminal để chạy lệnh test harness.
- **Nếu FAIL:** Đọc log lỗi chi tiết, tự động chẩn đoán nguyên nhân và sửa lại tại chỗ (tối đa 3 chu kỳ). Tuyệt đối không báo cáo hoàn thành khi test còn fail!
- **Nếu PASS:** Thu thập output log thực tế làm bằng chứng kiểm định.

### 4. Bàn Giao Kèm Bằng Chứng (Guardrails & Handoff)
- Tổng hợp Artifact bàn giao gồm:
  - Bản tóm tắt các file đã sửa.
  - Đoạn log test chạy thành công 100% từ Terminal.
  - Đề xuất người dùng review và phê duyệt.
