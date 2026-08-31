// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using API.Extensions;
// using AutoMapper;
// using Niga_Domain.Data;
// using Niga_Domain.DTOs;
// using Niga_Domain.Interfaces;
// using Niga_Domain.Master;
// using Microsoft.AspNetCore.Mvc;
// using Niga_Domain.Helpers;

// namespace Niga_Web.Controllers
// {
//     [ApiController]
//     [Route("api/[controller]")]
//     public class LocationController : ControllerBase
//     {
//         private readonly ILocationRepository _locationRepository;
//         private readonly NIGACentrumContext _context;
//         private readonly IMapper _mapper;

//         public LocationController(ILocationRepository locationRepository, IMapper mapper, NIGACentrumContext context)
//         {
//             _context = context;
//             _mapper = mapper;
//             _locationRepository = locationRepository;
//         }

//         [HttpGet("ShowLocationList")]
//         public async Task<List<LocationMasterDto>> ShowLocationList([FromQuery] ParameterParams parameterParams)
//         {
//             var LocationList = await _locationRepository.GetLocationList(parameterParams);
//             Response.AddPaginationHeader(LocationList.CurrentPage, LocationList.PageSize,
//                     LocationList.TotalCount, LocationList.TotalPages);
//             return LocationList;
//         }
//         [HttpPost("AddLocation")]
//         public async Task<ActionResult<LocationMasterDto>> AddNewLocation(LocationMasterDto LocationMasterDto)
//         {
//             try
//             {
//                 if (await _locationRepository.LocationNameExists(LocationMasterDto.LocationName))
//                     return BadRequest("Location Is Already Exists");
//                 var Location = _mapper.Map<LocationMaster>(LocationMasterDto);
//                 _locationRepository.AddNewLocation(Location);
//                 if (await _locationRepository.SaveAllAsync())
//                     return new LocationMasterDto
//                     {
//                         LocationId = LocationMasterDto.LocationId,
//                         LocationName = LocationMasterDto.LocationName
//                     };
//                 return BadRequest("Failed To Add Data");

//             }
//             catch (Exception ex)
//             {
//                 return null;
//             }
//         }

//         [HttpGet("ShowLocationDetails/{Id}")]
//         public async Task<ActionResult<LocationMasterDto>> GetLocationDetailsByCode(int Id)
//         {
//             return await _locationRepository.GetDetailsById(Id);
//         }

//         [HttpPost("UpdateLocationDetails")]
//         public async Task<ActionResult<LocationMasterDto>> UpdateLocationDetails(LocationMasterDto updateLocationDto)
//         {
//             try
//             {
//                 var data = await _locationRepository.GetLocationById(updateLocationDto.LocationId);
//                 _mapper.Map(updateLocationDto, data);
//                 _locationRepository.UpdateLocation(data);
//                 if (await _locationRepository.SaveAllAsync())
//                     return new LocationMasterDto
//                     {
//                         LocationId = updateLocationDto.LocationId,
//                         LocationName = updateLocationDto.LocationName
//                     };
//                 return BadRequest("Failed To Update Data");
//             }
//             catch (Exception ex)
//             {
//                 var result = new
//                 {
//                     Status = 400,
//                     Message = ex.Message
//                 };
//                 return Ok(result);

//             }
//         }

//         [HttpPost("DeleteLocationDetails/{Id}")]
//         public async Task<ActionResult> DeleteLocationDetails(int Id)
//         {
//             var data = await _locationRepository.GetLocationById(Id);
//              try
//                 {
//                     _locationRepository.DeleteLocation(data);
//                     if (await _locationRepository.SaveAllAsync())
//                     {
//                         var result = new
//                         {
//                             Status = 200,
//                             Message = "Data Deleted Successfully"
//                         };
//                         return Ok(result);
//                     }
//                     return BadRequest("Failed To Delete Data");

//                 }
//                 catch (Exception ex)
//                 {
//                     var result = new
//                     {
//                         Status = 400,
//                         Message = ex.Message
//                     };
//                     return BadRequest(result);

//                 }
        
//         }

//     }
// }