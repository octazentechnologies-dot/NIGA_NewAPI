// using API.Helpers;
// using AutoMapper;
// using Homeocentrum.Niga.NewAPI.Domain.Data;
// using Homeocentrum.Niga.NewAPI.Domain.DTOs;
// using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
// using Homeocentrum.Niga.NewAPI.Domain.Helpers;
// using AutoMapper.QueryableExtensions;
// using Homeocentrum.Niga.NewAPI.Domain.Master;
// using Microsoft.EntityFrameworkCore;
// namespace Homeocentrum.Niga.NewAPI.Domain.Repositories
// {
//     public class LocationRepository:ILocationRepository,IDisposable
//     {

//         private bool isDisposed;
//         private readonly NIGACentrumContext _context;
//         private readonly IMapper _mapper;
//         public LocationRepository(NIGACentrumContext context,IMapper mapper)
//         {
//             _context = context;
//             _mapper=mapper; 
//         }
//         public async Task<PagedList<LocationMasterDto>> GetLocationList(ParameterParams parameterParams)
//         {     
//             var query=_context.Locations.Where(l=>l.IsActive==true)
//                             .OrderByDescending(c=>c.LocationId).AsQueryable();
//          if(parameterParams.search!=null){
//                query=query.Where(x=> x.LocationName.Contains(parameterParams.search.ToLower()));
//          }  
//           return await PagedList<LocationMasterDto>.CreateAsync(query.ProjectTo<LocationMasterDto>(_mapper.ConfigurationProvider)
//                                  .AsNoTracking(),parameterParams.PageNumber,parameterParams.PageSize);
//         }

//         public void AddNewLocation(LocationMaster company)
//         {
//             _context.Entry(company).State=EntityState.Added;
//         }
//         public async Task<bool> LocationNameExists(string LocationName)
//         {
//             return await _context.Locations.AnyAsync(x => x.LocationName==LocationName && x.IsActive==true);
//         }

//         public async Task<LocationMaster> GetLocationById(int Id) 
//         {
//              var location = await _context.Locations
//             .SingleOrDefaultAsync(x=>x.LocationId==Id && x.IsActive==true);
//             return location;
//         }
        
//          public void UpdateLocation(LocationMaster location)
//         {
//             _context.Entry(location).State=EntityState.Modified;

//         }

//         public void DeleteLocation(LocationMaster location)
//         {
//               location.IsActive=false;
//              _context.Entry(location).State=EntityState.Modified;
//         }

//         public async Task<bool> SaveAllAsync()
//         {
//             return await _context.SaveChangesAsync()>0;
//         }

       
//        public void Dispose()
//         {
//             Dispose(true);
//             GC.SuppressFinalize(this);
//         }
//         protected virtual void Dispose(bool disposing)
//         {
//             if(isDisposed) return;
//             if (disposing)
//             {
//                 // free managed resources
//             }
//             isDisposed = true;
//             // free native resources here if there are any
//         }

//         public async Task<LocationMasterDto> GetDetailsById(int Id)
//         {
//             var locationDetails= await _context.Locations
//                         .Where(x=>x.LocationId==Id && x.IsActive==true)
//                         .ProjectTo<LocationMasterDto>(_mapper.ConfigurationProvider)
//                         .SingleOrDefaultAsync();
//             return locationDetails;
//         }
//     }
// }
