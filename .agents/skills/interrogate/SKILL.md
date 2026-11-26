---
name: interrogate
description: Interrogate and challenge vague requirements, uncover hidden assumptions, map out edge cases, and stress-test technical decisions before writing code.
---

# SKILL: INTERROGATE (PHẢN BIỆN YÊU CẦU & THÁCH THỨC GIẢ ĐỊNH)

Kỹ năng chất vấn chuyên sâu theo chuẩn pstack. Dùng khi người dùng đưa ra yêu cầu còn mơ hồ, tính năng có rủi ro kiến trúc cao, hoặc khi được yêu cầu `/interrogate`.

## 🎯 Mục Tiêu
Triệt tiêu rủi ro hiểu sai đề bài, phát hiện các trường hợp biên (edge cases) và ngăn chặn việc viết code sai hướng ngay từ vạch xuất phát.

## 🔍 Khung Chất Vấn 4 Trọng Điểm

1. **Bản Chất Nghiệp Vụ (The "Why"):**
   - Vấn đề cốt lõi mà người dùng muốn giải quyết là gì?
   - Có thể giải quyết vấn đề này mà không cần viết thêm tính năng mới hay không?

2. **Ranh Giới Hợp Đồng (Contract Boundaries):**
   - Input nhận vào là gì? Output mong đợi có cấu trúc ra sao?
   - Xử lý thế nào với các giá trị biên: `null`, `undefined`, chuỗi rỗng `""`, số âm, dữ liệu cực lớn?

3. **Tác Dụng Phụ (Side Effects & Repercussions):**
   - Thay đổi này có ảnh hưởng tới các module khác không?
   - Có làm phá vỡ database schema, authentication flow hay transaction boundary không?

4. **Kế Hoạch Kiểm Thử (Verification Plan):**
   - Làm thế nào để biết tính năng đã hoạt động đúng 100%?
   - Kịch bản kiểm thử cụ thể (Given - When - Then) là gì?

## 💬 Phong Cách Tương Tác
- Đặt tối đa $\le 3 - 5$ câu hỏi sắc bén, đi thẳng vào trọng tâm.
- Đề xuất các lựa chọn kèm phân tích ưu/nhược điểm thay vì chỉ hỏi chung chung.
