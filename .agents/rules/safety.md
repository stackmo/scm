# Safety & Security Rules

## 1. Credentials & Secrets
- **Tuyệt đối không hardcode** API keys, passwords, tokens, database credentials trong source code hoặc test files.
- Sử dụng biến môi trường (`process.env`) hoặc tệp `.env` (đã được thêm vào `.gitignore`).
- Cung cấp file mẫu `.env.example` với giá trị giả định.

## 2. Destructive Operations
- Không tự ý xóa thư mục, cơ sở dữ liệu hoặc ghi đè các tệp tài liệu quan trọng mà không có sự xác nhận rõ ràng.
- Sử dụng các lệnh an toàn, kiểm tra kỹ đường dẫn trước khi thao tác file (`rm`, `del`).

## 3. Input Validation & Sanitization
- Mọi dữ liệu đến từ người dùng hoặc nguồn bên ngoài phải được kiểm tra (validate) kiểu dữ liệu và giới hạn độ dài.
- Phòng tránh các lỗ hổng phổ biến (XSS, SQL Injection, Path Traversal).
