# Universal Git & Commit Hygiene Specification (ai-spec)

Tài liệu quy định chuẩn mực Commit, quản lý nhánh và kỷ luật Git cho mọi AI Agent và Developer khi làm việc trên bất kỳ dự án nào tích hợp Universal AI Spec Kit.

---

## 1. Nguyên tắc Cốt lõi (Core Principles)

- **Verification-First**: Tuyệt đối **KHÔNG COMMIT** khi chưa chạy bước kiểm định tương ứng của dự án (xem chỉ dẫn trong `AGENTS.md` của dự án mục tiêu). Mọi commit phải đảm bảo build/test ở trạng thái PASS (0 error, xanh).
- **Atomic & Compilable Commits**:
  - Mỗi commit chỉ giải quyết **duy nhất một mục tiêu logic** (Single Concern).
  - **Luôn đảm bảo Compile/Build thành công**: Không bao giờ bẻ nhỏ commit dở dang làm gãy build của nhánh.
  - Ưu tiên $\le 3$ files mỗi lần nếu có thể, trừ khi cấu trúc kiến trúc (Clean Architecture, Multi-layer, Model nhị phân) yêu cầu gom đủ các file cấu thành concern đó.
- **Spec-Traceability linh hoạt theo loại Task**:
  - **Commit nghiệp vụ (`spec`, `feat`, `test`)**: Bắt buộc dẫn chiếu tới Spec ID (`SPEC-xxx`), Story (`STORY-xxx`), hoặc Feature ID (`FEAT-xxx`).
  - **Commit bảo trì / Tiny Patch (`docs`, `chore`, `harness`, `fix(tiny)`)**: Áp dụng theo `FEATURE_INTAKE.md`, không bắt buộc tạo spec giả tạo; chỉ cần mô tả rõ ràng lý do thay đổi.
- **No Secrets & Workspace Hygiene**:
  - Tuyệt đối không commit file chứa mật khẩu, credentials, API keys hoặc file build tạm thời (`bin/`, `obj/`, `node_modules/`, `deployment/`, `.log`).
  - Giữ git working tree sạch sẽ, không để lại file untracked hay file debug scratch bừa bãi trong repo mã nguồn.

---

## 2. Định dạng Commit Header & Body

Mọi commit message phải tuân thủ chuẩn Conventional Commits:

```text
<type>(<scope>): <mô tả ngắn gọn thể chủ động>

[Tùy chọn: Mô tả chi tiết lý do và hành vi thay đổi]

[Bắt buộc với commit nghiệp vụ, tùy chọn với chore/docs]:
Refs: FEAT-XXX, SPEC-[feature-name]
Task: docs/specs/[feature-name]/tasks.md#task-X.X
```

### Bảng Types Chuẩn:

| Type | Ý nghĩa & Khi nào dùng |
|:---|:---|
| `spec` | Thêm mới hoặc cập nhật BDD Spec, Acceptance Criteria, Gherkin Scenarios, Plan |
| `feat` | Thêm mới tính năng hoặc mở rộng chức năng nghiệp vụ |
| `test` | Thêm hoặc cập nhật unit tests, test assertions, mocks, kịch bản test |
| `fix` | Sửa lỗi logic, sửa bug, xử lý ngoại lệ |
| `refactor`| Tái cấu trúc code nhưng giữ nguyên hành vi nghiệp vụ |
| `harness` | Nâng cấp bộ công cụ ai-spec, verify pipeline, templates, rules |
| `docs` | Thêm hoặc sửa tài liệu, Architecture Decisions (DEC-XXX), Stories |
| `perf` | Tối ưu hóa hiệu năng, query DB, caching |
| `chore` | Cập nhật cấu hình phụ trợ, dependencies, dọn dẹp workspace |

---

## 3. Quy tắc Xác định Scope (Project-Local Scope Protocol)

`ai-spec` là bộ công cụ độc lập và dùng chung (Universal). Do đó:
- **Scope KHÔNG ĐƯỢC hardcode trong quy tắc dùng chung**, mà do từng dự án tự định nghĩa độc lập.
- **Bắt buộc tra cứu `AGENTS.md` của dự án mục tiêu**: Mỗi dự án khai báo danh mục Scope hợp lệ trong phần `## Project Scopes` hoặc `## Scopes` của riêng mình.
- Agent trước khi tạo commit **phải tra cứu danh mục Scope của chính dự án đó** để chọn đúng scope.
- **Phạm vi dùng chung mặc định**:
  - `root`: Áp dụng cho các thay đổi toàn cục repo hoặc xuyên suốt nhiều module.
  - `cli`, `verify`, `templates`, `rules`: Áp dụng khi chỉnh sửa bộ công cụ ai-spec.

---

## 4. Chu trình 5 Bước Spec-Driven Commit

1. **Bước 1 (Đặc tả)**: `spec(<scope>): define BDD scenarios for <feature>`
2. **Bước 2 (Test Red Phase)**: `test(<scope>): add Red phase test cases for <feature>`
3. **Bước 3 (Triển khai Green Phase)**: `feat(<scope>): implement business logic for <feature>`
4. **Bước 4 (Refactor & Polish)**: `refactor(<scope>): clean up implementation`
5. **Bước 5 (Verify & Complete)**: `chore(<scope>): verify and mark task completion`

*Lưu ý: Đối với task Tiny (sửa 1-2 file nhỏ, không đổi contract theo FEATURE_INTAKE), đi thẳng tới commit `fix(<scope>): ...` hoặc `chore(<scope>): ...` sau khi verify.*

---

## 5. Checklist trước khi Commit

- [ ] Đã chạy verification tương ứng của dự án (Pass 0 error).
- [ ] Header theo mẫu `<type>(<scope>): <mô tả>` với scope tra cứu từ `AGENTS.md` của dự án hiện tại.
- [ ] Giữ tính Atomic & Compilable (không làm gãy build của nhánh).
- [ ] Không có file thừa, file build rác (`bin/`, `obj/`, `deployment/`, `.log`).
- [ ] Không commit credentials, secrets hoặc file debug scratch.
- [ ] Đã liên kết `Refs: SPEC-...` nếu là commit tính năng nghiệp vụ.
