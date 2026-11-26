using SCM.Domain.Shared.Enums;

namespace SCM.Domain.MasterData.DataSeed;

public static class MasterDataDataSeeder
{
    public static (List<Department> Departments, List<ClinicRoom> Rooms) SeedDepartmentsAndRooms()
    {
        var depKham = new Department("K-KHAM", "Khoa Khám Bệnh", isClinical: true, sortOrder: 1, legacyId: 1);
        var depCdha = new Department("K-CDHA", "Khoa Chẩn Đoán Hình Ảnh", isClinical: false, sortOrder: 2, legacyId: 2);
        var depXn = new Department("K-XN", "Khoa Xét Nghiệm", isClinical: false, sortOrder: 3, legacyId: 3);
        var depDuoc = new Department("K-DUOC", "Khoa Dược & Nhà Thuốc", isClinical: false, sortOrder: 4, legacyId: 4);
        var depVienPhi = new Department("K-VP", "Bộ Phận Thu Ngân & Viện Phí", isClinical: false, sortOrder: 5, legacyId: 5);

        var departments = new List<Department> { depKham, depCdha, depXn, depDuoc, depVienPhi };

        var rooms = new List<ClinicRoom>
        {
            new ClinicRoom("P.101", "Phòng Khám Nội Tổng Quát", depKham.Id, RoomType.Consultation, standardExamFee: 100000, currentCapacity: 12, maxQueueLimit: 20, legacyId: 101),
            new ClinicRoom("P.102", "Phòng Khám Ngoại - Chấn Thương", depKham.Id, RoomType.Consultation, standardExamFee: 100000, currentCapacity: 10, maxQueueLimit: 15, legacyId: 102),
            new ClinicRoom("P.SA01", "Phòng Siêu Âm 4D Số 1", depCdha.Id, RoomType.Ultrasound, standardExamFee: 0, currentCapacity: 8, maxQueueLimit: 15, legacyId: 201),
            new ClinicRoom("P.XQ01", "Phòng Chụp X-Quang Kỹ Thuật Số", depCdha.Id, RoomType.XRay, standardExamFee: 0, currentCapacity: 15, maxQueueLimit: 25, legacyId: 202),
            new ClinicRoom("P.XN01", "Phòng Lấy Mẫu Xét Nghiệm", depXn.Id, RoomType.Laboratory, standardExamFee: 0, currentCapacity: 20, maxQueueLimit: 30, legacyId: 301),
            new ClinicRoom("Q.DUOC", "Quầy Phát Thuốc BHYT & Nhà Thuốc", depDuoc.Id, RoomType.Pharmacy, standardExamFee: 0, currentCapacity: 25, maxQueueLimit: 40, legacyId: 401),
            new ClinicRoom("Q.TN01", "Quầy Thu Ngân 01 - Quyết Toán Viện Phí", depVienPhi.Id, RoomType.Cashier, standardExamFee: 0, currentCapacity: 30, maxQueueLimit: 50, legacyId: 501),
            new ClinicRoom("Q.TD01", "Quầy Tiếp Đón Bệnh Nhân & Phân Luồng", depKham.Id, RoomType.Reception, standardExamFee: 0, currentCapacity: 40, maxQueueLimit: 60, legacyId: 601)
        };

        return (departments, rooms);
    }

    public static (List<ServiceGroup> Groups, List<MedicalService> Services) SeedMedicalServices()
    {
        var grpKham = new ServiceGroup("GRP-KHAM", "Công Khám Bệnh Chuyên Khoa", ServiceType.Examination, 1);
        var grpSa = new ServiceGroup("GRP-SA", "Dịch Vụ Siêu Âm", ServiceType.Imaging, 2);
        var grpXq = new ServiceGroup("GRP-XQ", "Dịch Vụ Chụp X-Quang", ServiceType.Imaging, 3);
        var grpXn = new ServiceGroup("GRP-XN-HH", "Xét Nghiệm Huyết Học & Sinh Hóa", ServiceType.Laboratory, 4);

        var groups = new List<ServiceGroup> { grpKham, grpSa, grpXq, grpXn };

        var services = new List<MedicalService>
        {
            new MedicalService("KB-NOI", "Khám chuyên khoa Nội", grpKham.Id, "Lần", standardPrice: 100000, insurancePrice: 39000, corporatePrice: 80000, doctorCommissionRate: 0.15m, ministryEquivalentCode: "01.0001.001", legacyId: 1),
            new MedicalService("KB-NGOAI", "Khám chuyên khoa Ngoại", grpKham.Id, "Lần", standardPrice: 100000, insurancePrice: 39000, corporatePrice: 80000, doctorCommissionRate: 0.15m, ministryEquivalentCode: "01.0001.002", legacyId: 2),
            new MedicalService("SA-4D-OBUNG", "Siêu âm Doppler màu ổ bụng tổng quát 4D", grpSa.Id, "Lần", standardPrice: 250000, insurancePrice: 150000, corporatePrice: 180000, doctorCommissionRate: 0.20m, ministryEquivalentCode: "02.0015.003", legacyId: 10),
            new MedicalService("SA-TIM", "Siêu âm tim Doppler màu qua thành ngực", grpSa.Id, "Lần", standardPrice: 350000, insurancePrice: 220000, corporatePrice: 280000, doctorCommissionRate: 0.20m, ministryEquivalentCode: "02.0016.001", legacyId: 11),
            new MedicalService("XQ-PHOI-THANG", "Chụp X-quang ngực thẳng kỹ thuật số (CR/DR)", grpXq.Id, "Lần", standardPrice: 120000, insurancePrice: 75000, corporatePrice: 90000, doctorCommissionRate: 0.10m, ministryEquivalentCode: "03.0005.001", legacyId: 20),
            new MedicalService("XN-CTM-24TS", "Tổng phân tích tế bào máu ngoại vi (24 thông số)", grpXn.Id, "Lần", standardPrice: 90000, insurancePrice: 48000, corporatePrice: 70000, doctorCommissionRate: 0.08m, ministryEquivalentCode: "04.0001.001", legacyId: 30),
            new MedicalService("XN-GLUCOSE", "Định lượng Glucose trong máu", grpXn.Id, "Lần", standardPrice: 50000, insurancePrice: 26000, corporatePrice: 40000, doctorCommissionRate: 0.05m, ministryEquivalentCode: "04.0002.001", legacyId: 31),
            new MedicalService("XN-URE-CREATININE", "Định lượng Ure và Creatinine máu", grpXn.Id, "Lần", standardPrice: 80000, insurancePrice: 42000, corporatePrice: 65000, doctorCommissionRate: 0.05m, ministryEquivalentCode: "04.0003.001", legacyId: 32)
        };

        return (groups, services);
    }

    public static (List<ActiveIngredient> Ingredients, List<Drug> Drugs) SeedDrugs()
    {
        var para = new ActiveIngredient("HC-PARA", "Paracetamol", atcCode: "N02BE01", description: "Giảm đau, hạ sốt");
        var amox = new ActiveIngredient("HC-AMOX", "Amoxicillin", atcCode: "J01CA04", description: "Kháng sinh nhóm Penicillin");
        var omep = new ActiveIngredient("HC-OMEP", "Omeprazole", atcCode: "A02BC01", description: "Thuốc ức chế bơm Proton (PPI)");

        var ingredients = new List<ActiveIngredient> { para, amox, omep };

        var drugs = new List<Drug>
        {
            new Drug("TH-PAN-500", "Panadol Extra 500mg/65mg", para.Id, "500mg Paracetamol + 65mg Caffeine", "Viên", UsageRoute.Oral, standardPrice: 2500, insurancePrice: 1800, costPrice: 1500, registrationNumber: "VD-28432-17", nationalDrugCode: "VN-5001-01", manufacturer: "GSK", countryOfOrigin: "Việt Nam", isPrescriptionRequired: false, isHighAlert: false, minStockLevel: 200, legacyId: 1),
            new Drug("TH-EFF-500", "Efferalgan 500mg", para.Id, "500mg Paracetamol", "Viên sủi", UsageRoute.Oral, standardPrice: 3500, insurancePrice: 2400, costPrice: 2200, registrationNumber: "VN-16503-13", nationalDrugCode: "VN-5001-02", manufacturer: "UPSA SAS", countryOfOrigin: "Pháp", isPrescriptionRequired: false, isHighAlert: false, minStockLevel: 100, legacyId: 2),
            new Drug("TH-AUG-625", "Augmentin 625mg", amox.Id, "500mg Amoxicillin + 125mg Acid Clavulanic", "Viên", UsageRoute.Oral, standardPrice: 18000, insurancePrice: 12500, costPrice: 11000, registrationNumber: "VN-18234-14", nationalDrugCode: "VN-5002-01", manufacturer: "GSK", countryOfOrigin: "Anh", isPrescriptionRequired: true, isHighAlert: false, minStockLevel: 50, legacyId: 3),
            new Drug("TH-OMEP-20", "Omeprazol 20mg", omep.Id, "20mg Omeprazole", "Viên nang", UsageRoute.Oral, standardPrice: 3000, insurancePrice: 1800, costPrice: 1400, registrationNumber: "VD-25412-16", nationalDrugCode: "VN-5003-01", manufacturer: "Domesco", countryOfOrigin: "Việt Nam", isPrescriptionRequired: true, isHighAlert: false, minStockLevel: 100, legacyId: 4)
        };

        return (ingredients, drugs);
    }

    public static List<Staff> SeedStaffs(List<Department> departments)
    {
        var depKham = departments.FirstOrDefault(d => d.DepartmentCode == "K-KHAM");
        var depCdha = departments.FirstOrDefault(d => d.DepartmentCode == "K-CDHA");
        var depDuoc = departments.FirstOrDefault(d => d.DepartmentCode == "K-DUOC");
        var depVienPhi = departments.FirstOrDefault(d => d.DepartmentCode == "K-VP");

        return new List<Staff>
        {
            new Staff(
                "BS01", "BS.CKI Nguyễn Văn An", StaffRole.Doctor, depKham?.Id,
                practiceCertificateCode: "001234/BYT-CCHN", specialty: "Nội Tổng Quát", academicTitle: "BS.CKI",
                phoneNumber: "0901234567", email: "bs.an@smartclinic.vn", identityNumber: "079085001234", legacyId: 1,
                gender: "Nam", dateOfBirth: new DateTime(1985, 5, 20), address: "123 Nguyễn Trãi, Quận 5, TP.HCM",
                avatarUrl: "https://images.unsplash.com/photo-1622253692010-333f2da6031d?w=150",
                licenseIssueDate: new DateTime(2024, 1, 15), licenseExpiryDate: new DateTime(2029, 1, 15),
                nationalStaffId: "BYT-001234", primarySpecialty: "Nội Tổng Quát", secondarySpecialties: "Nhi Khoa, Lão Khoa",
                canFinalizeHealthCheckup: true, digitalCertSerial: "54010123456789ABCDEF", assignedRoomIds: "PK.01, PK.02", consultationFee: 250000),

            new Staff(
                "BS02", "ThS.BS Trần Thị Bình", StaffRole.Doctor, depCdha?.Id,
                practiceCertificateCode: "005678/HCM-CCHN", specialty: "Chẩn Đoán Hình Ảnh", academicTitle: "ThS.BS",
                phoneNumber: "0912345678", email: "bs.binh@smartclinic.vn", identityNumber: "079188005678", legacyId: 2,
                gender: "Nữ", dateOfBirth: new DateTime(1988, 10, 12), address: "456 Lê Văn Sỹ, Quận 3, TP.HCM",
                avatarUrl: "https://images.unsplash.com/photo-1594824813583-a4c3f58b0933?w=150",
                licenseIssueDate: new DateTime(2023, 6, 1), licenseExpiryDate: new DateTime(2028, 6, 1),
                nationalStaffId: "BYT-005678", primarySpecialty: "Chẩn Đoán Hình Ảnh", secondarySpecialties: "Thăm Dò Chức Năng",
                canFinalizeHealthCheckup: false, digitalCertSerial: "54020987654321FEDCBA", assignedRoomIds: "PK.03", consultationFee: 300000),

            new Staff("DS01", "Dược sĩ Lê Văn Cường", StaffRole.Pharmacist, depDuoc?.Id, practiceCertificateCode: "009988/HCM-CCHN", specialty: "Dược Lâm Sàng", academicTitle: "Dược sĩ ĐH", phoneNumber: "0923456789", email: "ds.cuong@smartclinic.vn", identityNumber: "079090009988", legacyId: 3, gender: "Nam", primarySpecialty: "Dược Lâm Sàng"),
            new Staff("TN01", "Phạm Thị Duyên", StaffRole.Cashier, depVienPhi?.Id, specialty: "Kế toán viện phí", academicTitle: "Cử nhân", phoneNumber: "0934567890", email: "tn.duyen@smartclinic.vn", identityNumber: "079192003456", legacyId: 4, gender: "Nữ", primarySpecialty: "Kế toán"),
            new Staff("LT01", "Hoàng Thị Mai", StaffRole.Receptionist, depKham?.Id, specialty: "Lễ tân điều phối", academicTitle: "Cao đẳng", phoneNumber: "0945678901", email: "lt.mai@smartclinic.vn", identityNumber: "079195007890", legacyId: 5, gender: "Nữ", primarySpecialty: "Tiếp đón")
        };
    }

    public static List<Icd10> SeedIcd10s()
    {
        return new List<Icd10>
        {
            new Icd10("I10", "Tăng huyết áp vô căn (nguyên phát)", "Essential (primary) hypertension", "IX", isChronic: true),
            new Icd10("E11.9", "Đái tháo đường týp 2 không có biến chứng", "Type 2 diabetes mellitus without complications", "IV", isChronic: true),
            new Icd10("J00", "Viêm mũi họng cấp [cảm thường]", "Acute nasopharyngitis [common cold]", "X", isChronic: false),
            new Icd10("J02.9", "Viêm họng cấp, không xác định", "Acute pharyngitis, unspecified", "X", isChronic: false),
            new Icd10("K29.0", "Viêm dạ dày cấp tính xuất huyết", "Acute hemorrhagic gastritis", "XI", isChronic: false),
            new Icd10("K29.7", "Viêm dạ dày, không xác định", "Gastritis, unspecified", "XI", isChronic: true)
        };
    }

    public static List<HealthcareFacility> SeedHealthcareFacilities()
    {
        return new List<HealthcareFacility>
        {
            new HealthcareFacility("79014", "Bệnh viện Quận 1", "79", "Hạng 2", "338 Hai Bà Trưng, Phường Tân Định, Quận 1, TP.HCM"),
            new HealthcareFacility("79025", "Bệnh viện Đa khoa Sài Gòn", "79", "Hạng 2", "125 Lê Lợi, Phường Bến Thành, Quận 1, TP.HCM"),
            new HealthcareFacility("79010", "Bệnh viện Nhân dân Gia Định", "79", "Hạng 1", "01 Nơ Trang Long, Phường 7, Quận Bình Thạnh, TP.HCM"),
            new HealthcareFacility("79011", "Bệnh viện Trưng Vương", "79", "Hạng 1", "266 Lý Thường Kiệt, Phường 14, Quận 10, TP.HCM")
        };
    }
}
