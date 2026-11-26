using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Identity;
using SCM.Domain.Shared.Roles;
using Xunit;

namespace SCM.Domain.Tests.Identity;

public class DoctorCapabilityMatrixTests
{
    [Fact]
    public void DoctorCapability_Resolves_Vietnamese_Accent_Aliases_And_Secondary_Specialties()
    {
        var cap = new DoctorCapability
        {
            RoleCode = SCMRoles.Doctor,
            ActorGroup = ClinicalActorGroup.SpecialtyDoctor,
            PrimarySpecialty = "Mắt - Nhãn khoa",
            SecondarySpecialties = new[] { "Tai Mũi Họng", "Da liễu" },
            AssignedRoomCodes = new[] { "P.104", "P.105" }
        };

        Assert.True(cap.CanExamineSpecialty(ClinicalSpecialtyType.Ophthalmology));
        Assert.True(cap.CanExamineSpecialty(ClinicalSpecialtyType.Otorhinolaryngology));
        Assert.True(cap.CanExamineSpecialty(ClinicalSpecialtyType.Dermatology));
        Assert.False(cap.CanExamineSpecialty(ClinicalSpecialtyType.InternalMedicine));
        Assert.False(cap.CanExamineSpecialty(ClinicalSpecialtyType.Dentomaxillofacial));

        Assert.True(cap.CanAccessRoom("p.104"));
        Assert.False(cap.CanAccessRoom("P.106"));
        Assert.Equal("tab_eye", cap.GetDefaultClinicalTabKey());
    }

    [Fact]
    public void Paraclinical_And_Imaging_Roles_Cannot_Examine_Clinical_Specialties()
    {
        var tech = new DoctorCapability
        {
            RoleCode = SCMRoles.NurseTech,
            ActorGroup = ClinicalActorGroup.ParaclinicalTechnician,
            PrimarySpecialty = "InternalMedicine",
            AssignedRoomCodes = new[] { "P.XN01" }
        };

        Assert.False(tech.CanExamineSpecialty(ClinicalSpecialtyType.InternalMedicine));
        Assert.Equal("tab_vital_signs", tech.GetDefaultClinicalTabKey());

        var rad = new DoctorCapability
        {
            RoleCode = SCMRoles.Doctor,
            ActorGroup = ClinicalActorGroup.RadiologyDoctor,
            PrimarySpecialty = "Surgery"
        };
        Assert.False(rad.CanExamineSpecialty(ClinicalSpecialtyType.Surgery));
        Assert.Equal("tab_imaging_capture", rad.GetDefaultClinicalTabKey());
    }
}
