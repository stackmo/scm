using System.Globalization;
using System.Text;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Roles;

namespace SCM.Domain.Shared.Identity;

public class DoctorCapability
{
    private static readonly Dictionary<string, ClinicalSpecialtyType> SpecialtyAliasMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["internalmedicine"] = ClinicalSpecialtyType.InternalMedicine,
        ["noitongquat"] = ClinicalSpecialtyType.InternalMedicine,
        ["noikhoa"] = ClinicalSpecialtyType.InternalMedicine,
        ["noi"] = ClinicalSpecialtyType.InternalMedicine,

        ["surgery"] = ClinicalSpecialtyType.Surgery,
        ["ngoaitongquat"] = ClinicalSpecialtyType.Surgery,
        ["ngoaikhoa"] = ClinicalSpecialtyType.Surgery,
        ["ngoai"] = ClinicalSpecialtyType.Surgery,

        ["ophthalmology"] = ClinicalSpecialtyType.Ophthalmology,
        ["mat"] = ClinicalSpecialtyType.Ophthalmology,
        ["matnhankhoa"] = ClinicalSpecialtyType.Ophthalmology,
        ["nhankhoa"] = ClinicalSpecialtyType.Ophthalmology,

        ["otorhinolaryngology"] = ClinicalSpecialtyType.Otorhinolaryngology,
        ["taimuihong"] = ClinicalSpecialtyType.Otorhinolaryngology,
        ["tmh"] = ClinicalSpecialtyType.Otorhinolaryngology,
        ["ent"] = ClinicalSpecialtyType.Otorhinolaryngology,

        ["dentomaxillofacial"] = ClinicalSpecialtyType.Dentomaxillofacial,
        ["ranghammat"] = ClinicalSpecialtyType.Dentomaxillofacial,
        ["rhm"] = ClinicalSpecialtyType.Dentomaxillofacial,
        ["dental"] = ClinicalSpecialtyType.Dentomaxillofacial,

        ["dermatology"] = ClinicalSpecialtyType.Dermatology,
        ["dalieu"] = ClinicalSpecialtyType.Dermatology,

        ["obstetricsgynecology"] = ClinicalSpecialtyType.ObstetricsGynecology,
        ["sanphukhoa"] = ClinicalSpecialtyType.ObstetricsGynecology,
        ["san"] = ClinicalSpecialtyType.ObstetricsGynecology,
        ["obgyn"] = ClinicalSpecialtyType.ObstetricsGynecology,

        ["occupationalhealth"] = ClinicalSpecialtyType.OccupationalHealth,
        ["benhnghenghiep"] = ClinicalSpecialtyType.OccupationalHealth,
        ["nhikhoa"] = ClinicalSpecialtyType.OccupationalHealth,
        ["coxuongkhop"] = ClinicalSpecialtyType.OccupationalHealth
    };

    public Guid? UserId { get; init; }
    public Guid? StaffId { get; init; }
    public string StaffCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string RoleCode { get; init; } = SCMRoles.Doctor;
    public ClinicalActorGroup ActorGroup { get; init; } = ClinicalActorGroup.SpecialtyDoctor;
    public string PrimarySpecialty { get; init; } = string.Empty;
    public IReadOnlyList<string> SecondarySpecialties { get; init; } = Array.Empty<string>();
    public bool CanOrderCls { get; init; }
    public bool CanPrescribe { get; init; }
    public bool CanFinalizeHealthCheckup { get; init; }
    public bool CanViewFullClinicalHistory { get; init; } = true;
    public IReadOnlyList<string> AssignedRoomCodes { get; init; } = Array.Empty<string>();
    public bool HasValidLicense { get; init; }
    public string? LicenseNo { get; init; }
    public DateTime? LicenseExpiryDate { get; init; }

    public bool IsAdmin =>
        RoleCode.Equals(SCMRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase) ||
        RoleCode.Equals(SCMRoles.AdminChain, StringComparison.OrdinalIgnoreCase) ||
        RoleCode.Equals(SCMRoles.AdminBranch, StringComparison.OrdinalIgnoreCase);

    public bool CanExamineSpecialty(ClinicalSpecialtyType specialty)
    {
        if (ActorGroup == ClinicalActorGroup.ParaclinicalTechnician ||
            ActorGroup == ClinicalActorGroup.RadiologyDoctor ||
            ActorGroup == ClinicalActorGroup.LabDoctor)
        {
            return false;
        }

        if (ActorGroup == ClinicalActorGroup.InternalMedicineLead &&
            specialty == ClinicalSpecialtyType.InternalMedicine)
        {
            return true;
        }

        var primary = ParseSpecialty(PrimarySpecialty);
        if (primary == specialty) return true;

        return SecondarySpecialties.Any(s => ParseSpecialty(s) == specialty);
    }

    public bool CanAccessRoom(string roomCode)
    {
        if (IsAdmin) return true;
        if (string.IsNullOrWhiteSpace(roomCode)) return false;

        var normalized = roomCode.Trim();
        return AssignedRoomCodes.Any(r => r.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    public string GetDefaultClinicalTabKey()
    {
        return ActorGroup switch
        {
            ClinicalActorGroup.InternalMedicineLead => "tab_internal",
            ClinicalActorGroup.RadiologyDoctor => "tab_imaging_capture",
            ClinicalActorGroup.LabDoctor => "tab_lab_lis",
            ClinicalActorGroup.ParaclinicalTechnician => "tab_vital_signs",
            _ => ParseSpecialty(PrimarySpecialty) switch
            {
                ClinicalSpecialtyType.InternalMedicine => "tab_internal",
                ClinicalSpecialtyType.Surgery => "tab_surgery",
                ClinicalSpecialtyType.Ophthalmology => "tab_eye",
                ClinicalSpecialtyType.Otorhinolaryngology => "tab_ent",
                ClinicalSpecialtyType.Dentomaxillofacial => "tab_dental",
                ClinicalSpecialtyType.Dermatology => "tab_dermatology",
                ClinicalSpecialtyType.ObstetricsGynecology => "tab_obgyn",
                _ => "tab_history"
            }
        };
    }

    public static ClinicalSpecialtyType? ParseSpecialty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        if (Enum.TryParse<ClinicalSpecialtyType>(value.Trim(), true, out var parsed))
            return parsed;

        var normalized = NormalizeKey(value);
        return SpecialtyAliasMap.TryGetValue(normalized, out var mapped) ? mapped : null;
    }

    public static IReadOnlyList<string> ParseCsvList(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return Array.Empty<string>();
        return csv.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeKey(string input)
    {
        var decomposed = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            var mapped = ch switch
            {
                'đ' => 'd',
                'Đ' => 'd',
                _ => char.ToLowerInvariant(ch)
            };
            if (char.IsLetterOrDigit(mapped)) sb.Append(mapped);
        }
        return sb.ToString();
    }
}
