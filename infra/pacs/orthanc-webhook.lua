-- Orthanc OnStableStudy Webhook -> SmartClinicManagement RIS/PACS API
local webhookUrl = os.getenv("SCM_WEBHOOK_URL") or "http://host.docker.internal:5000/api/ris-pacs/pacs-webhook/study-stable"
local webhookSecret = os.getenv("SCM_PACS_WEBHOOK_SECRET") or "SCM-PACS-SECRET-2026"

function OnStableStudy(studyId, tags, metadata)
    local studyUid = tags["StudyInstanceUID"] or ""
    local accession = tags["AccessionNumber"] or ""
    local patientId = tags["PatientID"] or ""
    local modality = tags["Modality"] or tags["ModalitiesInStudy"] or ""

    if studyUid == "" then
        return
    end

    local payload = string.format(
        '{"studyInstanceUid":"%s","accessionNumber":"%s","patientId":"%s","modality":"%s","dicomWebUrl":"/pacs/ohif/viewer?StudyInstanceUIDs=%s"}',
        studyUid,
        accession,
        patientId,
        modality,
        studyUid
    )

    local headers = {
        ["Content-Type"] = "application/json",
        ["X-Pacs-Webhook-Secret"] = webhookSecret
    }

    local response = HttpPost(webhookUrl, payload, headers)
    if response == nil then
        print("[SCM-ORTHANC] Canh bao: Khong gui duoc OnStableStudy webhook cho Study " .. studyUid)
    else
        print("[SCM-ORTHANC] Da dong bo OnStableStudy Study=" .. studyUid .. " Accession=" .. accession)
    end
end
