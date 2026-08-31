using System;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.DTOs;
using Niga_Domain.Master;

namespace Niga_Domain.Helpers
{
    public static class QueryablePaginationHelper
    {
        public static async Task<PaginatedResult<T>> ToPaginatedResultAsync<T>(
            this IQueryable<T> query,
            PaginationRequestModel request)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1
                ? 10
                : Math.Min(request.PageSize, PaginationRequestModel.MaxPageSize);

            var totalRecords = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResult<T>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize)
            };
        }

        public static IQueryable<ThreeDBodyPartMeshKeyMaster> ApplyMeshKeyFilters(
            this IQueryable<ThreeDBodyPartMeshKeyMaster> query,
            PaginationRequestModel request)
        {
            query = query.Where(x => !x.DeleteStatus);

            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                var search = request.SearchText.Trim();
                query = query.Where(x => x.ThreeDBodyPartMeshKeyName.Contains(search));
            }

            return ApplyMeshKeySorting(query, request);
        }

        public static IQueryable<ThreeDBodyPartSectionMaster> ApplySectionMasterFilters(
            this IQueryable<ThreeDBodyPartSectionMaster> query,
            PaginationRequestModel request,
            int? meshKeyId = null)
        {
            query = query.Where(x => !x.DeleteStatus);

            if (meshKeyId.HasValue)
            {
                query = query.Where(x => x.ThreeDBodyPartMeshKeyId == meshKeyId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                var search = request.SearchText.Trim();
                query = query.Where(x =>
                    x.Section != null && x.Section.SectionName.Contains(search)
                    || x.ThreeDBodyPartMeshKey != null
                        && x.ThreeDBodyPartMeshKey.ThreeDBodyPartMeshKeyName.Contains(search));
            }

            return ApplySectionMasterSorting(query, request);
        }

        public static IQueryable<SubSectionMaster> ApplySubSectionByHotspotSearch(
            this IQueryable<SubSectionMaster> query,
            string hotspotName)
        {
            query = query.Where(x => !x.DeleteStatus && x.SubSectionName != null);

            var search = hotspotName.Trim();
            if (string.IsNullOrWhiteSpace(search))
                return query.OrderBy(x => x.SubSectionName);

            // Prefer sargable-ish Contains over multi-OR word-boundary LIKE (those forced full scans
            // and caused 15s per-concept timeouts). Whole-word filtering happens in-memory after fetch.
            var lower = search.ToLowerInvariant();
            query = query.Where(x => x.SubSectionName!.ToLower().Contains(lower));

            // Prefer shorter names (more specific rubrics) first.
            return query.OrderBy(x => x.SubSectionName!.Length).ThenBy(x => x.SubSectionName);
        }

        public static IQueryable<PatientAppointment> ApplyPatientAppointmentSorting(
            this IQueryable<PatientAppointment> query,
            PaginationRequestModel request)
        {
            var sortBy = (request.SortBy ?? "appointmentdate").Trim().ToLowerInvariant();
            var descending = !string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortBy switch
            {
                "appointmenttime" =>
                    descending
                        ? query.OrderByDescending(x => x.AppointmentDate).ThenByDescending(x => x.AppointmentTime)
                        : query.OrderBy(x => x.AppointmentDate).ThenBy(x => x.AppointmentTime),
                "status" =>
                    descending
                        ? query.OrderByDescending(x => x.Status)
                        : query.OrderBy(x => x.Status),
                "doctorid" =>
                    descending
                        ? query.OrderByDescending(x => x.DoctorId)
                        : query.OrderBy(x => x.DoctorId),
                "userid" =>
                    descending
                        ? query.OrderByDescending(x => x.UserId)
                        : query.OrderBy(x => x.UserId),
                "patientappid" or "id" =>
                    descending
                        ? query.OrderByDescending(x => x.PatientAppId)
                        : query.OrderBy(x => x.PatientAppId),
                _ =>
                    descending
                        ? query.OrderByDescending(x => x.AppointmentDate).ThenByDescending(x => x.AppointmentTime)
                        : query.OrderBy(x => x.AppointmentDate).ThenBy(x => x.AppointmentTime)
            };
        }

        public static IQueryable<ThreeDBodyPartSectionHotspot> ApplyHotspotFilters(
            this IQueryable<ThreeDBodyPartSectionHotspot> query,
            PaginationRequestModel request,
            int? sectionId = null)
        {
            query = query.Where(x => !x.DeleteStatus);

            if (sectionId.HasValue)
            {
                query = query.Where(x => x.SectionId == sectionId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                var search = request.SearchText.Trim();
                query = query.Where(x => x.HotspotName.Contains(search));
            }

            return ApplyHotspotSorting(query, request);
        }

        private static IQueryable<ThreeDBodyPartMeshKeyMaster> ApplyMeshKeySorting(
            IQueryable<ThreeDBodyPartMeshKeyMaster> query,
            PaginationRequestModel request)
        {
            var sortBy = (request.SortBy ?? "EnteredDate").Trim().ToLowerInvariant();
            var descending = !string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortBy switch
            {
                "threed_bodypart_meshkey_name" or "meshkeyname" or "name" =>
                    descending
                        ? query.OrderByDescending(x => x.ThreeDBodyPartMeshKeyName)
                        : query.OrderBy(x => x.ThreeDBodyPartMeshKeyName),
                "threed_bodypart_meshkeyid" or "meshkeyid" or "id" =>
                    descending
                        ? query.OrderByDescending(x => x.ThreeDBodyPartMeshKeyId)
                        : query.OrderBy(x => x.ThreeDBodyPartMeshKeyId),
                _ =>
                    descending
                        ? query.OrderByDescending(x => x.EnteredDate)
                        : query.OrderBy(x => x.EnteredDate)
            };
        }

        private static IQueryable<ThreeDBodyPartSectionMaster> ApplySectionMasterSorting(
            IQueryable<ThreeDBodyPartSectionMaster> query,
            PaginationRequestModel request)
        {
            var sortBy = (request.SortBy ?? "EnteredDate").Trim().ToLowerInvariant();
            var descending = !string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortBy switch
            {
                "threedbodypartsectionid" or "sectionid" =>
                    descending
                        ? query.OrderByDescending(x => x.ThreeDBodyPartSectionId)
                        : query.OrderBy(x => x.ThreeDBodyPartSectionId),
                "sectionname" or "name" =>
                    descending
                        ? query.OrderByDescending(x => x.Section!.SectionName)
                        : query.OrderBy(x => x.Section!.SectionName),
                "threed_bodypart_meshkeyid" or "meshkeyid" =>
                    descending
                        ? query.OrderByDescending(x => x.ThreeDBodyPartMeshKeyId)
                        : query.OrderBy(x => x.ThreeDBodyPartMeshKeyId),
                "threedbodypartsectionmasterid" or "id" =>
                    descending
                        ? query.OrderByDescending(x => x.ThreeDBodyPartSectionMasterId)
                        : query.OrderBy(x => x.ThreeDBodyPartSectionMasterId),
                "threed_bodypart_meshkey_name" or "meshkeyname" =>
                    descending
                        ? query.OrderByDescending(x => x.ThreeDBodyPartMeshKey!.ThreeDBodyPartMeshKeyName)
                        : query.OrderBy(x => x.ThreeDBodyPartMeshKey!.ThreeDBodyPartMeshKeyName),
                _ =>
                    descending
                        ? query.OrderByDescending(x => x.EnteredDate)
                        : query.OrderBy(x => x.EnteredDate)
            };
        }

        private static IQueryable<ThreeDBodyPartSectionHotspot> ApplyHotspotSorting(
            IQueryable<ThreeDBodyPartSectionHotspot> query,
            PaginationRequestModel request)
        {
            var sortBy = (request.SortBy ?? "EnteredDate").Trim().ToLowerInvariant();
            var descending = !string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortBy switch
            {
                "hotspotname" or "name" =>
                    descending
                        ? query.OrderByDescending(x => x.HotspotName)
                        : query.OrderBy(x => x.HotspotName),
                "sectionid" =>
                    descending
                        ? query.OrderByDescending(x => x.SectionId)
                        : query.OrderBy(x => x.SectionId),
                "sectionhotspotid" or "id" =>
                    descending
                        ? query.OrderByDescending(x => x.SectionHotspotId)
                        : query.OrderBy(x => x.SectionHotspotId),
                _ =>
                    descending
                        ? query.OrderByDescending(x => x.EnteredDate)
                        : query.OrderBy(x => x.EnteredDate)
            };
        }
    }
}
