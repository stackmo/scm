# Feature Intake — Phân loại Request trước khi Implementation

> Sử dụng file này để phân loại mọi request trước khi bắt tay code.
> Agent phải đọc và điền form này trước mọi implementation có ý nghĩa.

---

## Cách sử dụng

Trước khi implement, agent phải tự hỏi và điền vào checklist phân loại:

### Bước 1: Phân loại request

| Loại | Điều kiện | Hành động |
|------|-----------|-----------|
| **Tiny** | 1-2 files, không thay đổi contract, không ảnh hưởng logic chính | Patch trực tiếp, không cần plan |
| **Normal** | 3+ files hoặc thay đổi 1 contract/interface | Tạo story artifact + nêu verification proof |
| **High-risk** | Thay đổi data schema, auth, security, nhiều contract, refactor lớn | Tạo decision artifact + story + đợi approve |

### Bước 2: Điền checklist

```
[ ] Loại: tiny / normal / high-risk
[ ] Mô tả ngắn: ...
[ ] Files bị ảnh hưởng: ...
[ ] Contracts/interfaces bị thay đổi: ...
[ ] Verification proof (test cụ thể sẽ chạy): ...
[ ] Story artifact: docs/stories/<name>.md (nếu normal/high-risk)
[ ] Decision artifact: docs/decisions/<name>.md (nếu high-risk)
```

---

## Ví dụ phân loại

### Tiny (patch trực tiếp)
- Sửa typo trong README
- Thêm 1 comment vào hàm đã có
- Fix bug nhỏ không ảnh hưởng interface

### Normal (cần story)
- Thêm tính năng mới với API endpoint mới
- Refactor 1 module, giữ nguyên interface
- Thêm test coverage cho feature hiện có

### High-risk (cần decision + story + approve)
- Thay đổi database schema
- Thay đổi authentication flow
- Refactor kiến trúc chính
- Breaking change ở public API
- Thay đổi dependency quan trọng

---

## Contracts cần đặc biệt chú ý

Những file/interface sau luôn cần High-risk review nếu bị thay đổi:

- `AGENTS.md` — rules chính
- `package.json` (scripts, dependencies)
- `src/index.js` — entrypoint
- `scripts/verify.*` — harness

---

## Nguyên tắc

- Tiny work → patch ngay, không over-engineer
- Normal/High-risk → tạo artifact trước, implement sau
- Agent không tự quyết high-risk một mình → hỏi user
