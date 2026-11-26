using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SCM.Application.Contracts.Identity;
using SCM.Domain.Branches;
using SCM.Domain.Clinical.Exceptions;
using SCM.Domain.Identity;
using SCM.Domain.MasterData;
using SCM.Domain.MasterData.Gateways;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Identity;
using SCM.Domain.Shared.Roles;
using SCM.EntityFrameworkCore;
using SCM.EntityFrameworkCore.Seeding;

namespace SCM.Application.Identity;

public class RbacPermissionService : IRbacPermissionService
{
    private readonly SCMDbContext _db;
    private readonly ICurrentBranch _currentBranch;
    private readonly ICurrentDoctorContext _currentDoctor;
    private readonly IMemoryCache _cache;

    public RbacPermissionService(
        SCMDbContext db,
        ICurrentBranch currentBranch,
        ICurrentDoctorContext currentDoctor,
        IMemoryCache cache)
    {
        _db = db;
        _currentBranch = currentBranch;
        _currentDoctor = currentDoctor;
        _cache = cache;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto input)
    {
        if (string.IsNullOrWhiteSpace(input.UserName) || string.IsNullOrWhiteSpace(input.Password))
        {
            return new LoginResponseDto
            {
                Success = false,
                Message = "Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu."
            };
        }

        var normalizedUser = input.UserName.Trim();
        var user = await _db.UserAccounts
            .FirstOrDefaultAsync(u => u.UserName.ToLower() == normalizedUser.ToLower() && u.IsActive);

        if (user == null)
        {
            return new LoginResponseDto { Success = false, Message = "Tài khoản không tồn tại hoặc đã bị khóa." };
        }

        var expectedHash = RbacAndDictionarySeeder.HashPassword(input.Password);
        if (!string.IsNullOrWhiteSpace(user.PasswordHash) &&
            !string.Equals(user.PasswordHash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            return new LoginResponseDto { Success = false, Message = "Mật khẩu không chính xác." };
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var branch = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == user.BranchId);
        var branchCode = branch?.Code ?? user.BranchCode;
        _currentBranch.Change(user.BranchId, branchCode, false, user.Role);

        await HydrateCurrentContextAsync(user.Id, user.UserName, _currentBranch, _currentDoctor);
        var ctx = await BuildContextDtoAsync(user, branchCode);

        return new LoginResponseDto
        {
            Success = true,
            Message = "Đăng nhập thành công.",
            Context = ctx
        };
    }

    public async Task<CurrentDoctorContextDto> GetMyContextAsync()
    {
        if (!_currentDoctor.IsAuthenticated || !_currentDoctor.UserId.HasValue)
        {
            return new CurrentDoctorContextDto
            {
                IsAuthenticated = false,
                BranchCode = _currentBranch.Code ?? "CNQ1"
            };
        }

        var user = await _db.UserAccounts.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentDoctor.UserId.Value && u.IsActive);

        if (user == null)
        {
            return new CurrentDoctorContextDto { IsAuthenticated = false };
        }

        var branchCode = _currentBranch.Code ?? user.BranchCode;
        return await BuildContextDtoAsync(user, branchCode);
    }

    public async Task<List<RolePermissionRuleDto>> GetMatrixAsync(Guid? branchId = null)
    {
        var effectiveBranchId = branchId ?? _currentBranch.Id;
        var rules = await _db.RolePermissionRules.AsNoTracking()
            .Where(r => r.BranchId == null || (effectiveBranchId.HasValue && r.BranchId == effectiveBranchId.Value))
            .OrderBy(r => r.ActorGroup)
            .ThenBy(r => r.ModuleKey)
            .ThenBy(r => r.TabKey)
            .ToListAsync();

        // Merge global rules with branch overrides cleanly per (RoleCode, ActorGroup, TabKey)
        var merged = new Dictionary<(string, ClinicalActorGroup, string), RolePermissionRule>();
        foreach (var r in rules.Where(x => x.BranchId == null))
            merged[(r.RoleCode, r.ActorGroup, r.TabKey)] = r;
        foreach (var r in rules.Where(x => x.BranchId != null))
            merged[(r.RoleCode, r.ActorGroup, r.TabKey)] = r;

        return merged.Values
            .OrderBy(r => r.ActorGroup)
            .ThenBy(r => r.TabKey)
            .Select(MapRuleDto)
            .ToList();
    }

    public async Task<List<RolePermissionRuleDto>> UpdateRoleMatrixAsync(UpdateRbacMatrixDto input)
    {
        EnsureAdminAuthorized("Cập nhật Ma trận Phân quyền RBAC");

        var targetBranchId = input.BranchId;
        long nextVersion = (await _db.RolePermissionRules.MaxAsync(r => (long?)r.PermissionVersion) ?? 1) + 1;

        foreach (var dto in input.Rules)
        {
            var existing = await _db.RolePermissionRules.FirstOrDefaultAsync(r =>
                r.BranchId == targetBranchId &&
                r.RoleCode == dto.RoleCode &&
                r.ActorGroup == dto.ActorGroup &&
                r.TabKey == dto.TabKey);

            if (existing == null)
            {
                existing = new RolePermissionRule
                {
                    BranchId = targetBranchId,
                    RoleCode = dto.RoleCode,
                    ActorGroup = dto.ActorGroup,
                    ModuleKey = dto.ModuleKey,
                    TabKey = dto.TabKey
                };
                _db.RolePermissionRules.Add(existing);
            }

            existing.ModuleKey = dto.ModuleKey;
            existing.CanView = dto.CanView;
            existing.CanCreate = dto.CanCreate;
            existing.CanEdit = dto.CanEdit;
            existing.CanDelete = dto.CanDelete;
            existing.CanSign = dto.CanSign;
            existing.CanPrint = dto.CanPrint;
            existing.CanFinalize = dto.CanFinalize;
            existing.PermissionVersion = nextVersion;
            existing.UpdatedByUserId = _currentDoctor.UserId;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        _db.AuditLogPIIs.Add(new AuditLogPII(
            _currentDoctor.UserId,
            _currentDoctor.UserName,
            _currentBranch.Id,
            _currentBranch.Code ?? "CNQ1",
            "UPDATE_RBAC_MATRIX",
            nameof(RolePermissionRule),
            targetBranchId?.ToString() ?? "GLOBAL",
            $"Cập nhật {input.Rules.Count} rule phân quyền (version {nextVersion})"));

        await _db.SaveChangesAsync();
        InvalidatePermissionCache();

        return await GetMatrixAsync(targetBranchId);
    }

    public async Task<List<StaffCapabilityDto>> GetStaffCapabilitiesAsync()
    {
        var staffs = await _db.Staffs.AsNoTracking().OrderBy(s => s.StaffCode).ToListAsync();
        var users = await _db.UserAccounts.AsNoTracking().ToListAsync();

        var userByStaffId = users.Where(u => u.StaffId.HasValue)
            .GroupBy(u => u.StaffId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        return staffs.Select(s =>
        {
            userByStaffId.TryGetValue(s.Id, out var u);
            var group = InferActorGroup(u, s);
            return new StaffCapabilityDto
            {
                StaffId = s.Id,
                UserId = u?.Id ?? s.UserId,
                UserName = u?.UserName ?? string.Empty,
                StaffCode = s.StaffCode,
                FullName = s.FullName,
                RoleCode = u?.Role ?? MapStaffRoleToRoleCode(s.StaffRole),
                ActorGroup = group,
                PrimarySpecialty = s.PrimarySpecialty ?? s.Specialty ?? string.Empty,
                SecondarySpecialties = s.SecondarySpecialties ?? string.Empty,
                PracticeCertificateCode = s.PracticeCertificateCode,
                CanOrderCls = s.CanOrderCls,
                CanPrescribe = s.CanPrescribe,
                CanFinalizeHealthCheckup = s.CanFinalizeHealthCheckup,
                CanViewFullClinicalHistory = s.CanViewFullClinicalHistory,
                AssignedRoomIds = s.AssignedRoomIds ?? string.Empty,
                IsActive = s.IsActive
            };
        }).ToList();
    }

    public async Task<StaffCapabilityDto> UpdateStaffCapabilityAsync(Guid staffId, UpdateStaffCapabilityDto input)
    {
        EnsureAdminAuthorized("Phân công Chuyên khoa & Phòng khám cho Bác sĩ");

        var staff = await _db.Staffs.FirstOrDefaultAsync(s => s.Id == staffId)
            ?? throw new KeyNotFoundException($"Không tìm thấy nhân sự StaffId={staffId}");

        staff.ClinicalActorGroup = input.ActorGroup;
        staff.PrimarySpecialty = input.PrimarySpecialty?.Trim();
        staff.Specialty = staff.PrimarySpecialty;
        staff.SecondarySpecialties = input.SecondarySpecialties?.Trim();
        staff.CanOrderCls = input.CanOrderCls;
        staff.CanPrescribe = input.CanPrescribe;
        staff.CanFinalizeHealthCheckup = input.CanFinalizeHealthCheckup;
        staff.CanViewFullClinicalHistory = input.CanViewFullClinicalHistory;
        staff.AssignedRoomIds = input.AssignedRoomIds?.Trim();

        var linkedUser = await _db.UserAccounts.FirstOrDefaultAsync(u => u.StaffId == staffId);
        if (linkedUser != null)
        {
            linkedUser.ActorGroup = input.ActorGroup;
        }

        _db.AuditLogPIIs.Add(new AuditLogPII(
            _currentDoctor.UserId,
            _currentDoctor.UserName,
            _currentBranch.Id,
            _currentBranch.Code ?? "CNQ1",
            "UPDATE_STAFF_CAPABILITY",
            nameof(Staff),
            staff.Id.ToString(),
            $"Staff={staff.StaffCode}; Group={input.ActorGroup}; Rooms={staff.AssignedRoomIds}; Finalize={input.CanFinalizeHealthCheckup}"));

        await _db.SaveChangesAsync();
        InvalidatePermissionCache();

        return new StaffCapabilityDto
        {
            StaffId = staff.Id,
            UserId = linkedUser?.Id ?? staff.UserId,
            UserName = linkedUser?.UserName ?? string.Empty,
            StaffCode = staff.StaffCode,
            FullName = staff.FullName,
            RoleCode = linkedUser?.Role ?? MapStaffRoleToRoleCode(staff.StaffRole),
            ActorGroup = input.ActorGroup,
            PrimarySpecialty = staff.PrimarySpecialty ?? string.Empty,
            SecondarySpecialties = staff.SecondarySpecialties ?? string.Empty,
            PracticeCertificateCode = staff.PracticeCertificateCode,
            CanOrderCls = staff.CanOrderCls,
            CanPrescribe = staff.CanPrescribe,
            CanFinalizeHealthCheckup = staff.CanFinalizeHealthCheckup,
            CanViewFullClinicalHistory = staff.CanViewFullClinicalHistory,
            AssignedRoomIds = staff.AssignedRoomIds ?? string.Empty,
            IsActive = staff.IsActive
        };
    }

    public async Task<IReadOnlyDictionary<string, EffectiveTabPermissionDto>> GetEffectivePermissionsAsync(
        Guid? userId,
        string roleCode,
        ClinicalActorGroup actorGroup,
        Guid? branchId)
    {
        var cacheKey = $"rbac:perm:{branchId}:{roleCode}:{(int)actorGroup}:{userId}";
        if (_cache.TryGetValue(cacheKey, out Dictionary<string, EffectiveTabPermissionDto>? cached) && cached != null)
        {
            return cached;
        }

        var rules = await _db.RolePermissionRules.AsNoTracking()
            .Where(r => r.RoleCode == roleCode &&
                        r.ActorGroup == actorGroup &&
                        (r.BranchId == null || (branchId.HasValue && r.BranchId == branchId.Value)))
            .ToListAsync();

        var result = new Dictionary<string, EffectiveTabPermissionDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in rules.OrderBy(x => x.BranchId.HasValue ? 1 : 0))
        {
            result[r.TabKey] = new EffectiveTabPermissionDto
            {
                ModuleKey = r.ModuleKey,
                TabKey = r.TabKey,
                CanView = r.CanView,
                CanCreate = r.CanCreate,
                CanEdit = r.CanEdit,
                CanDelete = r.CanDelete,
                CanSign = r.CanSign,
                CanPrint = r.CanPrint,
                CanFinalize = r.CanFinalize
            };
        }

        if (userId.HasValue)
        {
            var overrides = await _db.UserPermissionOverrides.AsNoTracking()
                .Where(o => o.UserId == userId.Value && (o.BranchId == null || (branchId.HasValue && o.BranchId == branchId.Value)))
                .OrderBy(o => o.BranchId.HasValue ? 1 : 0)
                .ToListAsync();

            foreach (var ov in overrides)
            {
                if (!result.TryGetValue(ov.TabKey, out var perm))
                {
                    perm = new EffectiveTabPermissionDto { ModuleKey = "clinical_group", TabKey = ov.TabKey };
                    result[ov.TabKey] = perm;
                }
                if (ov.CanView.HasValue) perm.CanView = ov.CanView.Value;
                if (ov.CanCreate.HasValue) perm.CanCreate = ov.CanCreate.Value;
                if (ov.CanEdit.HasValue) perm.CanEdit = ov.CanEdit.Value;
                if (ov.CanDelete.HasValue) perm.CanDelete = ov.CanDelete.Value;
                if (ov.CanSign.HasValue) perm.CanSign = ov.CanSign.Value;
                if (ov.CanPrint.HasValue) perm.CanPrint = ov.CanPrint.Value;
                if (ov.CanFinalize.HasValue) perm.CanFinalize = ov.CanFinalize.Value;
            }
        }

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
        return result;
    }

    public async Task HydrateCurrentContextAsync(
        Guid? userId,
        string? userName,
        ICurrentBranch currentBranch,
        ICurrentDoctorContext currentDoctor)
    {
        UserAccount? user = null;
        if (userId.HasValue)
        {
            user = await _db.UserAccounts.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value && u.IsActive);
        }
        else if (!string.IsNullOrWhiteSpace(userName))
        {
            var uname = userName.Trim().ToLower();
            user = await _db.UserAccounts.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName.ToLower() == uname && u.IsActive);
        }

        if (user == null) return;

        var branchId = currentBranch.Id ?? user.BranchId;
        var branchCode = currentBranch.Code ?? user.BranchCode;
        currentBranch.Change(branchId, branchCode, currentBranch.IsAllBranches, user.Role);

        var (capability, actorGroup) = await ResolveDoctorCapabilityAsync(user);
        var perms = await GetEffectivePermissionsAsync(user.Id, user.Role, actorGroup, branchId);
        var version = await _db.RolePermissionRules.MaxAsync(r => (long?)r.PermissionVersion) ?? 1;

        currentDoctor.SetContext(user.Id, user.UserName, user.FullName, user.Role, actorGroup, capability, perms, version);
    }

    private async Task<(DoctorCapability? Capability, ClinicalActorGroup ActorGroup)> ResolveDoctorCapabilityAsync(UserAccount user)
    {
        Staff? staff = null;
        if (user.StaffId.HasValue)
        {
            staff = await _db.Staffs.AsNoTracking().FirstOrDefaultAsync(s => s.Id == user.StaffId.Value);
        }

        var actorGroup = InferActorGroup(user, staff);
        if (staff == null)
        {
            return (new DoctorCapability
            {
                UserId = user.Id,
                FullName = user.FullName,
                RoleCode = user.Role,
                ActorGroup = actorGroup,
                CanViewFullClinicalHistory = true
            }, actorGroup);
        }

        var license = await _db.PractitionerLicenses.AsNoTracking()
            .FirstOrDefaultAsync(l => l.StaffId == staff.Id && l.IsActive);

        var hasValidLicense = license != null || !string.IsNullOrWhiteSpace(staff.PracticeCertificateCode);
        if (staff.LicenseExpiryDate.HasValue && staff.LicenseExpiryDate.Value < DateTime.UtcNow)
        {
            hasValidLicense = false;
        }

        var capability = new DoctorCapability
        {
            UserId = user.Id,
            StaffId = staff.Id,
            StaffCode = staff.StaffCode,
            FullName = staff.FullName,
            RoleCode = user.Role,
            ActorGroup = actorGroup,
            PrimarySpecialty = staff.PrimarySpecialty ?? staff.Specialty ?? string.Empty,
            SecondarySpecialties = DoctorCapability.ParseCsvList(staff.SecondarySpecialties),
            CanOrderCls = staff.CanOrderCls,
            CanPrescribe = staff.CanPrescribe,
            CanFinalizeHealthCheckup = staff.CanFinalizeHealthCheckup,
            CanViewFullClinicalHistory = staff.CanViewFullClinicalHistory,
            AssignedRoomCodes = DoctorCapability.ParseCsvList(staff.AssignedRoomIds),
            HasValidLicense = hasValidLicense,
            LicenseNo = license?.LicenseNumber ?? staff.PracticeCertificateCode,
            LicenseExpiryDate = staff.LicenseExpiryDate
        };

        return (capability, actorGroup);
    }

    private async Task<CurrentDoctorContextDto> BuildContextDtoAsync(UserAccount user, string branchCode)
    {
        var (capability, actorGroup) = await ResolveDoctorCapabilityAsync(user);
        var branchId = _currentBranch.Id ?? user.BranchId;
        var perms = await GetEffectivePermissionsAsync(user.Id, user.Role, actorGroup, branchId);
        var version = await _db.RolePermissionRules.MaxAsync(r => (long?)r.PermissionVersion) ?? 1;

        var assignedRooms = new List<AssignedRoomDto>();
        var roomLoadLookup = await _db.ClinicRoomLoads.AsNoTracking()
            .ToDictionaryAsync(r => r.RoomCode.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);

        if (capability != null && capability.AssignedRoomCodes.Count > 0)
        {
            var upperCodes = capability.AssignedRoomCodes.Select(c => c.ToUpperInvariant()).ToList();
            var masterRooms = await _db.ClinicRooms.AsNoTracking()
                .Where(r => upperCodes.Contains(r.RoomCode.ToUpper()))
                .ToDictionaryAsync(r => r.RoomCode.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);

            foreach (var rawCode in capability.AssignedRoomCodes)
            {
                var key = rawCode.Trim().ToUpperInvariant();
                if (roomLoadLookup.TryGetValue(key, out var rl))
                {
                    assignedRooms.Add(new AssignedRoomDto { RoomCode = rl.RoomCode, RoomName = rl.RoomName, RoomType = rl.RoomType });
                }
                else if (masterRooms.TryGetValue(key, out var cr))
                {
                    assignedRooms.Add(new AssignedRoomDto { RoomCode = cr.RoomCode, RoomName = cr.RoomName, RoomType = cr.RoomType });
                }
                else
                {
                    assignedRooms.Add(new AssignedRoomDto { RoomCode = key, RoomName = key, RoomType = RoomType.GeneralClinical });
                }
            }
        }
        else if (capability?.IsAdmin == true)
        {
            assignedRooms = roomLoadLookup.Values
                .OrderBy(r => r.RoomCode)
                .Select(r => new AssignedRoomDto
                {
                    RoomCode = r.RoomCode,
                    RoomName = r.RoomName,
                    RoomType = r.RoomType
                })
                .ToList();
        }

        return new CurrentDoctorContextDto
        {
            IsAuthenticated = true,
            UserId = user.Id,
            UserName = user.UserName,
            FullName = capability?.FullName ?? user.FullName,
            RoleCode = user.Role,
            ActorGroup = actorGroup,
            StaffId = capability?.StaffId,
            StaffCode = capability?.StaffCode ?? string.Empty,
            BranchCode = branchCode,
            PrimarySpecialty = capability?.PrimarySpecialty ?? string.Empty,
            SecondarySpecialties = capability?.SecondarySpecialties.ToList() ?? new List<string>(),
            CanOrderCls = capability?.CanOrderCls ?? false,
            CanPrescribe = capability?.CanPrescribe ?? false,
            CanFinalizeHealthCheckup = capability?.CanFinalizeHealthCheckup ?? false,
            CanViewFullClinicalHistory = capability?.CanViewFullClinicalHistory ?? true,
            HasValidLicense = capability?.HasValidLicense ?? false,
            LicenseNo = capability?.LicenseNo,
            DefaultClinicalTabKey = capability?.GetDefaultClinicalTabKey() ?? "tab_history",
            AssignedRooms = assignedRooms,
            EffectivePermissions = perms.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase),
            PermissionVersion = version
        };
    }

    private void EnsureAdminAuthorized(string actionName)
    {
        if (!_currentDoctor.IsAuthenticated)
        {
            throw new ClinicalAccessDeniedException(
                SCMErrorCodes.ClinicalAccessDenied,
                $"Vui lòng đăng nhập tài khoản quản trị để thực hiện: {actionName}.",
                null,
                PermissionAction.Edit.ToString(),
                "tab_admin_acl");
        }

        if (_currentDoctor.Capability?.IsAdmin == true || _currentDoctor.HasPermission("tab_admin_acl", PermissionAction.Edit))
        {
            return;
        }

        throw new ClinicalAccessDeniedException(
            SCMErrorCodes.ClinicalAccessDenied,
            $"Tài khoản [{_currentDoctor.UserName}] không có quyền quản trị phân quyền ({actionName}).",
            _currentDoctor.StaffId,
            "tab_admin_acl:Edit",
            nameof(RolePermissionRule));
    }

    private static ClinicalActorGroup InferActorGroup(UserAccount? user, Staff? staff)
    {
        if (staff?.ClinicalActorGroup.HasValue == true) return staff.ClinicalActorGroup.Value;
        if (user != null) return user.ActorGroup;
        if (staff?.StaffRole == StaffRole.Nurse || staff?.StaffRole == StaffRole.Technician)
            return ClinicalActorGroup.ParaclinicalTechnician;
        if (staff?.StaffRole == StaffRole.Administrator)
            return ClinicalActorGroup.ClinicalAdmin;
        return ClinicalActorGroup.SpecialtyDoctor;
    }

    private static string MapStaffRoleToRoleCode(StaffRole role) => role switch
    {
        StaffRole.Administrator => SCMRoles.AdminBranch,
        StaffRole.Nurse or StaffRole.Technician => SCMRoles.NurseTech,
        StaffRole.Pharmacist => SCMRoles.Pharmacist,
        StaffRole.Receptionist => SCMRoles.Receptionist,
        _ => SCMRoles.Doctor
    };

    private static RolePermissionRuleDto MapRuleDto(RolePermissionRule r) => new()
    {
        Id = r.Id,
        BranchId = r.BranchId,
        RoleCode = r.RoleCode,
        ActorGroup = r.ActorGroup,
        ModuleKey = r.ModuleKey,
        TabKey = r.TabKey,
        CanView = r.CanView,
        CanCreate = r.CanCreate,
        CanEdit = r.CanEdit,
        CanDelete = r.CanDelete,
        CanSign = r.CanSign,
        CanPrint = r.CanPrint,
        CanFinalize = r.CanFinalize,
        PermissionVersion = r.PermissionVersion
    };

    private void InvalidatePermissionCache()
    {
        if (_cache is MemoryCache mc)
        {
            mc.Compact(1.0);
        }
    }
}
