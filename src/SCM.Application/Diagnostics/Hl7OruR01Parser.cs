namespace SCM.Application.Diagnostics;

/// <summary>
/// Parser bản tin HL7 v2.x ORU^R01 (tách segment PID, OBR lấy TubeBarcode, OBX lấy TestCode/Value/Unit/RefRange/Flag).
/// Tái sử dụng chuẩn hóa đơn vị và ánh xạ 35 chỉ số M3 từ LisAstmParser.
/// </summary>
public static class Hl7OruR01Parser
{
    public static List<AstmParsedResult> ParseMessage(string hl7Message)
    {
        var results = new List<AstmParsedResult>();
        if (string.IsNullOrWhiteSpace(hl7Message))
            return results;

        var payloadHash = LisAstmParser.ComputePayloadHash(hl7Message);
        var lines = hl7Message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string currentBarcode = string.Empty;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var fields = line.Split('|');
            if (fields.Length == 0) continue;

            var segment = fields[0].Trim().ToUpperInvariant();
            if (segment == "OBR")
            {
                // OBR-2 (Placer Order Number) or OBR-3 (Filler Order Number)
                var placer = fields.Length > 2 ? fields[2].Trim() : string.Empty;
                var filler = fields.Length > 3 ? fields[3].Trim() : string.Empty;
                var rawBarcode = !string.IsNullOrWhiteSpace(placer) ? placer : filler;
                if (rawBarcode.Contains('^')) rawBarcode = rawBarcode.Split('^')[0].Trim();
                if (!string.IsNullOrWhiteSpace(rawBarcode)) currentBarcode = rawBarcode;
            }
            else if (segment == "OBX" && fields.Length >= 6)
            {
                // OBX|1|NM|KALI^Kali mau||6.9|mmol/L|3.5-5.1|HH|||F
                var testRaw = fields.Length > 3 ? fields[3].Trim() : string.Empty;
                var testCode = ExtractHl7TestCode(testRaw);
                var valueRaw = fields.Length > 5 ? fields[5].Trim() : string.Empty;
                var unitRaw = fields.Length > 6 ? fields[6].Trim() : string.Empty;
                var refRange = fields.Length > 7 ? fields[7].Trim() : string.Empty;
                var abnormalFlag = fields.Length > 8 ? fields[8].Trim() : string.Empty;

                if (string.IsNullOrWhiteSpace(testCode)) continue;

                results.Add(LisAstmParser.NormalizeAndBuildResult(
                    currentBarcode,
                    testCode,
                    valueRaw,
                    unitRaw,
                    refRange,
                    abnormalFlag,
                    payloadHash));
            }
        }

        return results;
    }

    private static string ExtractHl7TestCode(string testRaw)
    {
        if (string.IsNullOrWhiteSpace(testRaw)) return string.Empty;
        if (testRaw.Contains('^'))
        {
            var segments = testRaw.Split('^', StringSplitOptions.RemoveEmptyEntries);
            return segments.FirstOrDefault()?.Trim() ?? testRaw.Trim();
        }
        return testRaw.Trim();
    }
}
