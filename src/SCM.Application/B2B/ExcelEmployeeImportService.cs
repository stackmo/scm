using System.Globalization;
using MiniExcelLibs;
using SCM.Application.Contracts.B2B;
using SCM.Application.Contracts.B2B.Dtos;
using SCM.Domain.Shared.Enums;

namespace SCM.Application.B2B;

public class ExcelEmployeeImportService : IExcelEmployeeImportService
{
    private readonly IEmployeeImportAppService _employeeImportAppService;

    public ExcelEmployeeImportService(IEmployeeImportAppService employeeImportAppService)
    {
        _employeeImportAppService = employeeImportAppService;
    }

    public async Task<EmployeeImportResultDto> ImportFromExcelStreamAsync(
        Stream excelStream,
        Guid companyId,
        Guid contractId,
        Guid packageId)
    {
        var rows = excelStream.Query(useHeaderRow: true);
        var employees = new List<EmployeeImportItemDto>();

        foreach (IDictionary<string, object> row in rows)
        {
            var fullName = GetValue(row, "Họ và tên", "Họ tên", "HoTen", "FullName", "Name");
            var cccd = GetValue(row, "Số CCCD", "CCCD", "CMND", "IdentificationNumber", "CccdNumber");
            var dobStr = GetValue(row, "Ngày sinh", "NgaySinh", "DateOfBirth", "DOB");
            var genderStr = GetValue(row, "Giới tính", "GioiTinh", "Gender");
            var department = GetValue(row, "Phòng ban", "PhongBan", "Department");
            var employeeCode = GetValue(row, "Mã NV", "Mã nhân viên", "MaNV", "EmployeeCode");
            var targetGroupName = GetValue(row, "Nhóm đối tượng", "NhomDoiTuong", "TargetGroupName", "Group");
            var phone = GetValue(row, "Số điện thoại", "SĐT", "SDT", "PhoneNumber", "Phone");
            var bhyt = GetValue(row, "Mã BHYT", "BHYT", "HealthInsuranceCode");
            var notes = GetValue(row, "Ghi chú", "GhiChu", "Notes");

            // Skip empty rows
            if (string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(cccd))
                continue;

            var dob = ParseDate(dobStr);
            var gender = ParseGender(genderStr);

            employees.Add(new EmployeeImportItemDto
            {
                FullName = fullName,
                IdentificationNumber = cccd,
                DateOfBirth = dob,
                Gender = gender,
                Department = department,
                EmployeeCode = employeeCode,
                TargetGroupName = targetGroupName,
                PhoneNumber = phone,
                HealthInsuranceCode = bhyt,
                Notes = notes
            });
        }

        var importPayload = new EmployeeImportDto
        {
            CompanyId = companyId,
            ContractId = contractId,
            PackageId = packageId,
            Employees = employees
        };

        return await _employeeImportAppService.ImportEmployeesAsync(importPayload);
    }

    private static string GetValue(IDictionary<string, object> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            // Case-insensitive lookup
            var match = row.FirstOrDefault(kv => string.Equals(kv.Key?.Trim(), key, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(match.Key) && match.Value != null)
            {
                return match.Value.ToString()?.Trim() ?? string.Empty;
            }
        }
        return string.Empty;
    }

    private static DateTime ParseDate(string dateStr)
    {
        if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        string[] formats = ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "yyyy/MM/dd", "MM/dd/yyyy"];
        if (DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return dt;

        if (double.TryParse(dateStr, out var oaDate))
        {
            try
            {
                return DateTime.FromOADate(oaDate);
            }
            catch
            {
                // ignore
            }
        }

        return DateTime.MinValue;
    }

    private static Gender ParseGender(string genderStr)
    {
        var normalized = genderStr.Trim().ToLowerInvariant();
        if (normalized == "nam" || normalized == "male" || normalized == "1")
            return Gender.Male;
        if (normalized == "nữ" || normalized == "nu" || normalized == "female" || normalized == "0" || normalized == "2")
            return Gender.Female;

        return Gender.Male;
    }
}
