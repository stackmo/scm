using System;
using System.Collections.Generic;
using SCM.Domain.Queueing;
using SCM.Domain.Shared.Enums;
using Xunit;

namespace SCM.Domain.Tests;

public class Wave4_Reception_Invariants_Unit_Tests
{
    // =========================================================================
    // Invariant 2: CccdQrParser (Đề Án 06) Tests
    // Chuẩn: SoCCCD|SoCMND|HoTen|NgaySinh|GioiTinh|DiaChi|NgayCap
    // =========================================================================
    [Theory]
    [InlineData("001099012345|012345678|NGUYỄN VĂN AN|15081999|Nam|123 Phố Huế, Hàng Bài, Hoàn Kiếm, Hà Nội|10102021")]
    [InlineData("079195000123||TRẦN THỊ BÍCH|20111995|Nữ|456 Lê Duẩn, Bến Nghé, Quận 1, TP Hồ Chí Minh|15052022")]
    public void CccdQrParser_Should_Parse_Valid_Qr_Code(string rawQr)
    {
        var result = CccdQrParser.Parse(rawQr);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Data);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.CccdNumber));
        Assert.False(string.IsNullOrWhiteSpace(result.Data.FullName));
        Assert.True(result.Data.DateOfBirth > DateTime.MinValue);
        Assert.NotEqual(Gender.Other, result.Data.Gender);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.Address));
    }

    [Fact]
    public void CccdQrParser_Should_Extract_Correct_Fields()
    {
        string rawQr = "001099012345|012345678|NGUYỄN VĂN AN|15081999|Nam|123 Phố Huế, Hoàn Kiếm, Hà Nội|10102021";
        var result = CccdQrParser.Parse(rawQr);

        Assert.True(result.IsValid);
        var data = result.Data!;
        Assert.Equal("001099012345", data.CccdNumber);
        Assert.Equal("012345678", data.CmndNumber);
        Assert.Equal("NGUYỄN VĂN AN", data.FullName);
        Assert.Equal(new DateTime(1999, 8, 15), data.DateOfBirth);
        Assert.Equal(Gender.Male, data.Gender);
        Assert.Equal("123 Phố Huế, Hoàn Kiếm, Hà Nội", data.Address);
        Assert.Equal(new DateTime(2021, 10, 10), data.IssueDate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid_string_without_pipes")]
    [InlineData("001099|123|Short")] // Less than 6-7 segments
    public void CccdQrParser_Should_Reject_Malformed_Qr(string rawQr)
    {
        var result = CccdQrParser.Parse(rawQr);
        Assert.False(result.IsValid);
        Assert.Null(result.Data);
        Assert.NotNull(result.ErrorMessage);
    }

    // =========================================================================
    // Invariant 4: Privacy Masker (Nghị định 13/2023/NĐ-CP) Tests
    // Màn hình hàng đợi công cộng che mờ PII: Ng*** V** A
    // =========================================================================
    [Theory]
    [InlineData("Nguyễn Văn An", "Ng**** Vă* An")]
    [InlineData("Trần Thị Bích", "Tr** Th* Bích")]
    [InlineData("Lê An", "L* An")]
    [InlineData("Phạm Hoàng Minh Tuấn", "Ph** Ho*** Mi** Tuấn")]
    [InlineData("A", "A")]
    public void PrivacyMasker_MaskFullName_Should_Mask_Middle_Tokens(string fullName, string expectedMasked)
    {
        string masked = PrivacyMasker.MaskFullName(fullName);
        Assert.Equal(expectedMasked, masked);
    }

    [Theory]
    [InlineData("001099012345", "001*****2345")]
    [InlineData("0901234567", "090****567")]
    public void PrivacyMasker_MaskIdentifier_Should_Mask_Center_Digits(string identifier, string expectedMasked)
    {
        string masked = PrivacyMasker.MaskIdentifier(identifier);
        Assert.Equal(expectedMasked, masked);
    }

    // =========================================================================
    // Invariant 5: Master Patient Index (MPI) Fuzzy Search Tests
    // Chuẩn hóa tên không dấu + Năm sinh + SĐT/CCCD chống trùng
    // =========================================================================
    [Fact]
    public void MpiFuzzyMatcher_Normalize_Should_Remove_Diacritics_And_Uppercase()
    {
        string input = "Nguyễn Văn Đạt";
        string normalized = MpiFuzzyMatcher.NormalizeVietnamese(input);
        Assert.Equal("NGUYEN VAN DAT", normalized);
    }

    [Theory]
    [InlineData("Nguyễn Văn An", 1999, "001099012345", "NGUYEN VAN AN", 1999, "001099012345", 1.0)] // Trùng tuyệt đối
    [InlineData("Nguyễn Văn An", 1999, null, "Nguyen Van An", 1999, null, 1.0)] // Trùng tên không dấu + năm sinh
    [InlineData("Nguyễn Văn An", 1999, "0901234567", "Nguyễn Văn Ánh", 1999, "0901234567", 0.9)] // Cùng SĐT + năm sinh, tên gần giống
    [InlineData("Nguyễn Văn An", 1999, null, "Trần Văn Bình", 1985, null, 0.0)] // Khác hẳn
    public void MpiFuzzyMatcher_CalculateMatchScore_Should_Evaluate_Similarity(
        string nameA, int birthYearA, string? idA,
        string nameB, int birthYearB, string? idB,
        double minExpectedScore)
    {
        double score = MpiFuzzyMatcher.CalculateMatchScore(nameA, birthYearA, idA, nameB, birthYearB, idB);
        Assert.True(score >= minExpectedScore - 0.05, $"Expected score >= {minExpectedScore}, actual: {score}");
    }

    // =========================================================================
    // Invariant 1: ClinicRoomQueue Domain State & Pessimistic Number Allocation
    // =========================================================================
    [Fact]
    public void ClinicRoomQueue_NextQueueNumber_Should_Increment_CurrentMaxNumber()
    {
        var queue = new ClinicRoomQueue(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.Date);
        Assert.Equal(0, queue.CurrentMaxNumber);
        Assert.Equal(0, queue.CurrentServingNumber);

        int stt1 = queue.GenerateNextQueueNumber();
        Assert.Equal(1, stt1);
        Assert.Equal(1, queue.CurrentMaxNumber);

        int stt2 = queue.GenerateNextQueueNumber();
        Assert.Equal(2, stt2);
        Assert.Equal(2, queue.CurrentMaxNumber);

        queue.AdvanceServingNumber(1);
        Assert.Equal(1, queue.CurrentServingNumber);
    }
}
