using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.MasterData;
using SCM.Application.Contracts.MasterData.Dtos;
using SCM.Domain.Branches;
using SCM.Domain.MasterData;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;

namespace SCM.Application.MasterData;

public class MasterDataAppService : IMasterDataAppService
{
    private readonly SCMDbContext _dbContext;

    public MasterDataAppService(SCMDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<DepartmentDto>> GetDepartmentsAsync(Guid? branchId = null, bool activeOnly = true)
    {
        var query = _dbContext.Departments.AsNoTracking();
        if (activeOnly) query = query.Where(d => d.IsActive);

        if (branchId.HasValue && branchId.Value != Guid.Empty)
        {
            query = query.Where(d => d.BranchId == null || d.BranchId == branchId.Value);
        }

        return await query.OrderBy(d => d.SortOrder).Select(d => new DepartmentDto
        {
            Id = d.Id,
            BranchId = d.BranchId,
            DepartmentCode = d.DepartmentCode,
            DepartmentName = d.DepartmentName,
            MinistryDepartmentCode = d.MinistryDepartmentCode,
            IsClinical = d.IsClinical,
            SortOrder = d.SortOrder,
            IsActive = d.IsActive,
            LegacyId = d.LegacyId
        }).ToListAsync();
    }

    public async Task<List<ClinicRoomDto>> GetClinicRoomsAsync(Guid? branchId = null, Guid? departmentId = null, bool activeOnly = true)
    {
        var query = _dbContext.ClinicRooms.Include(r => r.Department).Include(r => r.Branch).AsNoTracking();

        if (branchId.HasValue && branchId.Value != Guid.Empty)
            query = query.Where(r => r.BranchId == branchId.Value);

        if (departmentId.HasValue && departmentId.Value != Guid.Empty)
            query = query.Where(r => r.DepartmentId == departmentId.Value);

        if (activeOnly)
            query = query.Where(r => r.IsActive);

        return await query.Select(r => new ClinicRoomDto
        {
            Id = r.Id,
            BranchId = r.BranchId,
            BranchName = r.Branch != null ? r.Branch.Name : null,
            RoomCode = r.RoomCode,
            RoomName = r.RoomName,
            DepartmentId = r.DepartmentId,
            DepartmentName = r.Department != null ? r.Department.DepartmentName : null,
            RoomType = r.RoomType,
            DefaultDoctorId = r.DefaultDoctorId,
            StandardExamFee = r.StandardExamFee,
            CurrentCapacity = r.CurrentCapacity,
            MaxQueueLimit = r.MaxQueueLimit,
            IsActive = r.IsActive
        }).ToListAsync();
    }

    public async Task<List<ClinicRoomDto>> ProvisionBranchRoomsFromTemplateAsync(Guid branchId, string templateType = "POLYCLINIC_STANDARD")
    {
        var branch = await _dbContext.Branches.FindAsync(branchId);
        if (branch == null)
            throw new ArgumentException($"Chi nhánh với Id {branchId} không tồn tại.");

        // Kiểm tra hoặc tự động tạo các Khoa chuẩn Bộ Y Tế (QĐ 130) dùng chung
        var deptKham = await _dbContext.Departments.FirstOrDefaultAsync(d => d.DepartmentCode == "K-KHAM");
        if (deptKham == null)
        {
            deptKham = new Department("K-KHAM", "Khoa Khám Bệnh", isClinical: true, sortOrder: 1, ministryDepartmentCode: "K01");
            _dbContext.Departments.Add(deptKham);
        }

        var deptCls = await _dbContext.Departments.FirstOrDefaultAsync(d => d.DepartmentCode == "K-CLS");
        if (deptCls == null)
        {
            deptCls = new Department("K-CLS", "Khoa Cận Lâm Sàng & Xét Nghiệm", isClinical: false, sortOrder: 2, ministryDepartmentCode: "K11");
            _dbContext.Departments.Add(deptCls);
        }

        var deptCdha = await _dbContext.Departments.FirstOrDefaultAsync(d => d.DepartmentCode == "K-CDHA");
        if (deptCdha == null)
        {
            deptCdha = new Department("K-CDHA", "Khoa Chẩn Đoán Hình Ảnh", isClinical: false, sortOrder: 3, ministryDepartmentCode: "K12");
            _dbContext.Departments.Add(deptCdha);
        }

        var deptDuoc = await _dbContext.Departments.FirstOrDefaultAsync(d => d.DepartmentCode == "K-DUOC");
        if (deptDuoc == null)
        {
            deptDuoc = new Department("K-DUOC", "Khoa Dược & Nhà Thuốc Cơ Sở", isClinical: false, sortOrder: 4, ministryDepartmentCode: "K20");
            _dbContext.Departments.Add(deptDuoc);
        }

        await _dbContext.SaveChangesAsync();

        // Danh sách 13 buồng khám vật lý tiêu chuẩn theo Thông tư 32/2023 và Mẫu M3
        var standardRooms = new List<ClinicRoom>
        {
            new ClinicRoom("P101", "Phòng Khám Thể Lực (BMI, Huyết Áp)", deptKham.Id, RoomType.GeneralClinical, standardExamFee: 50000, currentCapacity: 10, maxQueueLimit: 20, branchId: branchId),
            new ClinicRoom("P102", "Phòng Khám Nội Khoa", deptKham.Id, RoomType.InternalMedicine, standardExamFee: 100000, currentCapacity: 10, maxQueueLimit: 20, branchId: branchId),
            new ClinicRoom("P103", "Phòng Khám Ngoại Khoa", deptKham.Id, RoomType.Surgery, standardExamFee: 100000, currentCapacity: 10, maxQueueLimit: 20, branchId: branchId),
            new ClinicRoom("P104", "Phòng Khám Sản Phụ Khoa", deptKham.Id, RoomType.ObGyn, standardExamFee: 120000, currentCapacity: 8, maxQueueLimit: 15, branchId: branchId),
            new ClinicRoom("P105", "Phòng Khám Mắt (Thị Lực, Khúc Xạ)", deptKham.Id, RoomType.Ophthalmology, standardExamFee: 80000, currentCapacity: 10, maxQueueLimit: 20, branchId: branchId),
            new ClinicRoom("P106", "Phòng Khám Tai Mũi Họng", deptKham.Id, RoomType.ENT, standardExamFee: 80000, currentCapacity: 10, maxQueueLimit: 20, branchId: branchId),
            new ClinicRoom("P107", "Phòng Khám Răng Hàm Mặt (32 Răng)", deptKham.Id, RoomType.Dental, standardExamFee: 80000, currentCapacity: 10, maxQueueLimit: 20, branchId: branchId),
            new ClinicRoom("P108", "Phòng Khám Da Liễu", deptKham.Id, RoomType.Dermatology, standardExamFee: 80000, currentCapacity: 10, maxQueueLimit: 20, branchId: branchId),
            new ClinicRoom("P201", "Phòng Lấy Mẫu Bệnh Phẩm Xét Nghiệm", deptCls.Id, RoomType.Laboratory, standardExamFee: 0, currentCapacity: 20, maxQueueLimit: 50, branchId: branchId),
            new ClinicRoom("P202", "Phòng Chụp X-Quang Kỹ Thuật Số (DR)", deptCdha.Id, RoomType.XRay, standardExamFee: 80000, currentCapacity: 15, maxQueueLimit: 30, branchId: branchId),
            new ClinicRoom("P203", "Phòng Siêu Âm Tổng Quát", deptCdha.Id, RoomType.Ultrasound, standardExamFee: 150000, currentCapacity: 10, maxQueueLimit: 20, branchId: branchId),
            new ClinicRoom("P204", "Phòng Đo Điện Tim (ECG)", deptCdha.Id, RoomType.Procedure, standardExamFee: 60000, currentCapacity: 15, maxQueueLimit: 25, branchId: branchId),
            new ClinicRoom("P301", "Quầy Thuốc Cơ Sở", deptDuoc.Id, RoomType.Pharmacy, standardExamFee: 0, currentCapacity: 20, maxQueueLimit: 50, branchId: branchId)
        };

        _dbContext.ClinicRooms.AddRange(standardRooms);
        await _dbContext.SaveChangesAsync();

        return standardRooms.Select(r => new ClinicRoomDto
        {
            Id = r.Id,
            BranchId = r.BranchId,
            BranchName = branch.Name,
            RoomCode = r.RoomCode,
            RoomName = r.RoomName,
            DepartmentId = r.DepartmentId,
            DepartmentName = r.Department?.DepartmentName,
            RoomType = r.RoomType,
            StandardExamFee = r.StandardExamFee,
            CurrentCapacity = r.CurrentCapacity,
            MaxQueueLimit = r.MaxQueueLimit,
            IsActive = r.IsActive
        }).ToList();
    }

    public async Task<List<BranchDto>> GetBranchesAsync(bool activeOnly = true)
    {
        var query = _dbContext.Branches.AsNoTracking();
        if (activeOnly) query = query.Where(b => b.ActiveStatus);

        return await query.OrderBy(b => b.Code).Select(b => new BranchDto
        {
            Id = b.Id,
            Code = b.Code,
            Name = b.Name,
            MinistryFacilityCode = b.MinistryFacilityCode,
            LicenseNo = b.LicenseNo,
            Address = b.Address,
            PhoneNumber = b.PhoneNumber,
            DoctorInCharge = b.DoctorInCharge,
            ActiveStatus = b.ActiveStatus,
            BankAccountDetails = b.BankAccountDetails,
            CreatedAt = b.CreatedAt
        }).ToListAsync();
    }

    public async Task<BranchDto> GetBranchByIdAsync(Guid branchId)
    {
        var b = await _dbContext.Branches.FindAsync(branchId);
        if (b == null) throw new ArgumentException($"Không tìm thấy chi nhánh với Id {branchId}");

        return new BranchDto
        {
            Id = b.Id,
            Code = b.Code,
            Name = b.Name,
            MinistryFacilityCode = b.MinistryFacilityCode,
            LicenseNo = b.LicenseNo,
            Address = b.Address,
            PhoneNumber = b.PhoneNumber,
            DoctorInCharge = b.DoctorInCharge,
            ActiveStatus = b.ActiveStatus,
            BankAccountDetails = b.BankAccountDetails,
            CreatedAt = b.CreatedAt
        };
    }

    public async Task<BranchDto> CreateBranchAsync(CreateBranchDto input)
    {
        var branch = new Branch(
            input.Code,
            input.Name,
            input.MinistryFacilityCode,
            input.LicenseNo,
            input.Address,
            input.PhoneNumber,
            input.DoctorInCharge,
            input.BankAccountDetails,
            input.DigitalCertConfig
        );

        _dbContext.Branches.Add(branch);
        await _dbContext.SaveChangesAsync();

        return new BranchDto
        {
            Id = branch.Id,
            Code = branch.Code,
            Name = branch.Name,
            MinistryFacilityCode = branch.MinistryFacilityCode,
            LicenseNo = branch.LicenseNo,
            Address = branch.Address,
            PhoneNumber = branch.PhoneNumber,
            DoctorInCharge = branch.DoctorInCharge,
            ActiveStatus = branch.ActiveStatus,
            BankAccountDetails = branch.BankAccountDetails,
            CreatedAt = branch.CreatedAt
        };
    }

    public async Task<List<BranchServiceAvailabilityDto>> GetBranchServiceAvailabilitiesAsync(Guid branchId)
    {
        var services = await _dbContext.MedicalServices.Where(s => s.IsActive).AsNoTracking().ToListAsync();
        var overrides = await _dbContext.BranchServiceAvailabilities.Where(o => o.BranchId == branchId).AsNoTracking().ToListAsync();

        return services.Select(s =>
        {
            var ovr = overrides.FirstOrDefault(o => o.MedicalServiceId == s.Id);
            bool isAvailable = ovr == null ? true : ovr.IsAvailable;
            decimal? customPrice = ovr?.CustomPrice;
            decimal effectivePrice = customPrice ?? s.StandardPrice;

            return new BranchServiceAvailabilityDto
            {
                Id = ovr?.Id ?? Guid.Empty,
                BranchId = branchId,
                MedicalServiceId = s.Id,
                ServiceCode = s.ServiceCode,
                ServiceName = s.ServiceName,
                BasePrice = s.StandardPrice,
                CustomPrice = customPrice,
                CustomFloorPrice = ovr?.CustomFloorPrice,
                EffectivePrice = effectivePrice,
                IsAvailable = isAvailable
            };
        }).ToList();
    }

    public async Task<BranchServiceAvailabilityDto> SetBranchServiceAvailabilityAsync(SetBranchServiceAvailabilityDto input)
    {
        var service = await _dbContext.MedicalServices.FindAsync(input.MedicalServiceId);
        if (service == null) throw new ArgumentException($"Không tìm thấy dịch vụ y tế {input.MedicalServiceId}");

        var ovr = await _dbContext.BranchServiceAvailabilities
            .FirstOrDefaultAsync(o => o.BranchId == input.BranchId && o.MedicalServiceId == input.MedicalServiceId);

        if (ovr == null)
        {
            ovr = new BranchServiceAvailability(input.BranchId, input.MedicalServiceId, input.IsAvailable, input.CustomPrice, input.CustomFloorPrice);
            _dbContext.BranchServiceAvailabilities.Add(ovr);
        }
        else
        {
            ovr.IsAvailable = input.IsAvailable;
            ovr.CustomPrice = input.CustomPrice;
            ovr.CustomFloorPrice = input.CustomFloorPrice;
            ovr.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        return new BranchServiceAvailabilityDto
        {
            Id = ovr.Id,
            BranchId = ovr.BranchId,
            MedicalServiceId = ovr.MedicalServiceId,
            ServiceCode = service.ServiceCode,
            ServiceName = service.ServiceName,
            BasePrice = service.StandardPrice,
            CustomPrice = ovr.CustomPrice,
            CustomFloorPrice = ovr.CustomFloorPrice,
            EffectivePrice = ovr.CustomPrice ?? service.StandardPrice,
            IsAvailable = ovr.IsAvailable
        };
    }

    public async Task<List<MedicalServiceDto>> GetMedicalServicesAsync(string? filter = null, bool? isBHYT = null, bool activeOnly = true)
    {
        var query = _dbContext.MedicalServices.Include(s => s.ServiceGroup).AsNoTracking();

        if (activeOnly)
            query = query.Where(s => s.IsActive);
        if (isBHYT.HasValue)
            query = query.Where(s => s.IsCoveredByInsurance == isBHYT.Value);
        if (!string.IsNullOrWhiteSpace(filter))
            query = query.Where(s => s.ServiceCode.Contains(filter) || s.ServiceName.Contains(filter) || (s.MinistryEquivalentCode != null && s.MinistryEquivalentCode.Contains(filter)));

        return await query.OrderBy(s => s.ServiceCode).Take(100).Select(s => new MedicalServiceDto
        {
            Id = s.Id,
            ServiceCode = s.ServiceCode,
            ServiceName = s.ServiceName,
            ServiceGroupId = s.ServiceGroupId,
            ServiceGroupName = s.ServiceGroup != null ? s.ServiceGroup.GroupName : null,
            Unit = s.Unit,
            StandardPrice = s.StandardPrice,
            InsurancePrice = s.InsurancePrice,
            CorporatePrice = s.CorporatePrice,
            DoctorCommissionRate = s.DoctorCommissionRate,
            MinistryEquivalentCode = s.MinistryEquivalentCode,
            MinistryEquivalentName = s.MinistryEquivalentName,
            MinistryPriceCode = s.MinistryPriceCode,
            MinistryCostGroupId = s.MinistryCostGroupId,
            InsuranceCoverageRate = s.InsuranceCoverageRate,
            IsCoveredByInsurance = s.IsCoveredByInsurance,
            SurgeryType = s.SurgeryType,
            IsActive = s.IsActive,
            LegacyId = s.LegacyId
        }).ToListAsync();
    }

    public async Task<List<DrugDto>> GetDrugsAsync(string? filter = null, bool? isBHYT = null, bool activeOnly = true)
    {
        var query = _dbContext.Drugs.Include(d => d.ActiveIngredient).AsNoTracking();

        if (activeOnly)
            query = query.Where(d => d.IsActive);
        if (isBHYT.HasValue)
            query = query.Where(d => d.IsInsuranceCovered == isBHYT.Value);
        if (!string.IsNullOrWhiteSpace(filter))
            query = query.Where(d => d.DrugCode.Contains(filter) || d.DrugName.Contains(filter) || (d.RegistrationNumber != null && d.RegistrationNumber.Contains(filter)));

        return await query.OrderBy(d => d.DrugName).Take(100).Select(d => new DrugDto
        {
            Id = d.Id,
            DrugCode = d.DrugCode,
            DrugName = d.DrugName,
            ActiveIngredientId = d.ActiveIngredientId,
            ActiveIngredientName = d.ActiveIngredient != null ? d.ActiveIngredient.IngredientName : null,
            ActiveIngredientCode = d.ActiveIngredientCode,
            Content = d.Content,
            Unit = d.Unit,
            UsageRoute = d.UsageRoute,
            MinistryRouteCode = d.MinistryRouteCode,
            StandardPrice = d.StandardPrice,
            InsurancePrice = d.InsurancePrice,
            CostPrice = d.CostPrice,
            RegistrationNumber = d.RegistrationNumber,
            NationalDrugCode = d.NationalDrugCode,
            InsuranceCoverageRate = d.InsuranceCoverageRate,
            IsInsuranceCovered = d.IsInsuranceCovered,
            Manufacturer = d.Manufacturer,
            CountryOfOrigin = d.CountryOfOrigin,
            IsPrescriptionRequired = d.IsPrescriptionRequired,
            IsHighAlert = d.IsHighAlert,
            IsActive = d.IsActive
        }).ToListAsync();
    }

    public async Task<List<MedicalSupplyDto>> GetMedicalSuppliesAsync(string? filter = null, bool activeOnly = true)
    {
        var query = _dbContext.MedicalSupplies.AsNoTracking();

        if (activeOnly) query = query.Where(s => s.IsActive);
        if (!string.IsNullOrWhiteSpace(filter))
            query = query.Where(s => s.SupplyCode.Contains(filter) || s.SupplyName.Contains(filter));

        return await query.OrderBy(s => s.SupplyName).Take(100).Select(s => new MedicalSupplyDto
        {
            Id = s.Id,
            SupplyCode = s.SupplyCode,
            SupplyName = s.SupplyName,
            Unit = s.Unit,
            Specification = s.Specification,
            StandardPrice = s.StandardPrice,
            InsurancePrice = s.InsurancePrice,
            MinistrySupplyCode = s.MinistrySupplyCode,
            MinistryCostGroupId = s.MinistryCostGroupId,
            InsuranceCoverageRate = s.InsuranceCoverageRate,
            IsCoveredByInsurance = s.IsCoveredByInsurance,
            IsActive = s.IsActive
        }).ToListAsync();
    }

    public async Task<List<StaffDto>> GetStaffsAsync(StaffRole? role = null, bool activeOnly = true)
    {
        var query = _dbContext.Staffs.AsNoTracking();

        if (activeOnly) query = query.Where(s => s.IsActive);
        if (role.HasValue) query = query.Where(s => s.StaffRole == role.Value);

        return await query.OrderBy(s => s.StaffCode).Select(s => new StaffDto
        {
            Id = s.Id,
            StaffCode = s.StaffCode,
            FullName = s.FullName,
            StaffRole = s.StaffRole,
            DepartmentId = s.DepartmentId,
            PracticeCertificateCode = s.PracticeCertificateCode,
            Specialty = s.Specialty,
            AcademicTitle = s.AcademicTitle,
            PhoneNumber = s.PhoneNumber,
            Email = s.Email,
            NationalStaffId = s.NationalStaffId,
            CanFinalizeHealthCheckup = s.CanFinalizeHealthCheckup,
            CertificateTrainingKsk = s.CertificateTrainingKsk,
            DigitalCertSerial = s.DigitalCertSerial,
            IsActive = s.IsActive
        }).ToListAsync();
    }

    public async Task<List<Icd10Dto>> SearchIcd10Async(string keyword, int maxResultCount = 20)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return new List<Icd10Dto>();
        var lowerKeyword = keyword.ToLower();

        return await _dbContext.AppRef_ICDs.AsNoTracking()
            .Where(x => x.MaICD.ToLower().Contains(lowerKeyword) || x.Name.ToLower().Contains(lowerKeyword))
            .OrderBy(x => x.MaICD)
            .Take(maxResultCount)
            .Select(x => new Icd10Dto
            {
                Id = x.Id,
                IcdCode = x.MaICD,
                DiseaseName = x.Name
            }).ToListAsync();
    }

    public async Task<List<DentalStatusCatalogDto>> GetDentalStatusCatalogAsync()
    {
        return await _dbContext.AppRef_TinhTrangRangs.AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new DentalStatusCatalogDto
            {
                Id = x.Id,
                TenGoi = x.Name,
                MoTa = x.Code,
                MaSo = x.Id
            }).ToListAsync();
    }
}
