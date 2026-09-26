using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using System.Security.Claims;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>
    /// M02 Admin ACL probes (New-API only). Verification: docs/M02_ADMIN_CLINICAL_VERIFICATION_GUIDE.md
    /// </summary>
    [Route("api/AdminAcl")]
    [ApiController]
    [Authorize]
    public class AdminAclController : ControllerBase
    {
        [HttpGet("me")]
        public IActionResult Me()
        {
            var canAccess = AdminAuthorizationPolicies.IsAdminPortalUser(User);
            int? userId = null;
            try { userId = User.GetUserId(); }
            catch
            {
                var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("nameid")?.Value
                    ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.NameId)?.Value;
                if (int.TryParse(raw, out var parsed)) userId = parsed;
            }

            return Ok(new
            {
                success = true,
                message = "Admin ACL status",
                data = new
                {
                    userId,
                    userName = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName)?.Value
                        ?? User.Identity?.Name,
                    roleId = AdminAuthorizationPolicies.GetRoleId(User),
                    roleName = AdminAuthorizationPolicies.GetRoleName(User),
                    canAccessAdminPortal = canAccess,
                    canMutateAdminMasters = canAccess,
                    policyName = AdminAuthorizationPolicies.AdminPortal,
                    phasesImplemented = new[] { "W0", "W1", "W2", "W3", "W4", "W5", "W6", "W7" },
                    notes = canAccess
                        ? "Admin/Management may mutate masters protected with AdminPortal (W1–W7)."
                        : "Not Admin/Management — mutate APIs must return 403."
                }
            });
        }

        [HttpGet("ping")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult Ping()
        {
            return Ok(new
            {
                success = true,
                message = "AdminPortal policy OK",
                data = new { policy = AdminAuthorizationPolicies.AdminPortal, at = DateTime.UtcNow }
            });
        }

        /// <summary>W1 repertory map (kept for compatibility).</summary>
        [HttpGet("repertory")]
        public IActionResult Repertory() => Coverage();

        /// <summary>M02 W0–W6 coverage + dual-API map (New-API only).</summary>
        [HttpGet("coverage")]
        public IActionResult Coverage()
        {
            var canMutate = AdminAuthorizationPolicies.IsAdminPortalUser(User);

            return Ok(new
            {
                success = true,
                message = "M02 Admin Clinical ACL coverage (W0–W7)",
                data = new
                {
                    policy = AdminAuthorizationPolicies.AdminPortal,
                    callerCanMutate = canMutate,
                    verificationDoc = "docs/M02_ADMIN_CLINICAL_VERIFICATION_GUIDE.md",
                    phases = new object[]
                    {
                        new { id = "W0", name = "Foundation", status = "done",
                            items = new[] { "JWT RoleId/RoleName", "AdminPortal policy", "UI AdminProtected", "AdminAcl/me|ping" } },
                        new { id = "W1", name = "Repertory ADM-R01…R09", status = "done",
                            oldMutate = new[] { "Section","SubSection","RubricRemedy","Remedy","RemedyGrade","Language","Intensity","BodyPart" },
                            newMutate = new[] { "Section","SubSection","RubricRemedy","Remedy","RemedyGrade","Excel import/export" },
                            doctorReads = new[] { "SearchRubricsByKeyword","GetRubricDetails","clipboard/board reads on Old" } },
                        new { id = "W2", name = "Materia Medica ADM-M01…M04", status = "done",
                            oldMutate = new[] { "Author","MateriaMedicaMaster","MateriaMedicaHead","MeteriaMedicaDetails" },
                            newMutate = new[] { "(MM remedies details is GET-only on New)" },
                            dualApi = "Admin MM CRUD → Old-API; keep host" },
                        new { id = "W3", name = "Diagnosis ADM-D01…D03", status = "done",
                            oldMutate = new[] { "DiagnosisSystem","DiagnosisTherapeuticsDetail","Diagnosis Save/Delete","DiagnosisGroup" },
                            newMutate = new[] { "(no Diagnosis controllers on New — Old only)" },
                            doctorReads = new[] { "DiagnosisSearch","GetDiagnosisKeywordByTab","GetRubricByKeywordID","GetThrepoticByDiagonisID" } },
                        new { id = "W4", name = "Adverse Effect ADM-A01…A03", status = "done",
                            oldMutate = new[] { "DrugSystem","DrugGroup","AllopathicDrug","Adverse/Other/Serious Delete" },
                            newMutate = new[] { "AllopathicDrug Save/Delete","SeriousSideEffect Delete" },
                            doctorReads = new[] { "GetAllopathicDrugfordropdown (New)" } },
                        new { id = "W5", name = "Questions ADM-Q01…Q02", status = "done",
                            oldMutate = new[] { "QuestionSection/Group/SubGroup","ClinicalQuestions","ClinicalQueKeyword" },
                            newMutate = new[] { "QuestionSection/Group/SubGroup Add/Update/Delete" },
                            dualApi = "Admin question CRUD primarily Old; New has parity controllers locked" },
                        new { id = "W6", name = "3D Body ADM-3D1…3D3", status = "done",
                            oldMutate = new[] { "(none — 3D is New-only)" },
                            newMutate = new[] { "MeshKey","SectionMaster","Hotspot Add/Update/Delete" },
                            dualApi = "Already New-API — confirmed; AdminPortal on mutate" },
                        new { id = "W7", name = "Business ADM-B01…B04", status = "done",
                            oldMutate = new[] { "Qualification Save/Delete","PatientLabTest AddEdit/Delete","Package Save/Delete/Topup","RoleMaster","RoleDetails","MenuMaster" },
                            newMutate = new[] { "Qualification Add/Update/Delete","Package Save/Delete/Topup" },
                            newApis = new[] { "GET /api/mastersAPI/GetMenuByRole (restored)" },
                            dualApi = "Qualifications→New; Lab catalog→Old (board PatientLab reads Old); Packages→Old (+ New parity); Roles→Old",
                            notes = new[] { "PackageEntryDetail = S1 SaaS only", "PatientLab SaveOrder/Entry NOT AdminPortal (doctor clinical)", "Account/Pharmacy menu seed optional SQL template" } }
                    },
                    sampleMutateForbiddenForDoctor = new[]
                    {
                        "POST /api/Author (Old SaveAuthor)",
                        "POST /api/diagnosis (Old SaveDiagnosis)",
                        "POST /api/DrugSystem (Old)",
                        "POST /api/questionsection (Old)",
                        "POST /api/threeDBodyPartMeshKeyMaster/AddThreeDBodyPartMeshKeyMaster (New)",
                        "POST /api/AllopathicDrug (New Save)",
                        "POST /api/section/AddSection (New)",
                        "POST /api/qualification/AddQualification (New)",
                        "POST /api/package/Save (New)",
                        "POST /api/package (Old SavePackage)",
                        "POST /api/roleMaster (Old)"
                    },
                    sampleDoctorReadsKeepOpen = new[]
                    {
                        "GET/POST diagnosis keyword/rubric tabs (Old)",
                        "GET /api/AllopathicDrug/GetAllopathicDrugfordropdown (New)",
                        "GET /api/subsection/SearchRubricsByKeyword (New)",
                        "3D hotspot GET lists for anatomy viewer (New)",
                        "GET /api/PatientLab/GetAllLabTests (catalog read)",
                        "GET /api/mastersAPI/GetPackages (doctor upgrade plans)",
                        "GET /api/mastersAPI/GetMenuByRole?userId= (own menus)"
                    },
                    howToVerify = "Doctor mutate → 403; Admin mutate → not 403; Doctor board reads → 200. See M02_ADMIN_CLINICAL_VERIFICATION_GUIDE.md"
                }
            });
        }
    }
}
