# Story: [Tên tính năng]

> Copy template này vào `docs/stories/<slug>.md` cho mỗi normal/high-risk feature.

---

## Metadata

| Field | Value |
|-------|-------|
| **ID** | STORY-XXX |
| **Ngày tạo** | YYYY-MM-DD |
| **Loại** | normal / high-risk |
| **Status** | Draft / In Progress / Done / Blocked |
| **Người thực hiện** | Agent / User |

---

## Mô tả

_Mô tả ngắn gọn tính năng cần implement. 2-3 câu._

---

## Acceptance Criteria (Given-When-Then)

```
Scenario 1: [Tên scenario]
  Given [context ban đầu]
  When  [action xảy ra]
  Then  [kết quả mong đợi]

Scenario 2: [Edge case]
  Given [context]
  When  [action biên]
  Then  [kết quả biên]
```

---

## Files bị ảnh hưởng

- `src/...` — [lý do]
- `tests/...` — [lý do]
- _(liệt kê đầy đủ trước khi implement)_

---

## Contracts / Interfaces thay đổi

- _(nêu tên function/API/module nếu interface thay đổi)_
- N/A nếu không thay đổi

---

## Verification Proof

Lệnh verify cụ thể sau khi implement:

```bash
npm run verify
# Hoặc cụ thể hơn:
node --test tests/feature.test.js
```

Expected output:
```
✓ [scenario 1 description]
✓ [scenario 2 description]
```

---

## Notes / Risks

- _(ghi chú, risk, blockers nếu có)_

---

## Status Log

| Ngày | Status | Ghi chú |
|------|--------|---------|
| YYYY-MM-DD | Draft | Tạo story |
| YYYY-MM-DD | Done | Implement xong, test pass |
