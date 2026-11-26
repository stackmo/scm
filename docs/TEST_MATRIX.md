# Test Matrix — Behavior Expectations & Validation Proof

> Cập nhật file này mỗi khi thêm feature mới hoặc thay đổi behavior.
> Mỗi row = 1 behavior expectation có test assertion tương ứng.

---

## Cách đọc

| Cột | Ý nghĩa |
|-----|---------|
| **Feature** | Tên tính năng / module |
| **Scenario** | Mô tả behavior theo Given-When-Then (ngắn) |
| **Test File** | File test chứa assertion |
| **Command** | Lệnh chạy để verify |
| **Status** | ✅ Pass / ❌ Fail / ⚠️ Pending |

---

## Matrix

| Feature | Scenario | Test File | Command | Status |
|---------|----------|-----------|---------|--------|
| Project Harness | Given project khởi tạo → AGENTS.md phải tồn tại | `tests/index.test.js` | `npm run verify` | ✅ |
| Project Harness | Given project khởi tạo → src/index.js export đúng | `tests/index.test.js` | `npm run verify` | ✅ |
| _(thêm feature mới ở đây)_ | | | | |

---

## Template thêm row mới

```markdown
| <FeatureName> | Given <context>, When <action>, Then <outcome> | tests/<file>.test.js | npm run verify | ⚠️ Pending |
```

---

## Quy tắc cập nhật

1. **Trước khi implement** feature normal/high-risk → thêm row với status `⚠️ Pending`
2. **Sau khi viết test** → status chuyển sang `❌ Fail` (Red phase)
3. **Sau khi implement + test pass** → status chuyển sang `✅ Pass` (Green phase)
4. **Không được xóa row** đã có trừ khi feature bị deprecated

---

## Lịch sử thay đổi

| Ngày | Thay đổi | Người thực hiện |
|------|---------|----------------|
| 2026-09-06 | Khởi tạo TEST_MATRIX với harness baseline | Codex Integration |
